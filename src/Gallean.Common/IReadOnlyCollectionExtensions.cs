namespace Gallean.Common;

public static class IReadOnlyCollectionExtensions
{
    extension<T>(IReadOnlyCollection<T> self)
    {
        public bool SequenceEqual(IReadOnlyCollection<T> other)
        {
            if (self.Count != other.Count)
            {
                return false;
            }

            using IEnumerator<T> e1 = self.GetEnumerator();
            using IEnumerator<T> e2 = other.GetEnumerator();

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;

            while (e1.MoveNext())
            {
                if (!e2.MoveNext())
                {
                    return false;
                }

                if (!comparer.Equals(e1.Current, e2.Current))
                {
                    return false;
                }
            }

            if (e2.MoveNext())
            {
                return false;
            }

            return true;
        }

        public bool Contains(T item)
        {
            if (self is ICollection<T> collection)
            {
                return collection.Contains(item);
            }
            else
            {
                EqualityComparer<T> comparer = EqualityComparer<T>.Default;

                foreach (T element in self)
                {
                    if (comparer.Equals(element, item))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
