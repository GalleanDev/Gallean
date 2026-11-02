using System.Text.Json;
using System.Text.Json.Serialization;
using Gallean.Common;

namespace Gallean.Engine.Contracts;

internal sealed class UnitNameJsonConverter : JsonConverter<UnitName>
{
    private const string ScenarioPropertyName = "scenario";
    private const string LeaderPropertyName = "leader";
    private const string RacePropertyName = "race";
    private const string IsMalePropertyName = "isMale";
    private const string IndexPropertyName = "index";

    public override UnitName Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return new LiteralUnitName(reader.GetRequiredString());
        }
        else if (reader.TokenType == JsonTokenType.StartObject)
        {
            return ReadObject(ref reader);
        }
        else
        {
            throw new JsonException("Invalid unit name.");
        }
    }

    public override void Write(Utf8JsonWriter writer, UnitName value, JsonSerializerOptions options)
    {
        value.Accept(new Writer(writer));
    }

    private static UnitName ReadObject(ref Utf8JsonReader reader)
    {
        ReadToken(ref reader, JsonTokenType.PropertyName);

        UnitName name;

        if (reader.ValueTextEquals(ScenarioPropertyName))
        {
            name = ReadScenarioName(ref reader);
        }
        else if (reader.ValueTextEquals(LeaderPropertyName))
        {
            name = ReadLeaderName(ref reader);
        }
        else
        {
            throw new JsonException("Invalid unit name.");
        }

        ReadToken(ref reader, JsonTokenType.EndObject);

        return name;
    }

    private static ScenarioUnitName ReadScenarioName(ref Utf8JsonReader reader)
    {
        ReadToken(ref reader, JsonTokenType.String);

        return new ScenarioUnitName(new ScenarioTextId(reader.GetRequiredString()));
    }

    private static LeaderUnitName ReadLeaderName(ref Utf8JsonReader reader)
    {
        ReadToken(ref reader, JsonTokenType.StartObject);

        ReadPropertyName(ref reader, RacePropertyName);
        ReadToken(ref reader, JsonTokenType.String);

        if (!Enum.TryParse(reader.GetRequiredString(), out WorldRace race))
        {
            throw new JsonException("Invalid unit name.");
        }

        ReadPropertyName(ref reader, IsMalePropertyName);
        bool isMale = ReadBoolean(ref reader);

        ReadPropertyName(ref reader, IndexPropertyName);
        ReadToken(ref reader, JsonTokenType.Number);
        int index = reader.GetRequiredInt32();

        ReadToken(ref reader, JsonTokenType.EndObject);

        return new LeaderUnitName(new LeaderNameId(race, isMale, index));
    }

    private static void ReadPropertyName(ref Utf8JsonReader reader, string name)
    {
        ReadToken(ref reader, JsonTokenType.PropertyName);

        if (!reader.ValueTextEquals(name))
        {
            throw new JsonException("Invalid unit name.");
        }
    }

    private static bool ReadBoolean(ref Utf8JsonReader reader)
    {
        if (!reader.Read() || reader.TokenType is not (JsonTokenType.True or JsonTokenType.False))
        {
            throw new JsonException("Invalid unit name.");
        }

        return reader.GetBoolean();
    }

    private static void ReadToken(ref Utf8JsonReader reader, JsonTokenType tokenType)
    {
        if (!reader.Read() || reader.TokenType != tokenType)
        {
            throw new JsonException("Invalid unit name.");
        }
    }

    private sealed class Writer(Utf8JsonWriter writer) : IUnitNameVisitor<Empty>
    {
        public Empty Visit(LiteralUnitName name)
        {
            writer.WriteStringValue(name.Text);

            return Empty.Value;
        }

        public Empty Visit(ScenarioUnitName name)
        {
            writer.WriteStartObject();
            writer.WriteString(ScenarioPropertyName, name.Id.Value);
            writer.WriteEndObject();

            return Empty.Value;
        }

        public Empty Visit(LeaderUnitName name)
        {
            writer.WriteStartObject();
            writer.WriteStartObject(LeaderPropertyName);
            writer.WriteString(RacePropertyName, name.Id.Race.ToString());
            writer.WriteBoolean(IsMalePropertyName, name.Id.IsMale);
            writer.WriteNumber(IndexPropertyName, name.Id.Index);
            writer.WriteEndObject();
            writer.WriteEndObject();

            return Empty.Value;
        }
    }
}
