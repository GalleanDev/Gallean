using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gallean.Entities;

public static partial class Db
{
    [JsonConverter(typeof(ScenarioIdJsonConverter))]
    public readonly record struct ScenarioId(string Value)
    {
        public bool Equals(ScenarioId other)
        {
            return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return Value is not null ? StringComparer.OrdinalIgnoreCase.GetHashCode(Value) : 0;
        }

        public override string ToString()
        {
            return Value;
        }

        public static implicit operator string(ScenarioId id)
        {
            return id.Value;
        }

        public static implicit operator ScenarioId(string value)
        {
            return new ScenarioId(value);
        }
    }

    internal class ScenarioIdJsonConverter : JsonConverter<ScenarioId>
    {
        public override ScenarioId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string? val = reader.GetString();
            return new ScenarioId(val ?? string.Empty);
        }

        public override void Write(Utf8JsonWriter writer, ScenarioId value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Value);
        }
    }
}
