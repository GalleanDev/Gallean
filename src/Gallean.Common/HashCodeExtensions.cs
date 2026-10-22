namespace Gallean.Common;

public static class HashCodeExtensions
{
    extension(ref HashCode hash)
    {
        public void AddSequence<T>(IEnumerable<T> values)
        {
            foreach (T value in values)
            {
                hash.Add(value);
            }
        }
    }
}
