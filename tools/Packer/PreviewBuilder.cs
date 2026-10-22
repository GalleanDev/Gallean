namespace Packer;

internal sealed partial class PreviewBuilder(AssetPack pack)
{
    private const string OutputExtension = ".webp";

    private const int CheckerSize = 16;

    private static readonly byte[] BackgroundCheckerDark = [38, 38, 38, 255];
    private static readonly byte[] BackgroundCheckerLight = [140, 140, 140, 255];
    private static readonly byte[] BackgroundAdditive = [0, 0, 0, 255];

    public PreviewBuilderResult Build(PreviewBuilderOptions options)
    {
        List<PackImageEntry> entries = SelectEntries(options.Categories);

        if (options.Categories is not null)
        {
            IReadOnlyList<string> unmatched = options.Categories.UnmatchedPatterns(GetIds(entries));

            if (unmatched.Count > 0)
            {
                return new PreviewBuilderResult.UnmatchedCategories(unmatched);
            }
        }

        if (entries.Count <= 0)
        {
            return new PreviewBuilderResult.NoSelection();
        }

        if (entries.Count > 1 && IsFilePath(options.OutputPath))
        {
            return new PreviewBuilderResult.AmbiguousOutput(options.OutputPath, GetIds(entries));
        }

        EnhancementCache cache = new(options.CacheDirectory, options.Device);
        List<ResolvedEntry> resolved = [];

        foreach (PackImageEntry entry in entries)
        {
            resolved.Add(Resolve(entry, cache, options.Transform));
        }

        List<string> missing = GetMissingFrames(resolved);

        if (missing.Count > 0)
        {
            return new PreviewBuilderResult.MissingFrames(missing);
        }

        return new PreviewBuilderResult.Written(Write(resolved, cache, options));
    }

