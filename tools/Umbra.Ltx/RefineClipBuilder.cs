namespace Umbra.Ltx;

internal static class RefineClipBuilder
{
    public static IEnumerable<RefineClip> Build(IEnumerable<UmbraSequenceGroup> groups)
    {
        foreach (UmbraSequenceGroup group in groups)
        {
            List<UmbraFrame> frames = group.CollectFrames();
            List<int> cycleLengths = [];

            foreach (UmbraSequence sequence in group.Sequences)
            {
                if (sequence.Frames.Count > 0)
                {
                    cycleLengths.Add(sequence.Frames.Count);
                }
            }

            if (frames.Count > 0)
            {
                yield return BuildClip(group.Id, frames, cycleLengths);
            }
        }
    }

    private static RefineClip BuildClip(UmbraSequenceId id, IReadOnlyList<UmbraFrame> frames, IReadOnlyList<int> cycleLengths)
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

        List<RefineClipFrame> clipFrames = [];

        foreach (UmbraFrame frame in frames)
        {
            UmbraSpriteMetadata metadata = frame.Sprite.Metadata;
            long scale = metadata.PixelScale;

            clipFrames.Add(new RefineClipFrame(frame, (metadata.OffsetX * scale) - left, (metadata.OffsetY * scale) - top));
        }

        return new RefineClip(id, right - left, bottom - top, clipFrames, cycleLengths);
    }
}
