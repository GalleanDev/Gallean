using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace Gallean.Client;

internal sealed class AutoFitTextBlock : OutlinedTextBlock
{
    static AutoFitTextBlock()
    {
        TextWrappingProperty.OverrideDefaultValue<AutoFitTextBlock>(TextWrapping.WrapWithOverflow);
    }

    protected override Type StyleKeyOverride => typeof(TextBlock);

    protected override TextLayout CreateTextLayout(string? text)
    {
        double maxWidth = GetMaxSizeFromConstraint().Width;
        double fontSize = GetFittedFontSize(text, maxWidth);

        return CreateTextLayout(text, fontSize, maxWidth);
    }

    private double GetFittedFontSize(string? text, double maxWidth)
    {
        using TextLayout layout = CreateTextLayout(text, FontSize, maxWidth);

        if (layout.Width > maxWidth && maxWidth > 0)
        {
            return FontSize * maxWidth / layout.Width;
        }
        else
        {
            return FontSize;
        }
    }

    private TextLayout CreateTextLayout(string? text, double fontSize, double maxWidth)
    {
        return new TextLayout(
            text,
            new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
            fontSize,
            Foreground,
            TextAlignment,
            TextWrapping,
            TextTrimming,
            TextDecorations,
            FlowDirection,
            maxWidth,
            lineHeight: LineHeight,
            letterSpacing: LetterSpacing,
            maxLines: MaxLines,
            fontFeatures: FontFeatures);
    }
}
