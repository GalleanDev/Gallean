using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;

namespace Gallean.Client;

internal sealed class TextRuns : AvaloniaObject
{
    public static readonly AttachedProperty<TextSpans?> SpansProperty =
        AvaloniaProperty.RegisterAttached<TextRuns, TextBlock, TextSpans?>("Spans");

    static TextRuns()
    {
        SpansProperty.Changed.AddClassHandler<TextBlock>(OnSpansChanged);
    }

    public static void SetSpans(TextBlock element, TextSpans? value)
    {
        element.SetValue(SpansProperty, value);
    }

    public static TextSpans? GetSpans(TextBlock element)
    {
        return element.GetValue(SpansProperty);
    }

    private static void OnSpansChanged(TextBlock textBlock, AvaloniaPropertyChangedEventArgs e)
    {
        TextSpans? text = GetSpans(textBlock);

        if (text is not null)
        {
            textBlock.Inlines = CreateInlines(text);
        }
        else
        {
            textBlock.Inlines = null;
        }
    }

    private static InlineCollection CreateInlines(TextSpans text)
    {
        InlineCollection inlines = [];

        foreach (TextSpan span in text.Spans)
        {
            Run run = new(span.Text);

            if (span.Style.HasFlag(TextStyle.Highlight))
            {
                run.Classes.Add("highlight");
            }

            if (span.Style.HasFlag(TextStyle.Bold))
            {
                run.Classes.Add("bold");
            }

            if (span.Style.HasFlag(TextStyle.Italic))
            {
                run.Classes.Add("italic");
            }

            inlines.Add(run);
        }

        return inlines;
    }
}
