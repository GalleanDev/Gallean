namespace Umbra;

internal static class UpscaleClipBuilder
{
    public static IReadOnlyList<UpscaleClip> Build(IReadOnlyList<UmbraSequence> sequences)
    {
        List<UpscaleClip> clips = [];

        foreach (UmbraSequence sequence in sequences)
        {
            List<UmbraFrame> frames = [];
            HashSet<UmbraSpriteHash> frameHashes = [];

            foreach (UmbraFrame frame in sequence.Frames)
            {
                if (frameHashes.Add(frame.Hash))
                {
                    frames.Add(frame);
                }
            }

            if (frames.Count > 0)
            {
                clips.Add(BuildClip(frames));
            }
        }

        return clips;
    }

    private static UpscaleClip BuildClip(IReadOnlyList<UmbraFrame> frames)
    {
        long left = long.MaxValue;
        long top = long.MaxValue;
        long right = long.MinValue;
        long bottom = long.MinValue;

        foreach (UmbraFrame frame in frames)
        {
            UmbraSpriteMetadata metadata = frame.Sprite.Metadata;
            long x = (long)metadata.OffsetX * metadata.PixelScale;
            long y = (long)metadata.OffsetY * metadata.PixelScale;

            left = Math.Min(left, x);
            top = Math.Min(top, y);
            right = Math.Max(right, x + metadata.PixelWidth);
            bottom = Math.Max(bottom, y + metadata.PixelHeight);
        }

        List<UpscaleClipFrame> clipFrames = [];

        foreach (UmbraFrame frame in frames)
        {
            UmbraSpriteMetadata metadata = frame.Sprite.Metadata;
            long scale = metadata.PixelScale;

            clipFrames.Add(new UpscaleClipFrame(frame, (metadata.OffsetX * scale) - left, (metadata.OffsetY * scale) - top));
        }

        return new UpscaleClip(right - left, bottom - top, clipFrames);
    }
}
