using System.Text.Json;
using System.Text.Json.Serialization;
using Gallean.Common;

namespace Gallean.Engine.Contracts;

public sealed class ScenarioLeaderJsonConverter : JsonConverter<ScenarioLeader>
{
    public override ScenarioLeader Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return WorldStateJsonSerializerContext.Default.ScenarioLeader.DeserializeRequired(ref reader);
    }

    public override void Write(Utf8JsonWriter writer, ScenarioLeader value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, WorldStateJsonSerializerContext.Default.ScenarioLeader);
    }
}
