using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Gallean.Entities.Generators;

[Generator]
public class DatabaseGenerator : IIncrementalGenerator
{
    private sealed record DiagnosticInfo(DiagnosticDescriptor Descriptor, Location? Location, string[] Arguments);

    private sealed record EntityMetadata(
        string ClassName,
        string FullyQualifiedName,
        string FileName,
        bool IsPerScenario,
        bool IsIndex,
        string? KeyProperty,
        string? KeyType,
        string? ScenarioKeyProperty,
        string? LookupKeyProperty,
        string? LookupKeyType,
        IReadOnlyList<DiagnosticInfo> Diagnostics)
    {
        public bool HasIndexRows => IsIndex && !IsPerScenario;
        public bool HasIndexPresence => IsIndex && IsPerScenario;
    }

    private static readonly DiagnosticDescriptor MultipleKeyPropertiesDescriptor = new(
        "GALENT001",
        "Entity has multiple properties with the same data key attribute",
        "Entity '{0}' has multiple properties marked with [{1}]: '{2}' and '{3}'",
        "Gallean.Entities.Generators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ConflictingKeyAttributesDescriptor = new(
        "GALENT002",
        "Property has conflicting data key attributes",
        "Property '{0}' of entity '{1}' cannot be marked with both [DataKey] and [DataLookupKey]",
        "Gallean.Entities.Generators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor MissingScenarioKeyDescriptor = new(
        "GALENT003",
        "Scenario entity has no scenario key",
        "Entity '{0}' is marked with [ScenarioDataFile] but has no property marked with [ScenarioKey]",
        "Gallean.Entities.Generators",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        IncrementalValuesProvider<EntityMetadata?> globalEntities = context.SyntaxProvider.ForAttributeWithMetadataName(
                "Gallean.Entities.GlobalDataFileAttribute",
                predicate: (node, _) => node is RecordDeclarationSyntax or ClassDeclarationSyntax or StructDeclarationSyntax,
                transform: GetEntityMetadata)
            .Where(entity => entity is not null);

        IncrementalValuesProvider<EntityMetadata?> scenarioEntities = context.SyntaxProvider.ForAttributeWithMetadataName(
                "Gallean.Entities.ScenarioDataFileAttribute",
                predicate: (node, _) => node is RecordDeclarationSyntax or ClassDeclarationSyntax or StructDeclarationSyntax,
                transform: GetEntityMetadata)
            .Where(entity => entity is not null);

        IncrementalValuesProvider<EntityMetadata?> campaignEntities = context.SyntaxProvider.ForAttributeWithMetadataName(
                "Gallean.Entities.CampaignDataFileAttribute",
                predicate: (node, _) => node is RecordDeclarationSyntax or ClassDeclarationSyntax or StructDeclarationSyntax,
                transform: GetEntityMetadata)
            .Where(entity => entity is not null);

        IncrementalValueProvider<ImmutableArray<EntityMetadata?>> allGlobals = globalEntities.Collect();
        IncrementalValueProvider<ImmutableArray<EntityMetadata?>> allScenarios = scenarioEntities.Collect();
        IncrementalValueProvider<ImmutableArray<EntityMetadata?>> allCampaigns = campaignEntities.Collect();

        context.RegisterSourceOutput(allGlobals, (spc, entities) => GenerateDatabase(spc, entities!, "Global"));
        context.RegisterSourceOutput(allScenarios, (spc, entities) => GenerateDatabase(spc, entities!, "Scenario"));
        context.RegisterSourceOutput(allCampaigns, (spc, entities) => GenerateDatabase(spc, entities!, "Campaign"));
    }

