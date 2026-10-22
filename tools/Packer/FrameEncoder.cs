using System.Buffers.Binary;

namespace Packer;

internal static partial class FrameEncoder
{
    private const int PaletteColors = 256;
    private const int QualityLossy = 85;
    private const int QualityLossless = 100;
    private const int QualityBackgroundAlbedo = 90;
    private const int BackgroundNormalPrecisionBits = 6;

    public static EncodedEntry EncodeFrames(
        IReadOnlyList<AssetSprite> frames,
        AssetKey key,
        IReadOnlyList<AssetTransform> applied,
        EncodingType encodingType,
        ISpriteFilterSource source,
        QuantizerPool quantizers)
    {
        SpriteFilterPlan plan = SpriteFilters.CreatePlan(key.CanonicalId, frames[0].Metadata, applied);
        IReadOnlyList<AssetSprite> filtered = Filter(frames, plan.Filters, new SpriteFilterContext(key, source));
        List<AssetSpriteMetadata> metadata = [];
        List<byte[]> images = [];
        HashSet<uint> colors = [];

        foreach (AssetSprite frame in filtered)
        {
            byte[] pixels = frame.Pixels.ToArray();
            CountColors(pixels, colors);
            metadata.Add(frame.Metadata);
            images.Add(pixels);
        }

        if (colors.Count > PaletteColors && encodingType.HasFlag(EncodingType.Palette))
        {
            WuQuantizer quantizer = quantizers.Rent();

            foreach (byte[] pixels in images)
            {
                quantizer.AddColors(pixels);
            }

            quantizer.ResolvePalette();

            foreach (byte[] pixels in images)
            {
                QuantizeColors(pixels, quantizer);
            }

            quantizers.Return(quantizer);
        }

        WebpSettings settings;

        if (encodingType.HasFlag(EncodingType.Lossy))
        {
            settings = new WebpSettings(Lossless: false, Exact: true, Method: 4, Quality: QualityLossy, SharpYuv: true);
        }
        else
        {
            settings = new WebpSettings(Lossless: true, Exact: true, Method: 4, Quality: QualityLossless, SharpYuv: false);
        }

        return new EncodedEntry(Save(metadata, images, settings), metadata, plan.Transforms);
    }

    public static AssetPack.Writer.EncodedImage EncodeSurfaceLayer(AssetSpriteLayer layer, AssetSpriteLayerType type)
    {
        WebpSettings settings = type switch
        {
            AssetSpriteLayerType.Normal => new WebpSettings(Lossless: false, Exact: false, Method: 6, Quality: QualityLossy, SharpYuv: true),
            AssetSpriteLayerType.Depth => new WebpSettings(Lossless: true, Exact: false, Method: 4, Quality: QualityLossless, SharpYuv: false),
            AssetSpriteLayerType.Albedo => new WebpSettings(Lossless: true, Exact: false, Method: 4, Quality: QualityLossless, SharpYuv: false)
        };

        ReadOnlySpan<byte> pixels = type switch
        {
            AssetSpriteLayerType.Normal => EncodeSurfaceNormals(layer.Pixels.Span),
            AssetSpriteLayerType.Depth => layer.Pixels.Span,
            AssetSpriteLayerType.Albedo => layer.Pixels.Span
        };

        return WebpEncoder.Encode(pixels, layer.PixelWidth, layer.PixelHeight, settings);
    }

    public static AssetPack.Writer.EncodedImage EncodeBackgroundLayer(AssetSpriteLayer layer, AssetSpriteLayerType type)
    {
        WebpSettings settings = type switch
        {
            AssetSpriteLayerType.Normal => new WebpSettings(Lossless: true, Exact: false, Method: 4, Quality: QualityLossless, SharpYuv: false),
            AssetSpriteLayerType.Depth => new WebpSettings(Lossless: true, Exact: false, Method: 4, Quality: QualityLossless, SharpYuv: false),
            AssetSpriteLayerType.Albedo => new WebpSettings(Lossless: false, Exact: false, Method: 4, Quality: QualityBackgroundAlbedo, SharpYuv: true)
        };

        ReadOnlySpan<byte> pixels = type switch
        {
            AssetSpriteLayerType.Normal => EncodeBackgroundNormals(layer.Pixels.Span),
            AssetSpriteLayerType.Depth => layer.Pixels.Span,
            AssetSpriteLayerType.Albedo => layer.Pixels.Span
        };

        return WebpEncoder.Encode(pixels, layer.PixelWidth, layer.PixelHeight, settings);
    }

    public static AssetAnimationMetadata FitBounds(
        AssetAnimationMetadata metadata,
        IReadOnlyList<AssetSpriteMetadata> source,
        IReadOnlyList<AssetSpriteMetadata> filtered)
    {
        AssetAnimationMetadata fitted = GetBounds(metadata, source);

        if (fitted == metadata)
        {
            return GetBounds(metadata, filtered);
        }
        else
        {
            return ExpandBounds(metadata, filtered);
        }
    }

