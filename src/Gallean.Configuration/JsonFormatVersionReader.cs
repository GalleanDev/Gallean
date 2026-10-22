using System.Text.Json;
using Gallean.Common;

namespace Gallean.Configuration;

internal static class JsonFormatVersionReader
{
    private const int PrefixLength = 64;

    public static async Task<int> ReadAsync(Stream stream, CancellationToken cancellationToken)
    {
        byte[] prefix = new byte[PrefixLength];

        int length = await stream.ReadAtLeastAsync(prefix, PrefixLength, false, cancellationToken);

        return Read(prefix.AsSpan(0, length), isFinalBlock: length < PrefixLength);
    }

    private static int Read(ReadOnlySpan<byte> prefix, bool isFinalBlock)
    {
        Utf8JsonReader reader = new(prefix, isFinalBlock, state: default);

        ReadToken(ref reader, JsonTokenType.StartObject);
        ReadToken(ref reader, JsonTokenType.PropertyName);

        if (!reader.ValueTextEquals("formatVersion"))
        {
            throw new JsonException("The file does not start with a format version.");
        }

        ReadToken(ref reader, JsonTokenType.Number);

        return reader.GetRequiredInt32();
    }

    private static void ReadToken(ref Utf8JsonReader reader, JsonTokenType tokenType)
    {
        if (!reader.Read() || reader.TokenType != tokenType)
        {
            throw new JsonException("The file does not start with a format version.");
        }
    }
}
