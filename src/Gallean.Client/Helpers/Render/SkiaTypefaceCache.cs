using System.Runtime.CompilerServices;
using Avalonia.Media;
using SkiaSharp;

namespace Gallean.Client;

internal static class SkiaTypefaceCache
{
    private static readonly ConditionalWeakTable<GlyphTypeface, SKTypeface> Typefaces = new();

    public static SKTypeface Get(GlyphTypeface glyphTypeface)
    {
        return Typefaces.GetValue(glyphTypeface, Load);
    }

    private static SKTypeface Load(GlyphTypeface glyphTypeface)
    {
        if (!glyphTypeface.PlatformTypeface.TryGetStream(out Stream? stream))
        {
            throw new InvalidOperationException($"Cannot read the font data of '{glyphTypeface.FamilyName}'.");
        }

        using (stream)
        {
            SKTypeface? typeface = SKTypeface.FromStream(stream);

            if (typeface is null)
            {
                throw new InvalidOperationException($"Cannot load the font data of '{glyphTypeface.FamilyName}'.");
            }

            return typeface;
        }
    }
}
