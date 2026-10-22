using System.Collections.Frozen;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gallean.Common;

namespace Gallean.Engine.Contracts;

internal sealed partial class ValueJsonConverterFactory : JsonConverterFactory
{
    private sealed class StringConverter<T>(Func<string, T> create) : JsonConverter<T>
        where T : struct, ISerializableString
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return create(reader.GetRequiredString());
        }

        public override T ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return create(reader.GetRequiredString());
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Value);
        }

        public override void WriteAsPropertyName(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WritePropertyName(value.Value);
        }
    }

    private sealed class IntConverter<T>(Func<int, T> create) : JsonConverter<T>
        where T : struct, ISerializableInt
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return create(reader.GetRequiredInt32());
        }

        public override T ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string name = reader.GetRequiredString();

            if (!int.TryParse(name, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            {
                throw new JsonException("Invalid integer property name.");
            }

            return create(value);
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value.Value);
        }

        public override void WriteAsPropertyName(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WritePropertyName(value.Value.ToString(CultureInfo.InvariantCulture));
        }
    }

    private static readonly FrozenDictionary<Type, JsonConverter> Converters = CreateConverters().ToFrozenDictionary();

    public override bool CanConvert(Type typeToConvert)
    {
        return Converters.ContainsKey(typeToConvert);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return Converters[typeToConvert];
    }
}
