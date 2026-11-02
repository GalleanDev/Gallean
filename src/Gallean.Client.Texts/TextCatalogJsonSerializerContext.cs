using System.Text.Json.Serialization;

namespace Gallean.Client.Texts;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    RespectNullableAnnotations = true,
    RespectRequiredConstructorParameters = true)]
[JsonSerializable(typeof(TextCatalogFile))]
internal sealed partial class TextCatalogJsonSerializerContext : JsonSerializerContext;
