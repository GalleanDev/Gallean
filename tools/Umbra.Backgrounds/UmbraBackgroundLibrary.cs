using System.Buffers.Binary;
using System.Reflection;

namespace Umbra.Backgrounds;

public static partial class UmbraBackgroundLibrary
{
    private sealed record LayerMetadata(int PixelWidth, int PixelHeight);

    public static UmbraBackgroundImage? TryGet(string name)
    {
        if (!Names.Contains(name))
        {
            return null;
        }

        UmbraBackgroundLayer color = ReadLayer(name, "color");
        UmbraBackgroundLayer normal = ReadLayer(name, "normal");
        UmbraBackgroundLayer depth = ReadLayer(name, "depth");
        UmbraBackgroundLayer albedo = ReadLayer(name, "albedo");
        IReadOnlyList<UmbraBackgroundLayer> fluids = ReadFluidLayers(name);

        CheckDimensions(name, "normal", normal, color);
        CheckDimensions(name, "depth", depth, color);
        CheckDimensions(name, "albedo", albedo, color);

        for (int index = 0; index < fluids.Count; index++)
        {
            CheckDimensions(name, MakeFluidLayerName(index), fluids[index], color);
        }

        return new UmbraBackgroundImage(name, color, normal, depth, albedo, fluids);
    }

    private static string MakeFluidLayerName(int index)
    {
        return $"fluid{index}";
    }

    private static string MakeResourceName(string background, string name)
    {
        return $"{background}/{name}.webp";
    }

    private static IReadOnlyList<UmbraBackgroundLayer> ReadFluidLayers(string background)
    {
        List<UmbraBackgroundLayer> result = [];

        while (true)
        {
            string name = MakeFluidLayerName(result.Count);

            if (HasLayer(background, name))
            {
                result.Add(ReadLayer(background, name));
            }
            else
            {
                break;
            }
        }

        return result;
    }

    private static void CheckDimensions(string background, string name, UmbraBackgroundLayer layer, UmbraBackgroundLayer color)
    {
        if ((long)layer.PixelWidth * color.PixelHeight != (long)layer.PixelHeight * color.PixelWidth)
        {
            throw new InvalidDataException($"The image '{background}:{name}' has incorrect dimensions.");
        }
    }

    private static bool HasLayer(string background, string name)
    {
        Assembly assembly = typeof(UmbraBackgroundLibrary).Assembly;

        return assembly.GetManifestResourceInfo(MakeResourceName(background, name)) is not null;
    }

    private static UmbraBackgroundLayer ReadLayer(string background, string name)
    {
        Assembly assembly = typeof(UmbraBackgroundLibrary).Assembly;

        using Stream? stream = assembly.GetManifestResourceStream(MakeResourceName(background, name));

        if (stream is null)
        {
            throw new KeyNotFoundException($"Cannot find image '{background}:{name}'.");
        }

        byte[] data = new byte[stream.Length];
        stream.ReadExactly(data);

        LayerMetadata? metadata = TryGetMetadata(data);
        if (metadata is null)
        {
            throw new InvalidDataException($"The image '{background}:{name}' has unexpected format.");
        }

        return new UmbraBackgroundLayer(metadata.PixelWidth, metadata.PixelHeight, data);
    }

    private static LayerMetadata? TryGetMetadata(ReadOnlySpan<byte> data)
    {
        const byte losslessSignature = 0b101111;
        const int headerLength = 25;
        const int sizeOffset = 21;
        const int sizeMask = 0b11111111111111;

        if (data.Length < headerLength)
        {
            return null;
        }

        if (!data[..4].SequenceEqual("RIFF"u8))
        {
            return null;
        }

        if (!data[8..12].SequenceEqual("WEBP"u8))
        {
            return null;
        }

        if (!data[12..16].SequenceEqual("VP8L"u8))
        {
            return null;
        }

        if (data[20] != losslessSignature)
        {
            return null;
        }

        uint size = BinaryPrimitives.ReadUInt32LittleEndian(data[sizeOffset..]);
        int pixelWidth = (int)(size & sizeMask) + 1;
        int pixelHeight = (int)((size >> 14) & sizeMask) + 1;

        return new LayerMetadata(pixelWidth, pixelHeight);
    }
}
