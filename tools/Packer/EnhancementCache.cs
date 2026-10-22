using Gallean.Common;
using ImageMagick;

namespace Packer;

internal sealed partial class EnhancementCache(string directory, UmbraDevice device)
{
    public bool Contains(Key key)
    {
        return File.Exists(GetPath(key));
    }

    public bool ContainsAll(IEnumerable<Key> keys)
    {
        foreach (Key key in keys)
        {
            if (!Contains(key))
            {
                return false;
            }
        }

        return true;
    }

    public Image Read(Key key)
    {
        using FileStream stream = File.OpenRead(GetPath(key));

        using MagickImage image = new(stream);
        image.Alpha(AlphaAction.Set);

        using IUnsafePixelCollection<byte> collection = image.GetPixelsUnsafe();

        return new Image((int)image.Width, (int)image.Height, collection.ToByteArray(PixelMapping.RGBA) ?? []);
    }

    public void Write(Key key, int pixelWidth, int pixelHeight, ReadOnlyMemory<byte> pixels)
    {
        PixelReadSettings settings = new((uint)pixelWidth, (uint)pixelHeight, StorageType.Char, PixelMapping.RGBA);

        using MagickImage image = new(pixels.Span, settings);
        image.Format = MagickFormat.Png32;
        image.Settings.SetDefine(MagickFormat.Png, "compression-level", "1");
        image.Settings.SetDefine(MagickFormat.Png, "compression-filter", "0");

        using AtomicFileWriter writer = new(GetPath(key));
        image.Write(writer.Stream);
        writer.Commit();
    }

    private string GetPath(Key key)
    {
        JoinStringBuilder name = new(".");

        name.Add(FormatDevice(device));
        name.Add(FormatHash(key.Hash.Value));
        name.Add(FormatToken(key.LayerType));
        name.Add(FormatEnhancements(key.Enhancements));
        name.Add(FormatToken(key.Upscaler));
        name.Add(FormatHash(key.PromptHash?.Value));

        return Path.Combine(directory, $"{name}.png");
    }

    private static string FormatDevice(UmbraDevice device)
    {
        return device switch
        {
            UmbraDevice.Cpu => "cpu",
            UmbraDevice.Gpu => "gpu"
        };
    }

    private static string FormatToken<TEnum>(TEnum? data) where TEnum : struct, Enum
    {
        if (data is not null)
        {
            string name = data.Value.ToString();

            return name.ToLowerInvariant();
        }
        else
        {
            return "none";
        }
    }

    private static string FormatEnhancements(IReadOnlyList<EnhancementType> enhancements)
    {
        string result = string.Join("+", enhancements);

        return result.ToLowerInvariant();
    }

    private static string FormatHash(ReadOnlyMemory<byte>? data)
    {
        if (data is not null)
        {
            return Convert.ToHexStringLower(data.Value.Span);
        }
        else
        {
            return "none";
        }
    }
}
