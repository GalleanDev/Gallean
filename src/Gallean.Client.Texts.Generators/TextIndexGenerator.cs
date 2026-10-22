using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Gallean.Client.Texts.Generators;

[Generator]
public class TextIndexGenerator : IIncrementalGenerator
{
    private sealed record TextMetadata(string? Text, int Placeholders, int? Arguments, string File, int Line, Location Location);

    private sealed record UnlocalizedTextMetadata(string Text, Location Location);

    private sealed record ViewMetadata(IReadOnlyList<TextMetadata> Texts, IReadOnlyList<UnlocalizedTextMetadata> Unlocalized);

    private const string TextTypeName = "Gallean.Client.Texts.TextCatalog";
    private static readonly HashSet<string> TranslateMethodNames =
    [
        "Tr",
        "TrSpans"
    ];

    private static readonly HashSet<string> TextAttributes =
    [
        "Text",
        "Content",
        "Title",
        "Header",
        "Watermark",
        "PlaceholderText",
        "ToolTip.Tip"
    ];

    private static readonly Regex AttributeExpression = new(
        "(?<name>[A-Za-z][A-Za-z0-9_.:]*)\\s*=\\s*\"(?<value>[^\"]*)\"",
        RegexOptions.Compiled);

    private static readonly Regex MarkerExpression = new(
        @"^\{\s*(?:[A-Za-z0-9_]+:)?Text(?:Extension)?\s+'(?<text>[^']*)'\s*\}$",
        RegexOptions.Compiled);

    private static readonly Regex PlaceholderExpression = new(
        @"\{(?<index>[0-9]+)(?::[^{}]*)?\}",
        RegexOptions.Compiled);

    private static readonly DiagnosticDescriptor NotLiteralDescriptor = new(
        "GALTXT001",
        "Localized text is not a string literal",
        "Text passed to Tr() is not a string literal, so it cannot be used for localization",
        "Gallean.Client.Texts.Generators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ArgumentCountDescriptor = new(
        "GALTXT002",
        "Localized text has a different number of placeholders than arguments",
        "Text '{0}' has {1} placeholders and {2} arguments",
        "Gallean.Client.Texts.Generators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor NotLocalizedDescriptor = new(
        "GALTXT003",
        "User interface text is not localized",
        "Text '{0}' is written as a literal. Use it with the Text markup extension for localization.",
        "Gallean.Client.Texts.Generators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValueProvider<string> projectDirectory = context.AnalyzerConfigOptionsProvider
            .Select(static (options, _) =>
            {
                options.GlobalOptions.TryGetValue("build_property.projectdir", out string? value);

                return value ?? string.Empty;
            });

        IncrementalValuesProvider<TextMetadata> calls = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => IsTranslateInvocation(node),
                static (context, ct) => GetCall(context, ct))
            .Where(static call => call is not null)
            .Select(static (call, _) => call!)
            .Combine(projectDirectory)
            .Select(static (pair, _) => pair.Left with { File = MakeRelativePath(pair.Left.File, pair.Right) });

        IncrementalValuesProvider<ViewMetadata> views = context.AdditionalTextsProvider
            .Where(static text => Path.GetExtension(text.Path) == ".axaml")
            .Combine(projectDirectory)
            .Select(static (pair, ct) => GetViewMetadata(pair.Left, pair.Right, ct));

        IncrementalValueProvider<string> rootNamespace = context.AnalyzerConfigOptionsProvider
            .Select(static (options, _) =>
            {
                options.GlobalOptions.TryGetValue("build_property.RootNamespace", out string? value);

                return value ?? "Gallean.Client";
            });

