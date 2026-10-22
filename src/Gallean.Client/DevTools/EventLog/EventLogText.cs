using System.Collections.Immutable;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;

namespace Gallean.Client;

internal sealed class EventLogText : AvaloniaObject
{
    public static readonly AttachedProperty<ImmutableArray<EventLogSpan>> SpansProperty =
        AvaloniaProperty.RegisterAttached<EventLogText, TextBlock, ImmutableArray<EventLogSpan>>("Spans");

    static EventLogText()
    {
        SpansProperty.Changed.AddClassHandler<TextBlock>(OnSpansChanged);
    }

    public static void SetSpans(TextBlock element, ImmutableArray<EventLogSpan> value)
    {
        element.SetValue(SpansProperty, value);
    }

    public static ImmutableArray<EventLogSpan> GetSpans(TextBlock element)
    {
        return element.GetValue(SpansProperty);
    }

    private static void OnSpansChanged(TextBlock textBlock, AvaloniaPropertyChangedEventArgs e)
    {
        ImmutableArray<EventLogSpan> spans = GetSpans(textBlock);

        if (!spans.IsDefault)
        {
            textBlock.Inlines = CreateInlines(spans);
        }
        else
        {
            textBlock.Inlines = null;
        }
    }

    private static InlineCollection CreateInlines(ImmutableArray<EventLogSpan> spans)
    {
        InlineCollection inlines = [];

        foreach (EventLogSpan span in spans)
        {
            Run run = new(span.Text);
            run.Classes.Add(GetClassName(span.Type));

            inlines.Add(run);
        }

        return inlines;
    }

    private static string GetClassName(EventLogSpanType type)
    {
        return type switch
        {
            EventLogSpanType.Text => "text",
            EventLogSpanType.Actor => "actor",
            EventLogSpanType.Object => "object",
            EventLogSpanType.Value => "value",
            EventLogSpanType.Reference => "reference",
            EventLogSpanType.Damage => "damage",
            EventLogSpanType.Healing => "healing"
        };
    }
}
