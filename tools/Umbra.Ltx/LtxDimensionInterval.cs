namespace Umbra.Ltx;

internal readonly record struct LtxDimensionInterval(long Start, long End, long LeftRamp, long RightRamp)
{
    public long Length => End - Start;

    public static IReadOnlyList<LtxDimensionInterval> SplitBySize(long length, long size, long overlap)
    {
        if (size >= length)
        {
            return [new LtxDimensionInterval(0, length, 0, 0)];
        }

        long stride = size - overlap;

        if (stride <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(overlap));
        }

        List<LtxDimensionInterval> intervals = [];

        for (long start = 0; start < length; start += stride)
        {
            long end = Math.Min(start + size, length);
            long leftRamp = start > 0 ? overlap : 0;
            long rightRamp = end < length ? overlap : 0;

            intervals.Add(new LtxDimensionInterval(start, end, leftRamp, rightRamp));

            if (end >= length)
            {
                break;
            }
        }

        return intervals;
    }
}
