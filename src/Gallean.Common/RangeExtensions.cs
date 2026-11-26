namespace Gallean.Common;

public static class RangeExtensions
{
    extension(Range range)
    {
        public IEnumerator<int> GetEnumerator()
        {
            if (range.Start.IsFromEnd || range.End.IsFromEnd)
            {
                throw new ArgumentOutOfRangeException(nameof(range), range, null);
            }

            for (int value = range.Start.Value; value < range.End.Value; value++)
            {
                yield return value;
            }
        }
    }
}
