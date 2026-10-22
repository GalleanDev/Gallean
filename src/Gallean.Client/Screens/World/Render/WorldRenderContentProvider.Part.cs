namespace Gallean.Client;

internal sealed partial class WorldRenderContentProvider
{
    private sealed class Part<TSource, TValue>
        where TSource : class
        where TValue : class
    {
        private TSource? cachedSource;
        private TValue? cachedValue;

        public TValue Resolve(TSource source, Func<TSource, TValue> buildFunc)
        {
            if (cachedValue is not null && ReferenceEquals(cachedSource, source))
            {
                return cachedValue;
            }

            TValue value = buildFunc(source);

            cachedSource = source;
            cachedValue = value;

            return value;
        }
    }

    private sealed class Part<TSource, TKey, TValue>(Func<TSource, TKey> keyFunc)
        where TSource : class
        where TValue : class
    {
        private TKey? cachedKey;
        private TSource? cachedSource;
        private TValue? cachedValue;

        public TValue Resolve(TSource source, Func<TSource, TValue> buildFunc)
        {
            if (cachedValue is not null && ReferenceEquals(cachedSource, source))
            {
                return cachedValue;
            }

            TKey key = keyFunc(source);

            if (cachedValue is not null && EqualityComparer<TKey>.Default.Equals(cachedKey, key))
            {
                cachedSource = source;
                return cachedValue;
            }

            TValue value = buildFunc(source);

            cachedSource = source;
            cachedKey = key;
            cachedValue = value;

            return value;
        }
    }
}
