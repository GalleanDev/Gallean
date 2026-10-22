using System.Text.Json;
using Gallean.Common;

namespace Gallean.Configuration;

internal static class GameSaveMetadataReader
{
    private const int PrefixLength = 4 * 1024;

    public static async Task<GameSaveMetadata> ReadAsync(Stream stream, CancellationToken cancellationToken)
    {
        byte[] prefix = new byte[PrefixLength];

        int length = await stream.ReadAtLeastAsync(prefix, PrefixLength, false, cancellationToken);

        return Read(prefix.AsSpan(0, length), isFinalBlock: length < PrefixLength);
    }

    private static GameSaveMetadata Read(ReadOnlySpan<byte> prefix, bool isFinalBlock)
    {
        Utf8JsonReader reader = new(prefix, isFinalBlock, state: default);

        ReadToken(ref reader, JsonTokenType.StartObject);
        ReadToken(ref reader, JsonTokenType.PropertyName);
        ReadToken(ref reader, JsonTokenType.Number);
        ReadToken(ref reader, JsonTokenType.PropertyName);

        if (!reader.ValueTextEquals("metadata"))
        {
            throw new JsonException("The save is invalid.");
        }

        ReadToken(ref reader, JsonTokenType.StartObject);

        Utf8JsonReader lookahead = reader;

        if (!lookahead.TrySkip())
        {
            throw new JsonException("The save is invalid.");
        }

        return GameSaveJsonSerializerContext.Default.GameSaveMetadata.DeserializeRequired(ref reader);
    }

    private static void ReadToken(ref Utf8JsonReader reader, JsonTokenType tokenType)
    {
        if (!reader.Read() || reader.TokenType != tokenType)
        {
            throw new JsonException("The save is invalid.");
        }
    }
}
