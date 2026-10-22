using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace Gallean.Client;

internal sealed class TextLines : AvaloniaObject
{
    public static readonly AttachedProperty<int> MinLinesProperty =
        AvaloniaProperty.RegisterAttached<TextLines, TextBlock, int>("MinLines");

    static TextLines()
    {
        MinLinesProperty.Changed.AddClassHandler<TextBlock>(OnMinLinesChanged);
        TextBlock.FontFamilyProperty.Changed.AddClassHandler<TextBlock>(OnFontChanged);
        TextBlock.FontSizeProperty.Changed.AddClassHandler<TextBlock>(OnFontChanged);
        TextBlock.FontStyleProperty.Changed.AddClassHandler<TextBlock>(OnFontChanged);
        TextBlock.FontWeightProperty.Changed.AddClassHandler<TextBlock>(OnFontChanged);
        TextBlock.FontStretchProperty.Changed.AddClassHandler<TextBlock>(OnFontChanged);
        TextBlock.FontFeaturesProperty.Changed.AddClassHandler<TextBlock>(OnFontChanged);
        TextBlock.LineHeightProperty.Changed.AddClassHandler<TextBlock>(OnFontChanged);
        TextBlock.LetterSpacingProperty.Changed.AddClassHandler<TextBlock>(OnFontChanged);
        TextBlock.PaddingProperty.Changed.AddClassHandler<TextBlock>(OnFontChanged);
    }

    public static void SetMinLines(TextBlock element, int value)
    {
        element.SetValue(MinLinesProperty, value);
    }

    public static int GetMinLines(TextBlock element)
    {
        return element.GetValue(MinLinesProperty);
    }

    private static void OnMinLinesChanged(TextBlock textBlock, AvaloniaPropertyChangedEventArgs e)
    {
        int lines = GetMinLines(textBlock);

        if (lines > 0)
        {
            textBlock.SetCurrentValue(Layoutable.MinHeightProperty, MeasureLines(textBlock, lines));
        }
        else
        {
            textBlock.SetCurrentValue(Layoutable.MinHeightProperty, 0.0);
        }
    }

    private static void OnFontChanged(TextBlock textBlock, AvaloniaPropertyChangedEventArgs e)
    {
        int lines = GetMinLines(textBlock);

        if (lines <= 0)
        {
            return;
        }

        textBlock.SetCurrentValue(Layoutable.MinHeightProperty, MeasureLines(textBlock, lines));
    }

    private static double MeasureLines(TextBlock textBlock, int lines)
    {
        Typeface typeface = new(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch);
        GenericTextRunProperties runProperties = new(typeface, textBlock.FontSize, fontFeatures: textBlock.FontFeatures);
        GenericTextParagraphProperties paragraphProperties = new(
            FlowDirection.LeftToRight,
            TextAlignment.Left,
            firstLineInParagraph: true,
            alwaysCollapsible: false,
            runProperties,
            TextWrapping.NoWrap,
            textBlock.LineHeight,
            indent: 0,
            textBlock.LetterSpacing);

        using TextLayout layout = new(new EmptyLineSource(lines), paragraphProperties);

        return Math.Ceiling(layout.Height) + textBlock.Padding.Top + textBlock.Padding.Bottom;
    }
}