        context.RegisterSourceOutput(
            calls.Collect().Combine(views.Collect()).Combine(rootNamespace),
            static (spc, source) => Generate(spc, source.Left.Left, source.Left.Right, source.Right));
    }

    private static bool IsTranslateInvocation(SyntaxNode node)
    {
        return node is InvocationExpressionSyntax invocation &&
               invocation.Expression switch
               {
                   IdentifierNameSyntax identifier => TranslateMethodNames.Contains(identifier.Identifier.ValueText),
                   MemberAccessExpressionSyntax member => TranslateMethodNames.Contains(member.Name.Identifier.ValueText),
                   _ => false
               };
    }

    private static TextMetadata? GetCall(GeneratorSyntaxContext context, CancellationToken ct)
    {
        InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)context.Node;
        SymbolInfo symbol = context.SemanticModel.GetSymbolInfo(invocation, ct);

        if (symbol.Symbol is not IMethodSymbol method ||
            method.ContainingType is null ||
            method.ContainingType.ToDisplayString() != TextTypeName)
        {
            return null;
        }

        SeparatedSyntaxList<ArgumentSyntax> arguments = invocation.ArgumentList.Arguments;
        Location location = invocation.GetLocation();
        FileLinePositionSpan span = location.GetLineSpan();
        string file = span.Path;
        int line = span.StartLinePosition.Line + 1;

        if (arguments.Count == 0 ||
            arguments[0].Expression is not LiteralExpressionSyntax literal ||
            !literal.IsKind(SyntaxKind.StringLiteralExpression))
        {
            return new TextMetadata(null, 0, null, file, line, location);
        }

        string text = (string)literal.Token.Value!;

        return new TextMetadata(text, CountPlaceholders(text), arguments.Count - 1, file, line, location);
    }

    private static string MakeRelativePath(string path, string projectDirectory)
    {
        string normalized = path.Replace('\\', '/');

        if (projectDirectory.Length > 0)
        {
            string directory = projectDirectory.Replace('\\', '/');

            if (!directory.EndsWith("/", StringComparison.Ordinal))
            {
                directory += "/";
            }

            if (normalized.StartsWith(directory, StringComparison.OrdinalIgnoreCase))
            {
                return normalized.Substring(directory.Length);
            }
        }

        return Path.GetFileName(normalized);
    }

    private static ViewMetadata GetViewMetadata(AdditionalText view, string projectDirectory, CancellationToken ct)
    {
        SourceText? source = view.GetText(ct);

        if (source is null)
        {
            return new ViewMetadata([], []);
        }

        string content = source.ToString();
        string file = MakeRelativePath(view.Path, projectDirectory);
        List<TextMetadata> texts = [];
        List<UnlocalizedTextMetadata> unlocalized = [];

        foreach (Match match in AttributeExpression.Matches(content))
        {
            if (TextAttributes.Contains(match.Groups["name"].Value))
            {
                Group value = match.Groups["value"];
                Match marker = MarkerExpression.Match(value.Value);
                TextSpan span = new(value.Index, value.Length);
                Location location = Location.Create(view.Path, span, source.Lines.GetLinePositionSpan(span));
                int line = source.Lines.GetLinePosition(value.Index).Line + 1;

                if (marker.Success)
                {
                    string text = marker.Groups["text"].Value;

                    texts.Add(new TextMetadata(text, CountPlaceholders(text), null, file, line, location));
                }
                else if (!value.Value.StartsWith("{", StringComparison.Ordinal) && HasLetter(value.Value))
                {
                    unlocalized.Add(new UnlocalizedTextMetadata(value.Value, location));
                }
            }
        }

        return new ViewMetadata(texts, unlocalized);
    }

    private static bool HasLetter(string value)
    {
        foreach (char character in value)
        {
            if (char.IsLetter(character))
            {
                return true;
            }
        }

        return false;
    }

    private static int CountPlaceholders(string text)
    {
        int count = 0;

        foreach (Match match in PlaceholderExpression.Matches(text))
        {
            if (int.TryParse(match.Groups["index"].Value, out int index))
            {
                count = Math.Max(count, index + 1);
            }
        }

        return count;
    }

    private static void Generate(
        SourceProductionContext context,
        ImmutableArray<TextMetadata> calls,
        ImmutableArray<ViewMetadata> views,
        string rootNamespace)
    {
        List<TextMetadata> texts = [];

        foreach (TextMetadata call in calls)
        {
            texts.Add(call);
        }

        foreach (ViewMetadata view in views)
        {
            texts.AddRange(view.Texts);
        }

        ReportProblems(context, texts, views);

        IReadOnlyList<TextMetadata> entries = texts
            .Where(text => text.Text is not null)
            .OrderBy(text => text.Text, StringComparer.Ordinal)
            .ThenBy(text => text.File, StringComparer.Ordinal)
            .ThenBy(text => text.Line)
            .ToList();

        CodeWriter cw = new();
        EmitHeader(cw, rootNamespace);
        EmitIndex(cw, entries);

        context.AddSource("ClientTextIndex.g.cs", SourceText.From(cw.ToString(), Encoding.UTF8));
    }

    private static void ReportProblems(
        SourceProductionContext context,
        IReadOnlyList<TextMetadata> texts,
        ImmutableArray<ViewMetadata> views)
    {
        foreach (TextMetadata text in texts)
        {
            if (text.Text is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(NotLiteralDescriptor, text.Location));

                continue;
            }

            if (text.Arguments is not null && text.Arguments != text.Placeholders)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ArgumentCountDescriptor,
                    text.Location,
                    text.Text,
                    text.Placeholders,
                    text.Arguments));
            }
        }

        foreach (ViewMetadata view in views)
        {
            foreach (UnlocalizedTextMetadata unlocalized in view.Unlocalized)
            {
                context.ReportDiagnostic(Diagnostic.Create(NotLocalizedDescriptor, unlocalized.Location, unlocalized.Text));
            }
        }
    }

    private static void EmitHeader(CodeWriter cw, string rootNamespace)
    {
        cw.AppendLine("// <auto-generated/>");
        cw.AppendLine("#nullable enable");
        cw.AppendLine();
        cw.AppendLine("using System.Collections.Immutable;");
        cw.AppendLine();
        cw.AppendLine($"namespace {rootNamespace};");
        cw.AppendLine();
    }

    private static void EmitIndex(CodeWriter cw, IReadOnlyList<TextMetadata> entries)
    {
        cw.AppendLine("internal sealed record ClientTextEntry(string Text, string File, int Line);");
        cw.AppendLine();

        using (cw.Block("internal static class ClientTextIndex"))
        {
            cw.AppendLine("public static ImmutableArray<ClientTextEntry> All { get; } =");
            cw.AppendLine("[");

            foreach (TextMetadata entry in entries)
            {
                string text = SymbolDisplay.FormatLiteral(entry.Text!, quote: true);
                string file = SymbolDisplay.FormatLiteral(entry.File, quote: true);

                cw.AppendLine($"    new({text}, {file}, {entry.Line}),");
            }

            cw.AppendLine("];");
        }
    }
}
