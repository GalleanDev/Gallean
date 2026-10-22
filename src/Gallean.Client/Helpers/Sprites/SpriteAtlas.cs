using System.Buffers;
using Avalonia;
using Avalonia.Skia;
using SkiaSharp;

namespace Gallean.Client;

internal sealed class SpriteAtlas(
    SpriteAtlasSource source,
    AtlasTexture texture,
    SKRect[] sources,
    SKRotationScaleMatrix[] transforms,
    SKRect[] bounds,
    IReadOnlyList<SpriteAtlasSource.AnimatedCommand> animatedCommands)
{
    public SpriteAtlasSource Source { get; } = source;

    public int SpriteCount => sources.Length;

    public void UpdateAnimations(int frameIndex, Rect visible)
    {
        SKRect visibleRect = visible.ToSKRect();

        foreach (SpriteAtlasSource.AnimatedCommand command in animatedCommands)
        {
            if (visibleRect.IntersectsWith(command.Bounds))
            {
                int loopedIndex = frameIndex % command.Frames.Count;
                Sprite frame = command.Frames[loopedIndex];
                SKRect frameSource = command.Sources[loopedIndex];
                sources[command.CommandIndex] = frameSource;
                transforms[command.CommandIndex] = SpriteAtlasSource.CreateAnchoredTransform(frameSource, command.Metadata, frame, command.Anchor);
            }
        }
    }

    public void Draw(SKCanvas canvas, SKPaint paint, Rect visible, SKColor[]? tints = null)
    {
        if (sources.Length <= 0)
        {
            return;
        }

        SKRect visibleRect = visible.ToSKRect();

        canvas.DrawAtlas(
            texture.Image,
            sources,
            transforms,
            tints,
            SKBlendMode.Modulate,
            RenderSamplingOptions.Linear,
            visibleRect,
            paint);
    }

    public void DrawRange(SKCanvas canvas, SKPaint paint, Rect visible, int spriteStart, int spriteCount)
    {
        if (spriteCount <= 0)
        {
            return;
        }

        int spriteEnd = spriteStart + spriteCount;
        SKRect cullRect = visible.ToSKRect();

        int visibleCount = 0;
        for (int index = spriteStart; index < spriteEnd; index++)
        {
            if (cullRect.IntersectsWith(bounds[index]))
            {
                visibleCount++;
            }
        }

        if (visibleCount <= 0)
        {
            return;
        }

        SKRect[] visibleSources = ArrayPool<SKRect>.Shared.Rent(visibleCount);
        SKRotationScaleMatrix[] visibleTransforms = ArrayPool<SKRotationScaleMatrix>.Shared.Rent(visibleCount);

        try
        {
            int cursor = 0;

            for (int index = spriteStart; index < spriteEnd; index++)
            {
                if (cullRect.IntersectsWith(bounds[index]))
                {
                    visibleSources[cursor] = sources[index];
                    visibleTransforms[cursor] = transforms[index];
                    cursor++;
                }
            }

            SkiaApi.DrawAtlas(
                canvas,
                texture.Image,
                visibleTransforms.AsSpan(0, visibleCount),
                visibleSources.AsSpan(0, visibleCount),
                SKBlendMode.Modulate,
                RenderSamplingOptions.Linear,
                cullRect,
                paint);
        }
        finally
        {
            ArrayPool<SKRect>.Shared.Return(visibleSources);
            ArrayPool<SKRotationScaleMatrix>.Shared.Return(visibleTransforms);
        }
    }
}