    private static bool IsFilePath(string outputPath)
    {
        string extension = Path.GetExtension(outputPath);

        return extension.Equals(OutputExtension, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetOutputPath(string outputPath, string id)
    {
        if (IsFilePath(outputPath))
        {
            return outputPath;
        }
        else
        {
            return Path.Combine(outputPath, $"{id.Replace('/', '-')}{OutputExtension}");
        }
    }

    private static void CreateOutputDirectory(string outputPath)
    {
        if (IsFilePath(outputPath))
        {
            string? directory = Path.GetDirectoryName(outputPath);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
        else
        {
            Directory.CreateDirectory(outputPath);
        }
    }

    private static List<string> GetIds(IReadOnlyList<PackImageEntry> entries)
    {
        List<string> ids = [];

        foreach (PackImageEntry entry in entries)
        {
            ids.Add(entry.Id);
        }

        return ids;
    }

    private static List<string> GetMissingFrames(IReadOnlyList<ResolvedEntry> entries)
    {
        List<string> missing = [];

        foreach (ResolvedEntry entry in entries)
        {
            missing.AddRange(entry.MissingFrames);
        }

        return missing;
    }

    private static UmbraPromptHash? GetPromptHash(IReadOnlyList<EnhancementType> enhancements, UmbraPromptHash promptHash)
    {
        if (enhancements.Contains(EnhancementType.Refine))
        {
            return promptHash;
        }
        else
        {
            return null;
        }
    }

    private static (EnhancementType Enhancement, AssetSpriteLayerType? LayerType) GetOutput(PreviewTransformType type)
    {
        return type switch
        {
            PreviewTransformType.Upscale => (EnhancementType.Upscale, null),
            PreviewTransformType.Refine => (EnhancementType.Refine, null),
            PreviewTransformType.Alpha => (EnhancementType.Alpha, null),
            PreviewTransformType.Normal => (EnhancementType.Surface, AssetSpriteLayerType.Normal)
        };
    }

    private static EnhancementCache.Key? TryResolveKey(
        EnhancementCache cache,
        PreviewTransformType? transform,
        IReadOnlyList<EnhancementType> enhancements,
        UpscalerType? upscaler,
        UmbraSpriteHash hash,
        UmbraPromptHash promptHash)
    {
        if (transform is not null)
        {
            (EnhancementType enhancement, AssetSpriteLayerType? layerType) = GetOutput(transform.Value);

            if (enhancements.Contains(enhancement))
            {
                IReadOnlyList<EnhancementType> outputEnhancements = EnhancementMap.GetOutputEnhancements(enhancements, enhancement);
                EnhancementCache.Key requested = new(outputEnhancements, upscaler, hash, layerType, GetPromptHash(outputEnhancements, promptHash));

                if (cache.Contains(requested))
                {
                    return requested;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
        else
        {
            for (int index = enhancements.Count - 1; index >= 0; index--)
            {
                EnhancementType enhancement = enhancements[index];

                if (enhancement != EnhancementType.Surface)
                {
                    IReadOnlyList<EnhancementType> outputEnhancements = EnhancementMap.GetOutputEnhancements(enhancements, enhancement);
                    EnhancementCache.Key key = new(outputEnhancements, upscaler, hash, null, GetPromptHash(outputEnhancements, promptHash));

                    if (cache.Contains(key))
                    {
                        return key;
                    }
                }
            }

            return null;
        }
    }

    private static List<EnhancementCache.Image> ReadImages(ResolvedEntry entry, EnhancementCache cache)
    {
        Dictionary<EnhancementCache.Key, EnhancementCache.Image> read = [];
        List<EnhancementCache.Image> images = [];

        foreach (EnhancementCache.Key key in entry.FrameKeys)
        {
            if (!read.TryGetValue(key, out EnhancementCache.Image? image))
            {
                image = cache.Read(key);
                read[key] = image;
            }

            images.Add(image);
        }

        return images;
    }

    private static FrameCanvas GetCanvas(
        IReadOnlyList<AssetSpriteMetadata> frameMetadata,
        IReadOnlyList<EnhancementCache.Image> images)
    {
        List<(int Left, int Top)> offsets = [];
        int left = int.MaxValue;
        int top = int.MaxValue;
        int right = int.MinValue;
        int bottom = int.MinValue;

        for (int index = 0; index < images.Count; index++)
        {
            AssetSpriteMetadata metadata = frameMetadata[index];
            EnhancementCache.Image image = images[index];
            int scale = Math.Max(1, image.PixelWidth / Math.Max(1, metadata.Width));
            int frameLeft = metadata.OffsetX * scale;
            int frameTop = metadata.OffsetY * scale;

            offsets.Add((frameLeft, frameTop));
            left = Math.Min(left, frameLeft);
            top = Math.Min(top, frameTop);
            right = Math.Max(right, frameLeft + image.PixelWidth);
            bottom = Math.Max(bottom, frameTop + image.PixelHeight);
        }

        List<FramePlacement> placements = [];

        foreach ((int Left, int Top) offset in offsets)
        {
            placements.Add(new FramePlacement(offset.Left - left, offset.Top - top));
        }

        return new FrameCanvas(right - left, bottom - top, placements);
    }

    private static byte Blend(byte destination, byte source, byte alpha, bool additive)
    {
        if (additive)
        {
            int result = source + ((destination * (byte.MaxValue - alpha)) / byte.MaxValue);

            return (byte)Math.Min(result, byte.MaxValue);
        }
        else
        {
            return (byte)(((source * alpha) + (destination * (byte.MaxValue - alpha))) / byte.MaxValue);
        }
    }

    private static byte[] DrawFrame(
        FrameCanvas canvas,
        FramePlacement placement,
        EnhancementCache.Image image,
        bool additive)
    {
        byte[] pixels = new byte[canvas.PixelWidth * canvas.PixelHeight * 4];

        FillBackground(pixels, canvas, additive);

        for (int y = 0; y < image.PixelHeight; y++)
        {
            for (int x = 0; x < image.PixelWidth; x++)
            {
                int source = ((y * image.PixelWidth) + x) * 4;
                int target = (((placement.Top + y) * canvas.PixelWidth) + placement.Left + x) * 4;
                byte alpha = image.Pixels[source + 3];

                pixels[target] = Blend(pixels[target], image.Pixels[source], alpha, additive);
                pixels[target + 1] = Blend(pixels[target + 1], image.Pixels[source + 1], alpha, additive);
                pixels[target + 2] = Blend(pixels[target + 2], image.Pixels[source + 2], alpha, additive);
            }
        }

        return pixels;
    }

    private static void FillBackground(byte[] pixels, FrameCanvas canvas, bool additive)
    {
        for (int y = 0; y < canvas.PixelHeight; y++)
        {
            for (int x = 0; x < canvas.PixelWidth; x++)
            {
                int offset = (((y * canvas.PixelWidth) + x) * 4);

                if (additive)
                {
                    BackgroundAdditive.CopyTo(pixels, offset);
                }
                else if (((x / CheckerSize) + (y / CheckerSize)) % 2 == 0)
                {
                    BackgroundCheckerLight.CopyTo(pixels, offset);
                }
                else
                {
                    BackgroundCheckerDark.CopyTo(pixels, offset);
                }
            }
        }
    }

    private static byte[] Compose(ResolvedEntry entry, EnhancementCache cache, TimeSpan frameDuration)
    {
        List<EnhancementCache.Image> images = ReadImages(entry, cache);
        FrameCanvas canvas = GetCanvas(entry.FrameMetadata, images);
        List<byte[]> frames = [];

        for (int index = 0; index < images.Count; index++)
        {
            frames.Add(DrawFrame(canvas, canvas.Placements[index], images[index], entry.FrameMetadata[index].Additive));
        }

        return WebpEncoder.EncodeAnimation(
            frames,
            canvas.PixelWidth,
            canvas.PixelHeight,
            frameDuration,
            new WebpSettings(Lossless: true, Exact: true, Method: 4, Quality: 100, SharpYuv: false));
    }

    private static List<string> Write(
        IReadOnlyList<ResolvedEntry> entries,
        EnhancementCache cache,
        PreviewBuilderOptions options)
    {
        CreateOutputDirectory(options.OutputPath);

        List<string> outputPaths = [];

        foreach (ResolvedEntry entry in entries)
        {
            string outputPath = GetOutputPath(options.OutputPath, entry.Id);

            File.WriteAllBytes(outputPath, Compose(entry, cache, options.FrameDuration));
            outputPaths.Add(outputPath);
        }

        return outputPaths;
    }

    private List<PackImageEntry> SelectEntries(CategorySelector? categories)
    {
        List<PackImageEntry> entries = [];

        foreach (PackImageEntry entry in PackImageEntry.Enumerate(pack))
        {
            if (entry.FrameMetadata.Count > 0 && (categories is null || categories.Matches(entry.Id)))
            {
                entries.Add(entry);
            }
        }

        return entries;
    }

    private ResolvedEntry Resolve(PackImageEntry entry, EnhancementCache cache, PreviewTransformType? transform)
    {
        IReadOnlyList<AssetSprite> frames = entry.ReadFrames(pack);
        List<AssetSpriteMetadata> frameMetadata = [];
        List<EnhancementCache.Key> frameKeys = [];
        List<string> missingFrames = [];
        IReadOnlyList<EnhancementType> enhancements = EnhancementMap.GetEnhancements(entry.Key);
        UpscalerType? upscaler = null;

        if (enhancements.Contains(EnhancementType.Upscale))
        {
            upscaler = UpscaleMap.GetUpscaler(entry.Key);
        }

        for (int index = 0; index < frames.Count; index++)
        {
            AssetSprite frame = frames[index];
            UmbraSpriteHash hash = frame.ToHash();
            EnhancementCache.Key? key = TryResolveKey(cache, transform, enhancements, upscaler, hash, RefinePromptHash.Get(entry.Key));

            if (key is not null)
            {
                frameMetadata.Add(frame.Metadata);
                frameKeys.Add(key.Value);
            }
            else
            {
                missingFrames.Add($"{entry.Id}#{index}");
            }
        }

        return new ResolvedEntry(entry.Id, frameMetadata, frameKeys, missingFrames);
    }
}
