using System.Text.Json;
using System.Text.Json.Serialization;
using Gallean.Common;

namespace Gallean.Engine.Contracts;

internal sealed class ImmutableBitmapJsonConverter : JsonConverter<ImmutableBitmap>
{
    internal sealed record BitmapJson(int Width, int Height, byte[] Bits);

    public override ImmutableBitmap Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        BitmapJson bitmap = WorldStateJsonSerializerContext.Default.BitmapJson.DeserializeRequired(ref reader);

        if (bitmap.Width < 0 || bitmap.Height < 0)
        {
            throw new JsonException("Invalid bitmap size.");
        }

        if (bitmap.Bits.Length != ImmutableBitmap.Builder.GetBitsLength(bitmap.Width, bitmap.Height))
        {
            throw new JsonException("Invalid bitmap bits.");
        }

        return new ImmutableBitmap.Builder(bitmap.Width, bitmap.Height, bitmap.Bits).ToImmutable();
    }

    public override void Write(Utf8JsonWriter writer, ImmutableBitmap value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(
            writer,
            new BitmapJson(value.Width, value.Height, value.ToBits()),
            WorldStateJsonSerializerContext.Default.BitmapJson);
    }
}
