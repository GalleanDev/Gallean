using System.Text.Json.Serialization;
using Gallean.Engine.Contracts;

namespace Gallean.Configuration;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    RespectRequiredConstructorParameters = true,
    Converters =
    [
        typeof(WorldStateJsonConverter)
    ])]
[JsonSerializable(typeof(GameSave))]
[JsonSerializable(typeof(GameSaveMetadata))]
public sealed partial class GameSaveJsonSerializerContext : JsonSerializerContext;
