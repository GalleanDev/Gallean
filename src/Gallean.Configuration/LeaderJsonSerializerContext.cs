using System.Text.Json.Serialization;
using Gallean.Engine.Contracts;

namespace Gallean.Configuration;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    RespectRequiredConstructorParameters = true,
    WriteIndented = true,
    Converters =
    [
        typeof(ScenarioLeaderJsonConverter)
    ])]
[JsonSerializable(typeof(LeaderFile))]
public sealed partial class LeaderJsonSerializerContext : JsonSerializerContext;
