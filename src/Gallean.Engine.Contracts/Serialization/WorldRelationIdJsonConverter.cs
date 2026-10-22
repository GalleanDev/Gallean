using System.Text.Json;
using System.Text.Json.Serialization;
using Gallean.Common;

namespace Gallean.Engine.Contracts;

internal sealed class WorldRelationIdJsonConverter : JsonConverter<WorldRelationId>
{
    public override WorldRelationId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Parse(reader.GetRequiredString());
    }

    public override WorldRelationId ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return Parse(reader.GetRequiredString());
    }

    public override void Write(Utf8JsonWriter writer, WorldRelationId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, WorldRelationId value, JsonSerializerOptions options)
    {
        writer.WritePropertyName(value.ToString());
    }

    private static WorldRelationId Parse(string value)
    {
        int separator = value.IndexOf('-');

        if (separator < 0)
        {
            throw new JsonException("Invalid relation id.");
        }

        WorldRace race = ParseRace(value.AsSpan(0, separator));
        WorldRace otherRace = ParseRace(value.AsSpan(separator + 1));

        return new WorldRelationId(race, otherRace);
    }

    private static WorldRace ParseRace(ReadOnlySpan<char> race)
    {
        if (!Enum.TryParse(race, out WorldRace result))
        {
            throw new JsonException("Invalid relation id race.");
        }

        return result;
    }
}
