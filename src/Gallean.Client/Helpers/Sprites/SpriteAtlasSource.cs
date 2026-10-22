using Avalonia;
using SkiaSharp;

namespace Gallean.Client;

internal sealed class SpriteAtlasSource
{
    public abstract record AbstractSprite;

    public sealed record StaticSprite(
        AtlasTexture.RegionKey Region,
        double X,
        double Y,
        double Width) : AbstractSprite;

    public sealed record AnimatedSprite(
        IReadOnlyList<AtlasTexture.RegionKey> Regions,
        IReadOnlyList<Sprite> Frames,
        AssetAnimationMetadata Metadata,
        Point Anchor) : AbstractSprite;

    public sealed class Plan(IReadOnlyList<AbstractSprite> sprites, IReadOnlySet<AtlasTexture.RegionKey> regions)
    {
        public IReadOnlyList<AbstractSprite> Sprites { get; } = sprites;
        public IReadOnlySet<AtlasTexture.RegionKey> Regions { get; } = regions;
    }

    internal sealed record AnimatedCommand(
        int CommandIndex,
        IReadOnlyList<SKRect> Sources,
        IReadOnlyList<Sprite> Frames,
        AssetAnimationMetadata Metadata,
        Point Anchor,
        SKRect Bounds);

    private readonly IReadOnlyList<AnimatedCommand> animatedCommands;
    private readonly SKRect[] bounds;
    private readonly SKRect[] sources;
    private readonly AtlasTexture texture;
    private readonly SKRotationScaleMatrix[] transforms;

    private SpriteAtlasSource(
        AtlasTexture texture,
        SKRect[] sources,
        SKRotationScaleMatrix[] transforms,
        SKRect[] bounds,
        IReadOnlyList<AnimatedCommand> animatedCommands)
    {
        this.texture = texture;
        this.sources = sources;
        this.transforms = transforms;
        this.bounds = bounds;
        this.animatedCommands = animatedCommands;
    }

    public static Plan CreatePlan(IReadOnlyList<AbstractSprite> sprites)
    {
        HashSet<AtlasTexture.RegionKey> regions = [];

        foreach (AbstractSprite sprite in sprites)
        {
            switch (sprite)
            {
                case StaticSprite staticSprite:
                    regions.Add(staticSprite.Region);
                    break;

                case AnimatedSprite animatedSprite:
                    foreach (AtlasTexture.RegionKey region in animatedSprite.Regions)
                    {
                        regions.Add(region);
                    }
                    break;
            }
        }

        return new Plan(sprites, regions);
    }

    public static SpriteAtlasSource FromPlan(Plan plan, AtlasTexture texture)
    {
        IReadOnlyDictionary<AtlasTexture.RegionKey, SKRect> regionSources = texture.Regions;
        SKRect[] sources = new SKRect[plan.Sprites.Count];
        SKRotationScaleMatrix[] transforms = new SKRotationScaleMatrix[plan.Sprites.Count];
        SKRect[] bounds = new SKRect[plan.Sprites.Count];
        List<AnimatedCommand> animatedCommands = [];

        for (int index = 0; index < plan.Sprites.Count; index++)
        {
            switch (plan.Sprites[index])
            {
                case StaticSprite staticSprite:
                    SKRect staticSource = regionSources[staticSprite.Region];
                    sources[index] = staticSource;
                    transforms[index] = CreateTransform(staticSource, staticSprite.X, staticSprite.Y, staticSprite.Width);
                    bounds[index] = ComputeStaticBounds(staticSource, staticSprite.X, staticSprite.Y, staticSprite.Width);
                    break;

                case AnimatedSprite animatedSprite:
                    List<SKRect> animationSources = [];

                    foreach (AtlasTexture.RegionKey region in animatedSprite.Regions)
                    {
                        animationSources.Add(regionSources[region]);
                    }

                    Sprite firstFrame = animatedSprite.Frames[0];
                    SKRect firstSource = animationSources[0];
                    sources[index] = firstSource;
                    transforms[index] = CreateAnchoredTransform(firstSource, animatedSprite.Metadata, firstFrame, animatedSprite.Anchor);
                    SKRect animatedBounds = ComputeAnimatedBounds(animatedSprite.Metadata, animatedSprite.Anchor);
                    bounds[index] = animatedBounds;
                    animatedCommands.Add(new AnimatedCommand(index, animationSources, animatedSprite.Frames, animatedSprite.Metadata, animatedSprite.Anchor, animatedBounds));
                    break;
            }
        }

        return new SpriteAtlasSource(texture, sources, transforms, bounds, animatedCommands);
    }

    public SpriteAtlas CreateAtlas()
    {
        if (animatedCommands.Count > 0)
        {
            SKRect[] sourceSnapshot = [.. sources];
            SKRotationScaleMatrix[] transformSnapshot = [.. transforms];

            return new SpriteAtlas(this, texture, sourceSnapshot, transformSnapshot, bounds, animatedCommands);
        }
        else
        {
            return new SpriteAtlas(this, texture, sources, transforms, bounds, animatedCommands);
        }
    }

    private static SKRotationScaleMatrix CreateTransform(SKRect source, double x, double y, double width)
    {
        float scale = (float)(width / source.Width);

        return new SKRotationScaleMatrix(scale, 0, (float)x, (float)y);
    }

    public static SKRotationScaleMatrix CreateAnchoredTransform(SKRect source, AssetAnimationMetadata animation, Sprite frame, Point anchor)
    {
        double x = anchor.X - (animation.OffsetX + animation.Width / 2.0) + frame.Metadata.OffsetX;
        double y = anchor.Y - (animation.OffsetY + animation.Height) + frame.Metadata.OffsetY;

        return CreateTransform(source, x, y, frame.Metadata.Width);
    }

    private static SKRect ComputeAnimatedBounds(AssetAnimationMetadata animation, Point anchor)
    {
        double left = anchor.X - (animation.Width / 2.0);
        double top = anchor.Y - animation.Height;

        return new SKRect((float)left, (float)top, (float)(left + animation.Width), (float)(top + animation.Height));
    }

    private static SKRect ComputeStaticBounds(SKRect source, double x, double y, double width)
    {
        double height = source.Height * width / source.Width;

        return new SKRect((float)x, (float)y, (float)(x + width), (float)(y + height));
    }
}