    public static AssetAnimationMetadata ExpandBounds(
        AssetAnimationMetadata metadata,
        IReadOnlyList<AssetSpriteMetadata> frames)
    {
        int left = metadata.OffsetX;
        int top = metadata.OffsetY;
        int right = metadata.OffsetX + metadata.Width;
        int bottom = metadata.OffsetY + metadata.Height;

        foreach (AssetSpriteMetadata frame in frames)
        {
            left = Math.Min(left, frame.OffsetX);
            top = Math.Min(top, frame.OffsetY);
            right = Math.Max(right, frame.OffsetX + frame.Width);
            bottom = Math.Max(bottom, frame.OffsetY + frame.Height);
        }

        return metadata with { Width = right - left, Height = bottom - top, OffsetX = left, OffsetY = top };
    }

    private static AssetAnimationMetadata GetBounds(
        AssetAnimationMetadata metadata,
        IReadOnlyList<AssetSpriteMetadata> frames)
    {
        int left = int.MaxValue;
        int top = int.MaxValue;
        int right = int.MinValue;
        int bottom = int.MinValue;

        foreach (AssetSpriteMetadata frame in frames)
        {
            left = Math.Min(left, frame.OffsetX);
            top = Math.Min(top, frame.OffsetY);
            right = Math.Max(right, frame.OffsetX + frame.Width);
            bottom = Math.Max(bottom, frame.OffsetY + frame.Height);
        }

        return metadata with { Width = right - left, Height = bottom - top, OffsetX = left, OffsetY = top };
    }

    private static AssetPack.Writer.EncodedImage[] Save(
        IReadOnlyList<AssetSpriteMetadata> frames,
        IReadOnlyList<byte[]> images,
        WebpSettings settings)
    {
        AssetPack.Writer.EncodedImage[] encoded = new AssetPack.Writer.EncodedImage[frames.Count];

        for (int index = 0; index < frames.Count; index++)
        {
            AssetSpriteMetadata metadata = frames[index];

            encoded[index] = WebpEncoder.Encode(images[index], metadata.PixelWidth, metadata.PixelHeight, settings);
        }

        return encoded;
    }

    private static void CountColors(ReadOnlySpan<byte> pixels, HashSet<uint> colors)
    {
        if (colors.Count > PaletteColors)
        {
            return;
        }

        for (int offset = 0; offset < pixels.Length; offset += 4)
        {
            colors.Add(BinaryPrimitives.ReadUInt32LittleEndian(pixels[offset..]));

            if (colors.Count > PaletteColors)
            {
                return;
            }
        }
    }

    private static void QuantizeColors(byte[] pixels, WuQuantizer quantizer)
    {
        for (int offset = 0; offset < pixels.Length; offset += 4)
        {
            WuQuantizer.RgbaColor quantized = quantizer.GetQuantizedColor(
                pixels[offset], pixels[offset + 1], pixels[offset + 2], pixels[offset + 3]);

            pixels[offset] = quantized.Red;
            pixels[offset + 1] = quantized.Green;
            pixels[offset + 2] = quantized.Blue;
            pixels[offset + 3] = quantized.Alpha;
        }
    }

    private static IReadOnlyList<AssetSprite> Filter(
        IReadOnlyList<AssetSprite> frames,
        IReadOnlyList<ISpriteFilter> filters,
        SpriteFilterContext context)
    {
        List<AssetSprite> cleared = [];

        foreach (AssetSprite frame in frames)
        {
            byte[] pixels = [.. frame.Pixels.Span];

            if (!frame.Metadata.Additive)
            {
                ClearTransparentPixels(pixels);
            }

            cleared.Add(frame with { Pixels = pixels });
        }

        IReadOnlyList<AssetSprite> filtered = cleared;

        foreach (ISpriteFilter filter in filters)
        {
            filtered = filter.Apply(filtered, context);
        }

        return filtered;
    }

    private static byte[] EncodeSurfaceNormals(ReadOnlySpan<byte> pixels)
    {
        byte[] result = [.. pixels];

        for (int offset = 0; offset < result.Length; offset += 4)
        {
            if (result[offset + 3] == 0)
            {
                result[offset] = 0x80;
                result[offset + 1] = 0x80;
                result[offset + 2] = 0xff;
            }

            result[offset + 3] = 0xff;
        }

        return result;
    }

    private static byte[] EncodeBackgroundNormals(ReadOnlySpan<byte> pixels)
    {
        byte[] result = [.. pixels];
        int step = 1 << (8 - BackgroundNormalPrecisionBits);
        int mask = ~(step - 1);
        int half = step / 2;

        for (int offset = 0; offset < result.Length; offset += 4)
        {
            result[offset] = (byte)Math.Min((result[offset] + half) & mask, byte.MaxValue);
            result[offset + 1] = (byte)Math.Min((result[offset + 1] + half) & mask, byte.MaxValue);
            result[offset + 2] = (byte)Math.Min((result[offset + 2] + half) & mask, byte.MaxValue);
        }

        return result;
    }

    private static void ClearTransparentPixels(byte[] pixels)
    {
        for (int offset = 0; offset < pixels.Length; offset += 4)
        {
            if (pixels[offset + 3] == 0)
            {
                pixels[offset] = 0;
                pixels[offset + 1] = 0;
                pixels[offset + 2] = 0;
            }
        }
    }
}