    private static EntityMetadata? GetEntityMetadata(GeneratorAttributeSyntaxContext context, CancellationToken ct)
    {
        INamedTypeSymbol symbol = (INamedTypeSymbol)context.TargetSymbol;
        AttributeData attribute = context.Attributes.First(a => a.AttributeClass?.Name is "GlobalDataFileAttribute" or "ScenarioDataFileAttribute" or "CampaignDataFileAttribute");

        if (attribute.ConstructorArguments.FirstOrDefault().Value is string fileName)
        {
            string? keyProperty = null;
            string? keyType = null;
            string? scenarioKeyProperty = null;
            string? lookupKeyProperty = null;
            string? lookupKeyType = null;
            List<DiagnosticInfo> diagnostics = [];

            bool isPerScenario = attribute.NamedArguments.Any(arg => arg is { Key: "PerScenario", Value.Value: true });
            bool isIndex = attribute.NamedArguments.Any(arg => arg is { Key: "Index", Value.Value: true });

            foreach (IPropertySymbol? member in symbol.GetMembers().OfType<IPropertySymbol>())
            {
                bool hasScenarioKey = member.GetAttributes().Any(attr => attr.AttributeClass?.Name == "ScenarioKeyAttribute");
                bool hasDataKey = member.GetAttributes().Any(attr => attr.AttributeClass?.Name == "DataKeyAttribute");
                bool hasLookupKey = member.GetAttributes().Any(attr => attr.AttributeClass?.Name == "DataLookupKeyAttribute");

                if (hasDataKey && hasLookupKey)
                {
                    diagnostics.Add(new DiagnosticInfo(
                        ConflictingKeyAttributesDescriptor,
                        member.Locations.FirstOrDefault(),
                        [member.Name, symbol.Name]));
                }

                if (hasScenarioKey)
                {
                    if (scenarioKeyProperty is not null)
                    {
                        diagnostics.Add(new DiagnosticInfo(
                            MultipleKeyPropertiesDescriptor,
                            member.Locations.FirstOrDefault(),
                            [symbol.Name, "ScenarioKey", scenarioKeyProperty, member.Name]));
                    }
                    else
                    {
                        scenarioKeyProperty = member.Name;
                    }
                }

                if (hasDataKey)
                {
                    if (keyProperty is not null)
                    {
                        diagnostics.Add(new DiagnosticInfo(
                            MultipleKeyPropertiesDescriptor,
                            member.Locations.FirstOrDefault(),
                            [symbol.Name, "DataKey", keyProperty, member.Name]));
                    }
                    else
                    {
                        keyProperty = member.Name;
                        keyType = member.Type.ToDisplayString();
                    }
                }

                if (hasLookupKey)
                {
                    if (lookupKeyProperty is not null)
                    {
                        diagnostics.Add(new DiagnosticInfo(
                            MultipleKeyPropertiesDescriptor,
                            member.Locations.FirstOrDefault(),
                            [symbol.Name, "DataLookupKey", lookupKeyProperty, member.Name]));
                    }
                    else
                    {
                        lookupKeyProperty = member.Name;
                        lookupKeyType = member.Type.ToDisplayString();
                    }
                }
            }

            if (attribute.AttributeClass?.Name == "ScenarioDataFileAttribute" && scenarioKeyProperty is null)
            {
                diagnostics.Add(new DiagnosticInfo(
                    MissingScenarioKeyDescriptor,
                    symbol.Locations.FirstOrDefault(),
                    [symbol.Name]));
            }

            return new EntityMetadata(symbol.Name, symbol.ToDisplayString(), fileName, isPerScenario, isIndex, keyProperty, keyType, scenarioKeyProperty, lookupKeyProperty, lookupKeyType, diagnostics);
        }
        else
        {
            return null;
        }
    }

