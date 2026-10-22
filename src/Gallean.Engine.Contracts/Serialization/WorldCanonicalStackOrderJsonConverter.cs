using System.Text.Json;
using System.Text.Json.Serialization;
using Gallean.Common;

namespace Gallean.Engine.Contracts;

internal sealed class WorldCanonicalStackOrderJsonConverter : JsonConverter<WorldCanonicalStackOrder>
{
    public override WorldCanonicalStackOrder Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        WorldStackOrder order = WorldStateJsonSerializerContext.Default.WorldStackOrder.DeserializeRequired(ref reader);

        if (order is WorldCanonicalStackOrder canonicalOrder)
        {
            return canonicalOrder;
        }
        else
        {
            throw new JsonException("Invalid stack order.");
        }
    }

    public override void Write(Utf8JsonWriter writer, WorldCanonicalStackOrder value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, WorldStateJsonSerializerContext.Default.WorldStackOrder);
    }
}
