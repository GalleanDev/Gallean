using System.Text.Json.Serialization;

namespace Gallean.Configuration;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    RespectRequiredConstructorParameters = true,
    WriteIndented = true)]
[JsonSerializable(typeof(GameSettings))]
internal sealed partial class GameSettingsJsonSerializerContext : JsonSerializerContext;
