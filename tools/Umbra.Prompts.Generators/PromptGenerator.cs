using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Umbra.Prompts.Generators;

[Generator]
public class PromptGenerator : IIncrementalGenerator
{
    private sealed record PromptMetadata(string Id, string Bytes);

    private const int HashLength = 12;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<PromptMetadata> prompts = context.AdditionalTextsProvider
            .Where(static text => Path.GetExtension(text.Path) == ".txt")
            .Combine(context.AnalyzerConfigOptionsProvider)
            .Select(static (source, token) => GetMetadata(source.Left, source.Right, token));

        IncrementalValueProvider<string> rootNamespace = context.AnalyzerConfigOptionsProvider
            .Select(static (options, _) =>
            {
                options.GlobalOptions.TryGetValue("build_property.RootNamespace", out string? value);

                return value ?? "Umbra.Prompts";
            });

        context.RegisterSourceOutput(prompts.Collect().Combine(rootNamespace), static (spc, source) => Generate(spc, source.Left, source.Right));
    }

    private static PromptMetadata GetMetadata(AdditionalText text, AnalyzerConfigOptionsProvider options, CancellationToken token)
    {
        options.GetOptions(text).TryGetValue("build_metadata.AdditionalFiles.PromptDirectory", out string? directory);

        string id = (directory ?? string.Empty) + Path.GetFileNameWithoutExtension(text.Path);
        SourceText? contents = text.GetText(token);

        return new PromptMetadata(id, ComputeBytes(contents?.ToString() ?? string.Empty));
    }

    private static string ComputeBytes(string text)
    {
        string normalized = text.Replace("\r\n", "\n").Trim();

        using SHA256 sha = SHA256.Create();

        byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(normalized));
        StringBuilder builder = new();

        for (int index = 0; index < HashLength; index++)
        {
            if (index > 0)
            {
                builder.Append(", ");
            }

            builder.Append("0x").Append(hash[index].ToString("x2", CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    private static void Generate(SourceProductionContext context, ImmutableArray<PromptMetadata> prompts, string rootNamespace)
    {
        CodeWriter writer = new();

        writer.AppendLine("using System.Collections.Generic;");
        writer.AppendLine();
        writer.AppendLine($"namespace {rootNamespace};");
        writer.AppendLine();

        using (writer.Block("public static class PromptMap"))
        {
            using (writer.Block("private static readonly IReadOnlyDictionary<string, UmbraPromptHash> Hashes = new Dictionary<string, UmbraPromptHash>", "};"))
            {
                foreach (PromptMetadata prompt in prompts.OrderBy(static prompt => prompt.Id, StringComparer.Ordinal))
                {
                    writer.AppendLine($"[\"{prompt.Id}\"] = new UmbraPromptHash(new byte[] {{ {prompt.Bytes} }}),");
                }
            }

            writer.AppendLine();

            using (writer.Block("public static UmbraPromptHash GetHash(string id)"))
            {
                writer.AppendLine("if (!Hashes.TryGetValue(id, out UmbraPromptHash hash))");

                using (writer.Block())
                {
                    writer.AppendLine("throw new KeyNotFoundException($\"No prompt found for key '{id}'.\");");
                }

                writer.AppendLine();
                writer.AppendLine("return hash;");
            }
        }

        context.AddSource("PromptMap.g.cs", SourceText.From(writer.ToString(), Encoding.UTF8));
    }
}
