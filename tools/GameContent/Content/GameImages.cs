namespace GameContent;

internal sealed class GameImages
{
    private readonly GameSourceCache cache;
    private readonly ImagesExtractor extractor;
    private readonly string fileName;

    private GameImages(ImagesExtractor extractor, string fileName, GameSourceCache cache)
    {
        this.extractor = extractor;
        this.fileName = fileName;
        this.cache = cache;
    }

    public static GameImages Load(string imagesRoot, string fileName, GameSourceCache cache)
    {
        string path = Path.Combine(imagesRoot, fileName);

        return new GameImages(ImagesExtractor.Load(path), fileName, cache);
    }

    public bool HasAnimation(string name)
    {
        return extractor.IsAnimationExists(name);
    }

    public IReadOnlyList<string> GetAllAnimationNames()
    {
        return extractor.GetAllAnimationNames();
    }

    public AssetSprite? TryGetSprite(string name)
    {
        return cache.GetOrAddArchiveSprite(fileName, name, LoadSprite);
    }

    public AssetSprite? TryGetCompositeSprite(string name)
    {
        IDictionary<string, RawBitmap>? parts = TryGetImageParts(name) ?? TryGetImageParts($"{name}.PNG");

        if (parts is not null)
        {
            return SpriteOperations.Composite([.. parts.Values]);
        }

        return TryGetSprite(name);
    }

    public AssetSprite? TryGetAnchoredSprite(string name)
    {
        return cache.GetOrAddArchiveAnchoredSprite(fileName, name, LoadAnchoredSprite);
    }

    public IReadOnlyList<AssetSprite> GetObjectSeries(string name)
    {
        return cache.GetOrAddArchiveObjectSeries(fileName, name, LoadObjectSeries);
    }

    public IReadOnlyList<AssetSprite>? TryGetAnimationFrames(string name)
    {
        if (!extractor.IsAnimationExists(name))
        {
            return null;
        }

        IReadOnlyCollection<RawBitmap>? raw = extractor.TryGetAnimationFrames(name);

        if (raw is null)
        {
            return null;
        }

        List<AssetSprite> frames = [];

        foreach (RawBitmap frame in raw)
        {
            frames.Add(ToFullCanvasSprite(frame));
        }

        return frames;
    }

    public IReadOnlyList<AssetSprite>? TryGetAnchoredAnimationFrames(string name)
    {
        if (!extractor.IsAnimationExists(name))
        {
            return null;
        }

        IReadOnlyCollection<RawBitmap>? raw = extractor.TryGetAnimationFrames(name);

        if (raw is null)
        {
            return null;
        }

        List<AssetSprite> frames = [];

        foreach (RawBitmap frame in raw)
        {
            frames.Add(ToAnchoredSprite(frame));
        }

        return frames;
    }

    public AssetAnimation? TryGetPositionedAnimation(
        string name,
        int canvasWidth,
        int canvasHeight,
        int offsetX,
        int offsetY)
    {
        if (!extractor.IsAnimationExists(name))
        {
            return null;
        }

        IReadOnlyCollection<RawBitmap>? raw = extractor.TryGetAnimationFrames(name);

        if (raw is null)
        {
            return null;
        }

        List<AssetSprite> frames = [];

        foreach (RawBitmap frame in raw)
        {
            frames.Add(SpriteOperations.ClipToCanvas(frame, canvasWidth, canvasHeight, offsetX, offsetY));
        }

        return new AssetAnimation(new AssetAnimationMetadata(frames.Count, canvasWidth, canvasHeight, 0, 0, TriggerFrame: null, SecondTriggerFrame: null), frames);
    }

    public IDictionary<string, RawBitmap>? TryGetImageParts(string name)
    {
        try
        {
            return extractor.GetImageParts(name);
        }
        catch (ResourceException)
        {
            return null;
        }
    }

    private AssetSprite? LoadSprite(string name)
    {
        if (extractor.IsAnimationExists(name))
        {
            return null;
        }

        RawBitmap? raw = TryGet(name) ?? TryGet($"{name}.PNG");

        if (raw is not null)
        {
            return ToFullCanvasSprite(raw);
        }
        else
        {
            return null;
        }
    }

    private AssetSprite? LoadAnchoredSprite(string name)
    {
        if (extractor.IsAnimationExists(name))
        {
            return null;
        }

        if (extractor.IsImageExists(name))
        {
            return ToAnchoredSprite(extractor.GetImage(name));
        }
        else
        {
            return null;
        }
    }

    private IReadOnlyList<AssetSprite> LoadObjectSeries(string name)
    {
        IDictionary<string, RawBitmap> parts;

        try
        {
            parts = extractor.GetImageParts(name);
        }
        catch (ResourceException)
        {
            return [];
        }

        List<AssetSprite> sprites = new(parts.Count);

        foreach (KeyValuePair<string, RawBitmap> part in parts.OrderBy(part => part.Key, StringComparer.Ordinal))
        {
            sprites.Add(ToAnchoredSprite(part.Value));
        }

        return sprites;
    }

    private RawBitmap? TryGet(string name)
    {
        try
        {
            return extractor.GetImage(name);
        }
        catch (ResourceException)
        {
            return null;
        }
    }

    private static AssetSprite ToFullCanvasSprite(RawBitmap bitmap)
    {
        int width = bitmap.OriginalWidth;
        int height = bitmap.OriginalHeight;

        if (bitmap.Bounds.X == 0 &&
            bitmap.Bounds.Y == 0 &&
            bitmap.Bounds.Width == width &&
            bitmap.Bounds.Height == height)
        {
            return new AssetSprite(
                new AssetSpriteMetadata(width, height, width, height, 0, 0, Additive: bitmap.Additive),
                bitmap.Data);
        }

        byte[] pixels = new byte[width * height * 4];

        for (int row = 0; row < bitmap.Bounds.Height; row++)
        {
            int targetY = bitmap.Bounds.Y + row;

            if (targetY < 0 || targetY >= height)
            {
                continue;
            }

            int sourceRow = row * bitmap.Bounds.Width * 4;
            int targetRow = (targetY * width + bitmap.Bounds.X) * 4;
            int copyLength = Math.Min(bitmap.Bounds.Width * 4, width * 4 - bitmap.Bounds.X * 4);

            if (copyLength > 0)
            {
                Buffer.BlockCopy(bitmap.Data, sourceRow, pixels, targetRow, copyLength);
            }
        }

        return new AssetSprite(
            new AssetSpriteMetadata(width, height, width, height, 0, 0, Additive: bitmap.Additive),
            pixels);
    }

    private static AssetSprite ToAnchoredSprite(RawBitmap raw)
    {
        AssetSprite content = SpriteOperations.ClipToCanvas(raw, raw.OriginalWidth, raw.OriginalHeight, 0, 0);

        return content.WithOffset(
            content.Metadata.OffsetX - (raw.OriginalWidth / 2),
            content.Metadata.OffsetY - (raw.OriginalHeight / 2));
    }
}