    private static void GenerateDatabase(SourceProductionContext context, ImmutableArray<EntityMetadata> entities, string databaseType)
    {
        if (entities.IsDefaultOrEmpty)
        {
            return;
        }

        foreach (EntityMetadata entity in entities)
        {
            foreach (DiagnosticInfo diagnostic in entity.Diagnostics)
            {
                context.ReportDiagnostic(Diagnostic.Create(diagnostic.Descriptor, diagnostic.Location, diagnostic.Arguments));
            }
        }

        string interfaceName = $"I{databaseType}Database";
        string className = $"{databaseType}Database";
        string loaderName = $"{databaseType}DatabaseLoader";
        string indexInterfaceName = $"I{databaseType}DatabaseIndex";
        string indexClassName = $"{databaseType}DatabaseIndex";
        string contextName = "DbJsonSerializerContext";

        List<EntityMetadata> indexEntities = [.. entities.Where(entity => entity.HasIndexRows)];
        List<EntityMetadata> presenceEntities = [.. entities.Where(entity => entity.HasIndexPresence)];
        List<EntityMetadata> ownEntities = [.. entities.Where(entity => !entity.HasIndexRows)];
        bool hasIndex = indexEntities.Count > 0 || presenceEntities.Count > 0;
        bool hasPerScenarioFiles = entities.Any(entity => entity.IsPerScenario);
        bool hasKeys = entities.Any(entity => entity.KeyProperty is not null);
        bool hasScenarioAndKey = entities.Any(entity => entity.ScenarioKeyProperty is not null && entity.KeyProperty is not null);

        CodeWriter cw = new();
        EmitHeader(cw);

        if (hasIndex)
        {
            using (cw.Block($"public interface {indexInterfaceName}"))
            {
                EmitInterfaceMembers(cw, indexEntities, databaseType, isIndex: true);
                EmitPresenceInterfaceMembers(cw, presenceEntities, databaseType);
            }
            cw.AppendLine();

            using (cw.Block($"public interface {interfaceName} : {indexInterfaceName}"))
            {
                EmitInterfaceMembers(cw, ownEntities, databaseType, isIndex: false);
            }
            cw.AppendLine();

            using (cw.Block($"public sealed record {indexClassName} : {indexInterfaceName}"))
            {
                EmitRecordMembers(cw, indexEntities, databaseType, isIndex: true);
                EmitPresenceRecordMembers(cw, presenceEntities, databaseType);
            }
            cw.AppendLine();
        }
        else
        {
            using (cw.Block($"public interface {interfaceName}"))
            {
                EmitInterfaceMembers(cw, entities, databaseType, isIndex: false);
            }
            cw.AppendLine();
        }

        using (cw.Block($"public sealed record {className} : {interfaceName}"))
        {
            EmitRecordMembers(cw, entities, databaseType, isIndex: false);
            EmitPresenceRecordMembers(cw, presenceEntities, databaseType);
        }
        cw.AppendLine();

        using (cw.Block($"public static class {loaderName}"))
        {
            cw.AppendLine($"private static readonly {contextName} Context = CreateContext();");
            cw.AppendLine();
            using (cw.Block($"private static {contextName} CreateContext()"))
            {
                cw.AppendLine($"JsonSerializerOptions options = new({contextName}.Default.Options);");
                cw.AppendLine("options.Converters.Add(new Db.IdJsonConverterFactory());");
                cw.AppendLine($"return new {contextName}(options);");
            }
            cw.AppendLine();

            using (cw.Block("private static List<T> DeserializeList<T>(string directory, string fileName, JsonTypeInfo<List<T>> typeInfo)"))
            {
                cw.AppendLine("return DbFileReader.Read(Path.Combine(directory, fileName), typeInfo);");
            }
            cw.AppendLine();

            if (hasKeys)
            {
                using (cw.Block("private static Dictionary<TKey, TValue> ByKey<TKey, TValue>(IReadOnlyCollection<TValue> items, Func<TValue, TKey> keySelector) where TKey : notnull"))
                {
                    cw.AppendLine("Dictionary<TKey, TValue> result = new(items.Count);");
                    cw.AppendLine();

                    using (cw.Block("foreach (TValue item in items)"))
                    {
                        cw.AppendLine("result[keySelector(item)] = item;");
                    }
                    cw.AppendLine();
                    cw.AppendLine("return result;");
                }
                cw.AppendLine();
            }

            if (hasScenarioAndKey)
            {
                using (cw.Block("private static Dictionary<TOuter, IReadOnlyDictionary<TInner, TValue>> ByOuterAndInnerKey<TOuter, TInner, TValue>(IEnumerable<TValue> items, Func<TValue, TOuter> outerSelector, Func<TValue, TInner> innerSelector) where TOuter : notnull where TInner : notnull"))
                {
                    cw.AppendLine("Dictionary<TOuter, Dictionary<TInner, TValue>> groups = [];");
                    cw.AppendLine();

                    using (cw.Block("foreach (TValue item in items)"))
                    {
                        cw.AppendLine("TOuter outerKey = outerSelector(item);");
                        cw.AppendLine();

                        using (cw.Block("if (!groups.TryGetValue(outerKey, out Dictionary<TInner, TValue>? inner))"))
                        {
                            cw.AppendLine("inner = [];");
                            cw.AppendLine("groups[outerKey] = inner;");
                        }
                        cw.AppendLine();
                        cw.AppendLine("inner[innerSelector(item)] = item;");
                    }
                    cw.AppendLine();
                    cw.AppendLine("Dictionary<TOuter, IReadOnlyDictionary<TInner, TValue>> result = new(groups.Count);");
                    cw.AppendLine();

                    using (cw.Block("foreach (KeyValuePair<TOuter, Dictionary<TInner, TValue>> group in groups)"))
                    {
                        cw.AppendLine("result[group.Key] = group.Value;");
                    }
                    cw.AppendLine();
                    cw.AppendLine("return result;");
                }
                cw.AppendLine();
            }

            if (hasPerScenarioFiles)
            {
                using (cw.Block("private static List<T> DeserializeScenarioList<T>(IReadOnlyList<string> scenarioDirectories, string fileName, JsonTypeInfo<List<T>> typeInfo)"))
                {
                    cw.AppendLine("List<T> rows = [];");
                    cw.AppendLine();

                    using (cw.Block("foreach (string scenarioDirectory in scenarioDirectories)"))
                    {
                        using (cw.Block("if (File.Exists(Path.Combine(scenarioDirectory, fileName)))"))
                        {
                            cw.AppendLine("rows.AddRange(DeserializeList(scenarioDirectory, fileName, typeInfo));");
                        }
                    }
                    cw.AppendLine();
                    cw.AppendLine("return rows;");
                }
                cw.AppendLine();

                using (cw.Block("private static IReadOnlyList<string> GetScenarioDirectories(string directory)"))
                {
                    cw.AppendLine("List<string> scenarioDirectories = [.. Directory.EnumerateDirectories(directory)];");
                    cw.AppendLine("scenarioDirectories.Sort(StringComparer.OrdinalIgnoreCase);");
                    cw.AppendLine();
                    cw.AppendLine("return scenarioDirectories;");
                }
                cw.AppendLine();

                using (cw.Block("private static IReadOnlyList<string> GetScenarioDirectory(string directory, Db.ScenarioId scenarioId)"))
                {
                    cw.AppendLine("string scenarioDirectory = Path.Combine(directory, scenarioId.Value);");
                    cw.AppendLine();

                    using (cw.Block("if (!Directory.Exists(scenarioDirectory))"))
                    {
                        cw.AppendLine("throw new DirectoryNotFoundException($\"Scenario directory '{scenarioDirectory}' was not found.\");");
                    }
                    cw.AppendLine();
                    cw.AppendLine("return [scenarioDirectory];");
                }
                cw.AppendLine();

                if (presenceEntities.Count > 0)
                {
                    using (cw.Block("private static IReadOnlySet<Db.ScenarioId> GetScenariosWithTable(string directory, string fileName)"))
                    {
                        cw.AppendLine("HashSet<Db.ScenarioId> scenarios = [];");
                        cw.AppendLine();

                        using (cw.Block("foreach (string scenarioDirectory in GetScenarioDirectories(directory))"))
                        {
                            using (cw.Block("if (File.Exists(Path.Combine(scenarioDirectory, fileName)))"))
                            {
                                cw.AppendLine("scenarios.Add(new Db.ScenarioId(Path.GetFileName(scenarioDirectory)));");
                            }
                        }
                        cw.AppendLine();
                        cw.AppendLine("return scenarios;");
                    }
                    cw.AppendLine();
                }

                using (cw.Block($"public static {className} Load(string directory)"))
                {
                    cw.AppendLine("return Load(directory, GetScenarioDirectories(directory));");
                }
                cw.AppendLine();

                using (cw.Block($"public static {className} Load(string directory, Db.ScenarioId scenarioId)"))
                {
                    cw.AppendLine("return Load(directory, GetScenarioDirectory(directory, scenarioId));");
                }
                cw.AppendLine();

                using (cw.Block($"private static {className} Load(string directory, IReadOnlyList<string> scenarioDirectories)"))
                {
                    EmitLoadBody(cw, entities, presenceEntities, databaseType, className, isIndex: false);
                }
            }
            else
            {
                using (cw.Block($"public static {className} Load(string directory)"))
                {
                    EmitLoadBody(cw, entities, presenceEntities, databaseType, className, isIndex: false);
                }
            }

            if (hasIndex)
            {
                cw.AppendLine();

                using (cw.Block($"public static {indexClassName} LoadIndex(string directory)"))
                {
                    EmitLoadBody(cw, indexEntities, presenceEntities, databaseType, indexClassName, isIndex: true);
                }
            }
        }

        context.AddSource($"{databaseType}Database.g.cs", SourceText.From(cw.ToString(), Encoding.UTF8));
    }

