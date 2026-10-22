using System.Text.Json;
using System.Text.Json.Serialization;
using Gallean.Common;

namespace Gallean.Engine.Contracts;

internal sealed class WorldInventoryJsonConverter : JsonConverter<WorldInventory>
{
    public override WorldInventory Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        WorldItem[] items = WorldStateJsonSerializerContext.Default.WorldItemArray.DeserializeRequired(ref reader);

        return new WorldInventory([.. items]);
    }

    public override void Write(Utf8JsonWriter writer, WorldInventory value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.Items.ToArray(), WorldStateJsonSerializerContext.Default.WorldItemArray);
    }
}
