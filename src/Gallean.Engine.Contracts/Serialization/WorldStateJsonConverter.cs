using System.Text.Json;
using System.Text.Json.Serialization;
using Gallean.Common;

namespace Gallean.Engine.Contracts;

public sealed class WorldStateJsonConverter : JsonConverter<WorldState>
{
    public override WorldState Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return WorldStateJsonSerializerContext.Default.WorldState.DeserializeRequired(ref reader);
    }

    public override void Write(Utf8JsonWriter writer, WorldState value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, WorldStateJsonSerializerContext.Default.WorldState);
    }
}
