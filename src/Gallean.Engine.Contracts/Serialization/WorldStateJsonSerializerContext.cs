using System.Text.Json.Serialization;

namespace Gallean.Engine.Contracts;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    RespectRequiredConstructorParameters = true,
    Converters =
    [
        typeof(ValueJsonConverterFactory),
        typeof(WorldRelationIdJsonConverter),
        typeof(WorldUnitGridJsonConverter),
        typeof(WorldInventoryJsonConverter),
        typeof(ImmutableBitmapJsonConverter),
        typeof(WorldCanonicalStackOrderJsonConverter)
    ])]
[JsonSerializable(typeof(WorldState))]
[JsonSerializable(typeof(WorldStackOrder))]
[JsonSerializable(typeof(ImmutableBitmapJsonConverter.BitmapJson))]
[JsonSerializable(typeof(WorldUnitGridJsonConverter.UnitGridEntryJson[]))]
[JsonSerializable(typeof(WorldItem[]))]
internal sealed partial class WorldStateJsonSerializerContext : JsonSerializerContext;
