using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Gallean.Client.Shaders.Generators;

[Generator]
public class ShaderGenerator : IIncrementalGenerator
{
    private sealed record UniformMetadata(string Name, string? Type, string? Zero, bool Child, string Declaration, Location Location)
    {
        public string Setter => $"Set{char.ToUpperInvariant(Name[0])}{Name.Substring(1)}";
    }

    private sealed record ShaderMetadata(string Name, string Resource, IReadOnlyList<UniformMetadata> Uniforms)
    {
        public string ProgramName => $"{Name}ShaderProgram";
    }

    private static readonly Dictionary<string, string> UniformTypes = new()
    {
        ["shader"] = "SKShader",
        ["float"] = "float",
        ["float2"] = "SKPoint",
        ["float3"] = "SKPoint3",
        ["float4"] = "SKColorF"
    };

    private static readonly DiagnosticDescriptor UnsupportedDeclarationDescriptor = new(
        "GALSHD001",
        "Unsupported shader uniform declaration",
        "Uniform declaration '{0}' in shader '{1}' has no corresponding C# type",
        "Gallean.Client.Shaders.Generators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<ShaderMetadata> shaders = context.AdditionalTextsProvider
            .Where(static text => Path.GetExtension(text.Path) == ".sksl")
            .Select(static (text, ct) => GetMetadata(text, ct));

        IncrementalValueProvider<string> rootNamespace = context.AnalyzerConfigOptionsProvider
            .Select(static (options, _) =>
            {
                options.GlobalOptions.TryGetValue("build_property.RootNamespace", out string? value);

                return value ?? "Gallean.Client.Shaders";
            });

        context.RegisterSourceOutput(shaders.Collect().Combine(rootNamespace), static (spc, source) => Generate(spc, source.Left, source.Right));
    }

    private static ShaderMetadata GetMetadata(AdditionalText text, CancellationToken ct)
    {
        return new ShaderMetadata(
            Path.GetFileNameWithoutExtension(text.Path),
            Path.GetFileName(text.Path),
            GetUniforms(text, ct));
    }

    private static IReadOnlyList<UniformMetadata> GetUniforms(AdditionalText text, CancellationToken ct)
    {
        SourceText? source = text.GetText(ct);

        if (source is null)
        {
            return [];
        }

        ShaderLexer lexer = new(source.ToString());
        ShaderParser parser = new(lexer.ReadTokens());
        ShaderSyntaxTree tree = parser.Parse();
        List<UniformMetadata> uniforms = [];

        foreach (ShaderUniformDeclaration uniform in tree.Uniforms)
        {
            uniforms.Add(GetUniform(uniform, source.ToString(uniform.Span), GetLocation(text, source, uniform.Span)));
        }

        foreach (TextSpan error in tree.Errors)
        {
            uniforms.Add(new UniformMetadata(string.Empty, null, null, false, source.ToString(error), GetLocation(text, source, error)));
        }

        return uniforms;
    }

    private static UniformMetadata GetUniform(ShaderUniformDeclaration uniform, string declaration, Location location)
    {
        if (uniform.Length is null &&
            UniformTypes.TryGetValue(uniform.TypeName, out string? type))
        {
            bool child = uniform.TypeName == "shader";
            string? zero = child ? null : "default";

            return new UniformMetadata(uniform.Name, type, zero, child, declaration, location);
        }
        else if (uniform.Length > 0 &&
                 uniform.TypeName != "shader" &&
                 UniformTypes.TryGetValue(uniform.TypeName, out string? elementType))
        {
            return new UniformMetadata(uniform.Name, $"ReadOnlySpan<{elementType}>", $"new {elementType}[{uniform.Length}]", false, declaration, location);
        }
        else
        {
            return new UniformMetadata(string.Empty, null, null, false, declaration, location);
        }
    }

    private static Location GetLocation(AdditionalText text, SourceText source, TextSpan span)
    {
        return Location.Create(text.Path, span, source.Lines.GetLinePositionSpan(span));
    }

