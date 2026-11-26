using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;

namespace Gallean.Client;

internal sealed partial class TextRuns : AvaloniaObject
{
    static TextRuns()
    {
        SpansProperty.Changed.AddClassHandler<TextBlock>(OnSpansChanged);
    }

    [GeneratedAttachedProperty]
    public static partial TextSpans? GetSpans(TextBlock element);

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
