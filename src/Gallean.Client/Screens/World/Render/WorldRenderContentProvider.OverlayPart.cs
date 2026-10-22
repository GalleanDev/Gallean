using SkiaSharp;

namespace Gallean.Client;

internal sealed partial class WorldRenderContentProvider
{
    private sealed class OverlayPart<TSource, TKey>
        where TSource : class
        where TKey : notnull
    {
        private TSource? cachedSource;
        private Dictionary<TKey, SKImage> current = [];
        private Dictionary<TKey, SKImage> previous = [];

        public SKImage Resolve(TSource source, TKey key, Func<SKImage> buildFunc)
        {
            if (!ReferenceEquals(cachedSource, source))
            {
                previous = current;
                current = [];
                cachedSource = source;
            }

            if (current.TryGetValue(key, out SKImage? image))
            {
                return image;
            }

            if (!previous.TryGetValue(key, out image))
            {
                image = buildFunc();
                SkiaMemoryPressure.TrackImage(image);
            }

            current[key] = image;

            return image;
        }
    }
}