    private static void EmitHeader(CodeWriter cw)
    {
        cw.AppendLine("// <auto-generated/>");
        cw.AppendLine("#nullable enable");
        cw.AppendLine();
        cw.AppendLine("using System;");
        cw.AppendLine("using System.Collections.Generic;");
        cw.AppendLine("using System.IO;");
        cw.AppendLine("using System.Linq;");
        cw.AppendLine("using System.Text.Json;");
        cw.AppendLine("using System.Text.Json.Serialization;");
        cw.AppendLine("using System.Text.Json.Serialization.Metadata;");
        cw.AppendLine("using Gallean.Entities;");
        cw.AppendLine();
        cw.AppendLine("namespace Gallean.Entities;");
        cw.AppendLine();
    }

    private static void EmitInterfaceMembers(CodeWriter cw, IReadOnlyList<EntityMetadata> entities, string databaseType, bool isIndex)
    {
        if (databaseType == "Global" && !isIndex)
        {
            cw.AppendLine("Db.GlobalVariables GlobalVariables { get; }");
        }

        foreach (EntityMetadata entity in entities)
        {
            if (IsGlobalVariables(entity, databaseType))
            {
                continue;
            }

            string propName = GetPlural(entity.ClassName);

            if (HasSingleKeyIndex(entity))
            {
                cw.AppendLine($"IReadOnlyDictionary<{entity.KeyType}, {entity.FullyQualifiedName}> {propName} {{ get; }}");
            }
            else
            {
                cw.AppendLine($"IReadOnlyList<{entity.FullyQualifiedName}> {propName} {{ get; }}");
            }

            if (entity.ScenarioKeyProperty is not null)
            {
                cw.AppendLine($"ILookup<Db.ScenarioId, {entity.FullyQualifiedName}> {propName}ByScenario {{ get; }}");
            }

            if (entity.ScenarioKeyProperty is not null && entity.KeyProperty is not null)
            {
                cw.AppendLine($"IReadOnlyDictionary<Db.ScenarioId, IReadOnlyDictionary<{entity.KeyType}, {entity.FullyQualifiedName}>> {propName}ByScenarioAnd{entity.KeyProperty} {{ get; }}");
            }

            if (entity.ScenarioKeyProperty is not null && entity.LookupKeyProperty is not null)
            {
                cw.AppendLine($"IReadOnlyDictionary<Db.ScenarioId, ILookup<{entity.LookupKeyType}, {entity.FullyQualifiedName}>> {propName}ByScenarioAnd{entity.LookupKeyProperty} {{ get; }}");
            }
            else if (entity.LookupKeyProperty is not null)
            {
                cw.AppendLine($"ILookup<{entity.LookupKeyType}, {entity.FullyQualifiedName}> {propName}By{entity.LookupKeyProperty} {{ get; }}");
            }
        }
    }

