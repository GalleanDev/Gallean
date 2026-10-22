namespace Umbra;

internal static class UmbraSequenceGroupExtensions
{
    public static List<UmbraFrame> CollectFrames(this UmbraSequenceGroup group)
    {
        List<UmbraFrame> frames = [];

        foreach (UmbraSequence sequence in group.Sequences)
        {
            frames.AddRange(sequence.Frames);
        }

        return frames;
    }

    public static List<UmbraSequence> CollectSequences(this IEnumerable<UmbraSequenceGroup> groups)
    {
        List<UmbraSequence> sequences = [];

        foreach (UmbraSequenceGroup group in groups)
        {
            sequences.AddRange(group.Sequences);
        }

        return sequences;
    }
}
