using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Gallean.Engine.Generators;

[Generator]
public class SerializableValueGenerator : IIncrementalGenerator
{
    private enum ScalarType
    {
        String,
        Int
    }

    private sealed record ValueMetadata(
        string Name,
        string QualifiedName,
        ScalarType Type,
        bool DeclaresInterface,
        bool HasScalarConstructor,
        Location? Location)
    {
        public string ConverterName => Type switch
        {
            ScalarType.String => "StringConverter",
            ScalarType.Int => "IntConverter"
        };
    }

    private const string ContractsNamespace = "Gallean.Engine.Contracts";
    private const string ConverterFactoryName = "Gallean.Engine.Contracts.ValueJsonConverterFactory";

    private static readonly DiagnosticDescriptor UndeclaredValueDescriptor = new(
        "GALENG001",
        "Single value type does not declare ISerializableString or ISerializableInt",
        "Type '{0}' wraps a single {1} but does not declare {2}. Declare it so the type is written to the save file as a scalar rather than an object.",
        "Gallean.Engine.Generators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingConstructorDescriptor = new(
        "GALENG002",
        "Serializable value type has no constructor taking its scalar",
        "Type '{0}' declares {1} but has no constructor taking a single {2}, so no JSON converter can be generated for it",
        "Gallean.Engine.Generators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<ValueMetadata?> values = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, _) => node is StructDeclarationSyntax or RecordDeclarationSyntax,
                transform: GetMetadata)
            .Where(static value => value is not null);

        IncrementalValueProvider<bool> declaresFactory = context.CompilationProvider.Select(static (compilation, _) =>
        {
            return compilation.Assembly.GetTypeByMetadataName(ConverterFactoryName) is not null;
        });

        context.RegisterSourceOutput(
            values.Collect().Combine(declaresFactory),
            static (spc, collected) => Generate(spc, collected.Left!, collected.Right));
    }

    private static ValueMetadata? GetMetadata(GeneratorSyntaxContext context, CancellationToken ct)
    {
        if (context.SemanticModel.GetDeclaredSymbol(context.Node, ct) is INamedTypeSymbol { IsValueType: true } symbol)
        {
            ScalarType? declaredType = GetDeclaredType(symbol);
            ScalarType? type = declaredType ?? GetSingleValueType(symbol);

            if (type is null)
            {
                return null;
            }

            return new ValueMetadata(
                symbol.Name,
                symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                type.Value,
                declaredType is not null,
                HasScalarConstructor(symbol, type.Value),
                symbol.Locations.FirstOrDefault());
        }
        else
        {
            return null;
        }
    }

    private static ScalarType? GetDeclaredType(INamedTypeSymbol symbol)
    {
        foreach (INamedTypeSymbol candidate in symbol.AllInterfaces)
        {
            if (candidate.ContainingNamespace?.ToDisplayString() == ContractsNamespace)
            {
                if (candidate.Name == FormatInterfaceName(ScalarType.String))
                {
                    return ScalarType.String;
                }
                else if (candidate.Name == FormatInterfaceName(ScalarType.Int))
                {
                    return ScalarType.Int;
                }
            }
        }

        return null;
    }

    private static ScalarType? GetSingleValueType(INamedTypeSymbol symbol)
    {
        IPropertySymbol? single = null;

        foreach (ISymbol member in symbol.GetMembers())
        {
            if (member is IPropertySymbol { IsStatic: false, DeclaredAccessibility: Accessibility.Public } property)
            {
                if (single is not null)
                {
                    return null;
                }

                single = property;
            }
        }

        if (single is not null && single.Name == "Value")
        {
            return GetScalarType(single.Type.SpecialType);
        }
        else
        {
            return null;
        }
    }

    private static ScalarType? GetScalarType(SpecialType type)
    {
        return type switch
        {
            SpecialType.System_String => ScalarType.String,
            SpecialType.System_Int32 => ScalarType.Int,
            _ => null
        };
    }

    private static SpecialType GetSpecialType(ScalarType type)
    {
        return type switch
        {
            ScalarType.String => SpecialType.System_String,
            ScalarType.Int => SpecialType.System_Int32
        };
    }

    private static bool HasScalarConstructor(INamedTypeSymbol symbol, ScalarType type)
    {
        SpecialType parameterType = GetSpecialType(type);

        foreach (IMethodSymbol constructor in symbol.InstanceConstructors)
        {
            if (constructor.Parameters.Length == 1 && constructor.Parameters[0].Type.SpecialType == parameterType)
            {
                return true;
            }
        }

        return false;
    }

    private static void Generate(SourceProductionContext context, ImmutableArray<ValueMetadata> values, bool declaresFactory)
    {
        List<ValueMetadata> registered = [];
        HashSet<string> valueNames = [];

        foreach (ValueMetadata value in values.OrderBy(value => value.Name, StringComparer.Ordinal))
        {
            if (valueNames.Add(value.QualifiedName))
            {
                if (!value.DeclaresInterface)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        UndeclaredValueDescriptor,
                        value.Location,
                        value.Name,
                        FormatScalarKeyword(value.Type),
                        FormatInterfaceName(value.Type)));
                }
                else if (!value.HasScalarConstructor)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        MissingConstructorDescriptor,
                        value.Location,
                        value.Name,
                        FormatInterfaceName(value.Type),
                        FormatScalarKeyword(value.Type)));
                }
                else
                {
                    registered.Add(value);
                }
            }
        }

        if (declaresFactory)
        {
            CodeWriter cw = new();
            EmitHeader(cw);

            using (cw.Block("internal sealed partial class ValueJsonConverterFactory"))
            {
                using (cw.Block("private static Dictionary<Type, JsonConverter> CreateConverters()"))
                {
                    using (cw.Block("return new Dictionary<Type, JsonConverter>", "};"))
                    {
                        foreach (ValueMetadata value in registered)
                        {
                            cw.AppendLine($"[typeof({value.QualifiedName})] = new {value.ConverterName}<{value.QualifiedName}>(value => new {value.QualifiedName}(value)),");
                        }
                    }
                }
            }

            context.AddSource("ValueJsonConverterFactory.Converters.g.cs", SourceText.From(cw.ToString(), Encoding.UTF8));
        }
    }

    private static void EmitHeader(CodeWriter cw)
    {
        cw.AppendLine("// <auto-generated/>");
        cw.AppendLine("#nullable enable");
        cw.AppendLine();
        cw.AppendLine("using System;");
        cw.AppendLine("using System.Collections.Generic;");
        cw.AppendLine("using System.Text.Json.Serialization;");
        cw.AppendLine();
        cw.AppendLine($"namespace {ContractsNamespace};");
        cw.AppendLine();
    }

    private static string FormatInterfaceName(ScalarType type)
    {
        return type switch
        {
            ScalarType.String => "ISerializableString",
            ScalarType.Int => "ISerializableInt"
        };
    }

    private static string FormatScalarKeyword(ScalarType type)
    {
        return type switch
        {
            ScalarType.String => "string",
            ScalarType.Int => "int"
        };
    }
}