    private static void EmitRecordMembers(CodeWriter cw, IReadOnlyList<EntityMetadata> entities, string databaseType, bool isIndex)
    {
        if (databaseType == "Global" && !isIndex)
        {
            cw.AppendLine("public Db.GlobalVariables GlobalVariables { get; init; } = null!;");
        }

        foreach (EntityMetadata entity in entities)
        {
            if (IsGlobalVariables(entity, databaseType))
            {
                continue;
            }

            string propName = GetPlural(entity.ClassName);

            if (HasSingleKeyIndex(entity))
            {
                cw.AppendLine($"public IReadOnlyDictionary<{entity.KeyType}, {entity.FullyQualifiedName}> {propName} {{ get; init; }} = null!;");
            }
            else
            {
                cw.AppendLine($"public IReadOnlyList<{entity.FullyQualifiedName}> {propName} {{ get; init; }} = [];");
            }

            if (entity.ScenarioKeyProperty is not null)
            {
                cw.AppendLine($"public ILookup<Db.ScenarioId, {entity.FullyQualifiedName}> {propName}ByScenario {{ get; init; }} = null!;");
            }

            if (entity.ScenarioKeyProperty is not null && entity.KeyProperty is not null)
            {
                cw.AppendLine($"public IReadOnlyDictionary<Db.ScenarioId, IReadOnlyDictionary<{entity.KeyType}, {entity.FullyQualifiedName}>> {propName}ByScenarioAnd{entity.KeyProperty} {{ get; init; }} = null!;");
            }

            if (entity.ScenarioKeyProperty is not null && entity.LookupKeyProperty is not null)
            {
                cw.AppendLine($"public IReadOnlyDictionary<Db.ScenarioId, ILookup<{entity.LookupKeyType}, {entity.FullyQualifiedName}>> {propName}ByScenarioAnd{entity.LookupKeyProperty} {{ get; init; }} = null!;");
            }
            else if (entity.LookupKeyProperty is not null)
            {
                cw.AppendLine($"public ILookup<{entity.LookupKeyType}, {entity.FullyQualifiedName}> {propName}By{entity.LookupKeyProperty} {{ get; init; }} = null!;");
            }
        }
    }

