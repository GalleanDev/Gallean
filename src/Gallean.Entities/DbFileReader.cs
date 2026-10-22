using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Gallean.Common;

namespace Gallean.Entities;

public static class DbFileReader
{
    public static List<JsonElement> Read(string path)
    {
        return Read(path, DbJsonSerializerContext.Default.JsonElementListInfo);
    }

    public static List<T> Read<T>(string path, JsonTypeInfo<List<T>> typeInfo)
    {
        Utf8JsonReader reader = new(File.ReadAllBytes(path));

        if (!TryRead(ref reader, JsonTokenType.StartObject))
        {
            throw new JsonException($"The data file '{path}' is invalid.");
        }

        if (!TryReadPropertyName(ref reader, "formatVersion"))
        {
            throw new JsonException($"The data file '{path}' does not specify a format version.");
        }

        if (!TryRead(ref reader, JsonTokenType.Number))
        {
            throw new JsonException($"The data file '{path}' has an invalid format version.");
        }

        int formatVersion = reader.GetRequiredInt32();

        if (formatVersion != DbFile.CurrentFormatVersion)
        {
            throw new InvalidDataException($"The data file '{path}' has unsupported format version '{formatVersion}'.");
        }

        if (!TryReadPropertyName(ref reader, "rows"))
        {
            throw new JsonException($"The data file '{path}' has no data.");
        }

        return typeInfo.DeserializeRequired(ref reader);
    }

    private static bool TryReadPropertyName(ref Utf8JsonReader reader, string name)
    {
        return TryRead(ref reader, JsonTokenType.PropertyName) && reader.ValueTextEquals(name);
    }

    private static bool TryRead(ref Utf8JsonReader reader, JsonTokenType tokenType)
    {
        return reader.Read() && reader.TokenType == tokenType;
    }
}
