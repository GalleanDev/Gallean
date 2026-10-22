using System.Runtime.CompilerServices;
using SkiaSharp;

namespace Gallean.Client;

internal static class SkiaMemoryPressure
{
    private sealed class Token
    {
        private readonly long bytes;

        public Token(long bytes)
        {
            this.bytes = bytes;
            GC.AddMemoryPressure(bytes);
        }

        ~Token()
        {
            GC.RemoveMemoryPressure(bytes);
        }
    }

    private static readonly ConditionalWeakTable<SKObject, Token> Tokens = new();

    public static void TrackImage(SKImage image)
    {
        TrackObject(image, (long)image.Width * image.Height * 4);
    }

    private static void TrackObject(SKObject obj, long bytes)
    {
        Tokens.AddOrUpdate(obj, new Token(bytes));
    }
}