    private static void EmitLoadBody(CodeWriter cw, IReadOnlyList<EntityMetadata> entities, IReadOnlyList<EntityMetadata> presenceEntities, string databaseType, string className, bool isIndex)
    {
        if (databaseType == "Global" && !isIndex)
        {
            cw.AppendLine("Db.GlobalVariables globalVariables = DeserializeList<Db.GlobalVariables>(directory, \"GVars.json\", Context.GlobalVariablesListInfo).Single();");
        }

        foreach (EntityMetadata entity in entities)
        {
            if (IsGlobalVariables(entity, databaseType))
            {
                continue;
            }

            string propName = GetPlural(entity.ClassName);
            string varName = char.ToLower(propName[0]) + propName.Substring(1);
            string fileName = entity.FileName.Split('/').Last();
            string deserializeExpression;

            if (entity.IsPerScenario)
            {
                deserializeExpression = $"DeserializeScenarioList<{entity.FullyQualifiedName}>(scenarioDirectories, \"{fileName}\", Context.{entity.ClassName}ListInfo)";
            }
            else
            {
                deserializeExpression = $"DeserializeList<{entity.FullyQualifiedName}>(directory, \"{fileName}\", Context.{entity.ClassName}ListInfo)";
            }

            string rowsExpression;

            if (HasSingleKeyIndex(entity))
            {
                cw.AppendLine($"IReadOnlyDictionary<{entity.KeyType}, {entity.FullyQualifiedName}> {varName} = ByKey({deserializeExpression}, x => x.{entity.KeyProperty});");
                rowsExpression = $"{varName}.Values";
            }
            else if (entity.KeyProperty is not null)
            {
                cw.AppendLine($"IReadOnlyList<{entity.FullyQualifiedName}> {varName} = [.. ByKey({deserializeExpression}, x => (x.{entity.ScenarioKeyProperty}, x.{entity.KeyProperty})).Values];");
                rowsExpression = varName;
            }
            else
            {
                cw.AppendLine($"IReadOnlyList<{entity.FullyQualifiedName}> {varName} = {deserializeExpression}.AsReadOnly();");
                rowsExpression = varName;
            }

            if (entity.ScenarioKeyProperty is not null)
            {
                cw.AppendLine($"ILookup<Db.ScenarioId, {entity.FullyQualifiedName}> {varName}ByScenario = {rowsExpression}.ToLookup(x => x.{entity.ScenarioKeyProperty});");
            }

            if (entity.ScenarioKeyProperty is not null && entity.KeyProperty is not null)
            {
                cw.AppendLine($"IReadOnlyDictionary<Db.ScenarioId, IReadOnlyDictionary<{entity.KeyType}, {entity.FullyQualifiedName}>> {varName}ByScenarioAnd{entity.KeyProperty} = ByOuterAndInnerKey({rowsExpression}, x => x.{entity.ScenarioKeyProperty}, x => x.{entity.KeyProperty});");
            }

            if (entity.ScenarioKeyProperty is not null && entity.LookupKeyProperty is not null)
            {
                cw.AppendLine($"IReadOnlyDictionary<Db.ScenarioId, ILookup<{entity.LookupKeyType}, {entity.FullyQualifiedName}>> {varName}ByScenarioAnd{entity.LookupKeyProperty} = {rowsExpression}.GroupBy(x => x.{entity.ScenarioKeyProperty}).ToDictionary(g => g.Key, g => g.ToLookup(x => x.{entity.LookupKeyProperty}));");
            }
            else if (entity.LookupKeyProperty is not null)
            {
                cw.AppendLine($"ILookup<{entity.LookupKeyType}, {entity.FullyQualifiedName}> {varName}By{entity.LookupKeyProperty} = {rowsExpression}.ToLookup(x => x.{entity.LookupKeyProperty});");
            }
        }

        foreach (EntityMetadata entity in presenceEntities)
        {
            string propName = GetPresenceName(entity, databaseType);
            string varName = char.ToLower(propName[0]) + propName.Substring(1);

            if (isIndex)
            {
                string fileName = entity.FileName.Split('/').Last();

                cw.AppendLine($"IReadOnlySet<Db.ScenarioId> {varName} = GetScenariosWithTable(directory, \"{fileName}\");");
            }
            else
            {
                string rowsVarName = char.ToLower(GetPlural(entity.ClassName)[0]) + GetPlural(entity.ClassName).Substring(1);

                cw.AppendLine($"IReadOnlySet<Db.ScenarioId> {varName} = new HashSet<Db.ScenarioId>({rowsVarName}ByScenario.Select(x => x.Key));");
            }
        }

        cw.AppendLine();

        using (cw.Block($"return new {className}", "};"))
        {
            if (databaseType == "Global" && !isIndex)
            {
                cw.AppendLine("GlobalVariables = globalVariables,");
            }

            foreach (EntityMetadata entity in entities)
            {
                if (IsGlobalVariables(entity, databaseType))
                {
                    continue;
                }

                string propName = GetPlural(entity.ClassName);
                string varName = char.ToLower(propName[0]) + propName.Substring(1);

                cw.AppendLine($"{propName} = {varName},");

                if (entity.ScenarioKeyProperty is not null)
                {
                    cw.AppendLine($"{propName}ByScenario = {varName}ByScenario,");
                }

                if (entity.ScenarioKeyProperty is not null && entity.KeyProperty is not null)
                {
                    cw.AppendLine($"{propName}ByScenarioAnd{entity.KeyProperty} = {varName}ByScenarioAnd{entity.KeyProperty},");
                }

                if (entity.ScenarioKeyProperty is not null && entity.LookupKeyProperty is not null)
                {
                    cw.AppendLine($"{propName}ByScenarioAnd{entity.LookupKeyProperty} = {varName}ByScenarioAnd{entity.LookupKeyProperty},");
                }
                else if (entity.LookupKeyProperty is not null)
                {
                    cw.AppendLine($"{propName}By{entity.LookupKeyProperty} = {varName}By{entity.LookupKeyProperty},");
                }
            }

            foreach (EntityMetadata entity in presenceEntities)
            {
                string propName = GetPresenceName(entity, databaseType);
                string varName = char.ToLower(propName[0]) + propName.Substring(1);

                cw.AppendLine($"{propName} = {varName},");
            }
        }
    }

