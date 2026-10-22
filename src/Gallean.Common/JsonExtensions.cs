using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Gallean.Common;

public static class JsonExtensions
{
    extension<T>(JsonTypeInfo<T> typeInfo)
    {
        public T DeserializeRequired(ref Utf8JsonReader reader)
        {
            return Require(JsonSerializer.Deserialize(ref reader, typeInfo));
        }

        public T DeserializeRequired(Stream stream)
        {
            return Require(JsonSerializer.Deserialize(stream, typeInfo));
        }

        public async Task<T> DeserializeRequiredAsync(Stream stream, CancellationToken cancellationToken)
        {
            return Require(await JsonSerializer.DeserializeAsync(stream, typeInfo, cancellationToken));
        }
    }

    extension(ref Utf8JsonReader reader)
    {
        public string GetRequiredString()
        {
            if (reader.TokenType is not (JsonTokenType.String or JsonTokenType.PropertyName))
            {
                throw new JsonException("Invalid string value.");
            }

            string? value = reader.GetString();

            if (value is null)
            {
                throw new JsonException("Unexpected null string value.");
            }

            return value;
        }

        public int GetRequiredInt32()
        {
            if (reader.TokenType is not JsonTokenType.Number || !reader.TryGetInt32(out int value))
            {
                throw new JsonException("Invalid integer value.");
            }

            return value;
        }
    }

    private static T Require<T>(T? value)
    {
        if (value is null)
        {
            throw new JsonException($"Unexpected null {typeof(T).Name}.");
        }

        return value;
    }
}
