using System.Text.Json.Serialization;

namespace Extractor;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    RespectNullableAnnotations = true)]
[JsonSerializable(typeof(IReadOnlyCollection<SceneInterface>), TypeInfoPropertyName = "SceneInterfaceCollection")]
[JsonSerializable(typeof(IReadOnlyDictionary<string, UnitTypeSounds>), TypeInfoPropertyName = "UnitTypeSoundsMap")]
internal sealed partial class ExtractorJsonSerializerContext : JsonSerializerContext;