    private static void EmitPresenceInterfaceMembers(CodeWriter cw, IReadOnlyList<EntityMetadata> entities, string databaseType)
    {
        foreach (EntityMetadata entity in entities)
        {
            cw.AppendLine($"IReadOnlySet<Db.ScenarioId> {GetPresenceName(entity, databaseType)} {{ get; }}");
        }
    }

    private static void EmitPresenceRecordMembers(CodeWriter cw, IReadOnlyList<EntityMetadata> entities, string databaseType)
    {
        foreach (EntityMetadata entity in entities)
        {
            cw.AppendLine($"public IReadOnlySet<Db.ScenarioId> {GetPresenceName(entity, databaseType)} {{ get; init; }} = null!;");
        }
    }

    private static string GetPresenceName(EntityMetadata entity, string databaseType)
    {
        string plural = GetPlural(entity.ClassName);

        if (plural.StartsWith(databaseType, StringComparison.Ordinal))
        {
            return $"ScenariosWith{plural.Substring(databaseType.Length)}";
        }
        else
        {
            return $"ScenariosWith{plural}";
        }
    }

    private static bool HasSingleKeyIndex(EntityMetadata entity)
    {
        return entity.KeyProperty is not null && entity.ScenarioKeyProperty is null;
    }

    private static bool IsGlobalVariables(EntityMetadata entity, string databaseType)
    {
        return entity.ClassName == "GlobalVariables" && databaseType == "Global";
    }

    private static string GetPlural(string word)
    {
        if (word.EndsWith("s"))
        {
            return word;
        }
        else if (word.EndsWith("y"))
        {
            return word.Substring(0, word.Length - 1) + "ies";
        }
        else if (word.EndsWith("ch") || word.EndsWith("sh") || word.EndsWith("x"))
        {
            return word + "es";
        }
        else
        {
            return word + "s";
        }
    }
}
