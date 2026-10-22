using Gallean.Common;
using SkiaSharp;

namespace Gallean.Client.Tests;

internal sealed class TestAssetProviderBuilder
{
    private readonly CleanupList cleanup = new();
    private Action<AssetKey>? onRequest;
    private Func<AssetKey, Animation?>? resolveAnimation;
    private Func<AssetKey, AssetStream?>? resolveSound;
    private Func<AssetKey, Sprite?>? resolveSprite;

    public TestAssetProviderBuilder ResolveSprite(Func<AssetKey, Sprite?> resolver)
    {
        resolveSprite = resolver;
        return this;
    }

    public TestAssetProviderBuilder ResolveAnimation(Func<AssetKey, Animation?> resolver)
    {
        resolveAnimation = resolver;
        return this;
    }

    public TestAssetProviderBuilder ResolveSound(Func<AssetKey, AssetStream?> resolver)
    {
        resolveSound = resolver;
        return this;
    }

    public TestAssetProviderBuilder OnRequest(Action<AssetKey> handler)
    {
        onRequest = handler;
        return this;
    }

    public Sprite CreateSprite(SKColor color, int width, int height)
    {
        return CreateSprite(color, width, height, 0, 0);
    }

    public Sprite CreateSprite(SKColor color, int width, int height, int offsetX, int offsetY)
    {
        SKImageInfo info = new(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using SKSurface surface = SKSurface.Create(info);
        surface.Canvas.Clear(color);

        SKImage snapshot = surface.Snapshot();
        SKImage image = snapshot.ToRasterImage(ensurePixelData: true);
        Sprite sprite = new(new AssetSpriteMetadata(width, height, width, height, offsetX, offsetY, Additive: false), image, Normal: null, Depth: null, Albedo: null, Lights: []);
        cleanup.Add(sprite.Image);

        return sprite;
    }

    public Sprite CreateSprite(AssetSpriteMetadata metadata, byte[] pixels)
    {
        SKImageInfo info = new(metadata.PixelWidth, metadata.PixelHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
        Sprite sprite = new(metadata, SKImage.FromPixelCopy(info, pixels), Normal: null, Depth: null, Albedo: null, Lights: []);
        cleanup.Add(sprite.Image);

        return sprite;
    }

    public AssetStream CreateSound(params byte[] data)
    {
        return AssetStream.FromBytes(data);
    }

    public Animation CreateAnimation(AssetAnimationMetadata metadata, IReadOnlyList<Sprite> frames)
    {
        return new Animation(metadata, frames);
    }

    public TestAssetProvider Build()
    {
        return new TestAssetProvider(
            resolveSprite ?? throw new InvalidOperationException("A sprite resolver is required."),
            resolveAnimation ?? throw new InvalidOperationException("An animation resolver is required."),
            resolveSound ?? throw new InvalidOperationException("A sound resolver is required."),
            onRequest,
            cleanup);
    }
}
