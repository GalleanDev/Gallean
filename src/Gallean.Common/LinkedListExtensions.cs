using System.Diagnostics.CodeAnalysis;

namespace Gallean.Common;

public static class LinkedListExtensions
{
    extension<T>(LinkedList<T> self)
    {
        public void AddRangeFirst(IReadOnlyList<T> collection)
        {
            for (int i = collection.Count - 1; i >= 0; i--)
            {
                self.AddFirst(collection[i]);
            }
        }

        public bool TryRemoveFirst([MaybeNullWhen(false)] out T value)
        {
            if (self.First is not null)
            {
                value = self.First.Value;
                self.RemoveFirst();
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        }
    }
}