    private static void Generate(SourceProductionContext context, ImmutableArray<ShaderMetadata> shaders, string rootNamespace)
    {
        IReadOnlyList<ShaderMetadata> sortedShaders = shaders
            .OrderBy(shader => shader.Name, StringComparer.Ordinal)
            .ToList();

        foreach (ShaderMetadata shader in sortedShaders)
        {
            foreach (UniformMetadata uniform in shader.Uniforms)
            {
                if (uniform.Type is null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        UnsupportedDeclarationDescriptor,
                        uniform.Location,
                        uniform.Declaration,
                        shader.Name));
                }
            }
        }

        CodeWriter cw = new();
        EmitHeader(cw, rootNamespace);
        EmitKeys(cw, sortedShaders);

        foreach (ShaderMetadata shader in sortedShaders)
        {
            cw.AppendLine();
            EmitProgram(cw, shader);
        }

        context.AddSource("Shaders.g.cs", SourceText.From(cw.ToString(), Encoding.UTF8));
    }

    private static void EmitHeader(CodeWriter cw, string rootNamespace)
    {
        cw.AppendLine("// <auto-generated/>");
        cw.AppendLine("#nullable enable");
        cw.AppendLine();
        cw.AppendLine("using System;");
        cw.AppendLine("using System.Collections.Generic;");
        cw.AppendLine("using SkiaSharp;");
        cw.AppendLine();
        cw.AppendLine($"namespace {rootNamespace};");
        cw.AppendLine();
    }

    private static void EmitKeys(CodeWriter cw, IReadOnlyList<ShaderMetadata> shaders)
    {
        using (cw.Block("internal static class ShaderKeys"))
        {
            foreach (ShaderMetadata shader in shaders)
            {
                cw.AppendLine($"public static ShaderKey {shader.Name} {{ get; }} = new(\"{shader.Name}\", \"{shader.Resource}\");");
                cw.AppendLine();
            }

            cw.AppendLine($"public static IReadOnlyList<ShaderKey> All {{ get; }} = [{string.Join(", ", shaders.Select(shader => shader.Name))}];");
        }
    }

    private static void EmitProgram(CodeWriter cw, ShaderMetadata shader)
    {
        using (cw.Block($"public sealed class {shader.ProgramName}"))
        {
            EmitEditor(cw, shader);

            cw.AppendLine();
            cw.AppendLine($"private readonly ShaderProgram program = new(ShaderKeys.{shader.Name});");
            cw.AppendLine();

            if (shader.Uniforms.Any(uniform => uniform.Zero is not null))
            {
                using (cw.Block($"public {shader.ProgramName}()"))
                {
                    cw.AppendLine("using Editor editor = Edit();");
                    cw.AppendLine();

                    foreach (UniformMetadata uniform in shader.Uniforms)
                    {
                        if (uniform.Zero is not null)
                        {
                            cw.AppendLine($"editor.{uniform.Setter}({uniform.Zero});");
                        }
                    }
                }

                cw.AppendLine();
            }

            using (cw.Block("public Editor Edit()"))
            {
                cw.AppendLine("return new Editor(program.Edit());");
            }
        }
    }

    private static void EmitEditor(CodeWriter cw, ShaderMetadata shader)
    {
        using (cw.Block("public readonly ref struct Editor"))
        {
            cw.AppendLine("private readonly ShaderProgram.Editor editor;");
            cw.AppendLine();

            using (cw.Block("internal Editor(ShaderProgram.Editor editor)"))
            {
                cw.AppendLine("this.editor = editor;");
            }

            foreach (UniformMetadata uniform in shader.Uniforms)
            {
                if (uniform.Type is not null)
                {
                    cw.AppendLine();

                    using (cw.Block($"public void {uniform.Setter}({uniform.Type} value)"))
                    {
                        if (uniform.Child)
                        {
                            cw.AppendLine($"editor.SetChild(\"{uniform.Name}\", value);");
                        }
                        else
                        {
                            cw.AppendLine($"editor.SetUniform(\"{uniform.Name}\", value);");
                        }
                    }
                }
            }

            cw.AppendLine();

            using (cw.Block("public SKShader Build()"))
            {
                cw.AppendLine("return editor.Build();");
            }

            cw.AppendLine();

            using (cw.Block("public SKData ToUniformData()"))
            {
                cw.AppendLine("return editor.ToUniformData();");
            }

            cw.AppendLine();

            using (cw.Block("public void Dispose()"))
            {
                cw.AppendLine("editor.Dispose();");
            }
        }
    }
}
