using Gallean.Common;
using SkiaSharp;

namespace Gallean.Client;

internal static class AnimationHitMaskBuilder
{
    private const int AlphaThreshold = 64;
    private const int DilationRadius = 1;

    public static ImmutableBitmap Build(AssetAnimationMetadata metadata, IReadOnlyList<Sprite> frames)
    {
        List<ImmutableBitmap> frameMasks = new(frames.Count);

        foreach (Sprite frame in frames)
        {
            frameMasks.Add(BuildFrameMask(metadata, frame));
        }

        ImmutableBitmap.Builder builder = ImmutableBitmap.Create(metadata.Width, metadata.Height).ToBuilder();

        for (int row = 0; row < metadata.Height; row++)
        {
            for (int column = 0; column < metadata.Width; column++)
            {
                int opaqueFrames = 0;

                foreach (ImmutableBitmap frameMask in frameMasks)
                {
                    if (frameMask.Get(column, row))
                    {
                        opaqueFrames++;
                    }
                }

                if (opaqueFrames * 2 >= frameMasks.Count)
                {
                    builder.Set(column, row);
                }
            }
        }

        return builder.ToImmutable();
    }

    private static ImmutableBitmap BuildFrameMask(AssetAnimationMetadata metadata, Sprite frame)
    {
        ImmutableBitmap.Builder builder = ImmutableBitmap.Create(metadata.Width, metadata.Height).ToBuilder();

        using SKPixmap pixels = frame.Image.PeekPixels();
        ReadOnlySpan<byte> bytes = pixels.GetPixelSpan();

        for (int pixelRow = 0; pixelRow < frame.Metadata.PixelHeight; pixelRow++)
        {
            for (int pixelColumn = 0; pixelColumn < frame.Metadata.PixelWidth; pixelColumn++)
            {
                byte alpha = bytes[(pixelRow * pixels.RowBytes) + (pixelColumn * 4) + 3];

                if (alpha >= AlphaThreshold)
                {
                    int column = frame.Metadata.OffsetX - metadata.OffsetX + (pixelColumn / frame.Metadata.PixelScale);
                    int row = frame.Metadata.OffsetY - metadata.OffsetY + (pixelRow / frame.Metadata.PixelScale);

                    if (column >= 0 && column < metadata.Width && row >= 0 && row < metadata.Height)
                    {
                        builder.Set(column, row);
                    }
                }
            }
        }

        return Dilate(builder.ToImmutable());
    }

    private static ImmutableBitmap Dilate(ImmutableBitmap mask)
    {
        ImmutableBitmap.Builder builder = mask.ToBuilder();

        for (int row = 0; row < mask.Height; row++)
        {
            for (int column = 0; column < mask.Width; column++)
            {
                if (mask.Get(column, row))
                {
                    int top = Math.Max(0, row - DilationRadius);
                    int bottom = Math.Min(mask.Height - 1, row + DilationRadius);
                    int left = Math.Max(0, column - DilationRadius);
                    int right = Math.Min(mask.Width - 1, column + DilationRadius);

                    for (int y = top; y <= bottom; y++)
                    {
                        for (int x = left; x <= right; x++)
                        {
                            builder.Set(x, y);
                        }
                    }
                }
            }
        }

        return builder.ToImmutable();
    }
}
