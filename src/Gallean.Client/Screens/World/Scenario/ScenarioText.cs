using System.Collections.Immutable;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;

namespace Gallean.Client;

internal sealed partial class ScenarioText : AvaloniaObject
{
    static ScenarioText()
    {
        SpansProperty.Changed.AddClassHandler<TextBlock>(OnSpansChanged);
    }

    [GeneratedAttachedProperty]
    public static partial ImmutableArray<ScenarioTextSpan> GetSpans(TextBlock element);

    private static void OnSpansChanged(TextBlock textBlock, AvaloniaPropertyChangedEventArgs e)
    {
        ImmutableArray<ScenarioTextSpan> spans = GetSpans(textBlock);

        if (!spans.IsDefault)
        {
            textBlock.Inlines = CreateInlines(spans);
        }
        else
        {
            textBlock.Inlines = null;
        }
    }

    private static InlineCollection CreateInlines(ImmutableArray<ScenarioTextSpan> spans)
    {
        InlineCollection inlines = [];

        foreach (ScenarioTextSpan span in spans)
        {
            Run run = new(span.Text);
            run.Classes.Add(GetClassName(span.Type));

            inlines.Add(run);
        }

        return inlines;
    }

    private static string GetClassName(ScenarioTextSpanType type)
    {
        return type switch
        {
            ScenarioTextSpanType.Text => "text",
            ScenarioTextSpanType.Character => "character",
            ScenarioTextSpanType.Place => "place",
            ScenarioTextSpanType.Faction => "faction",
            ScenarioTextSpanType.Item => "item"
        };
    }
}
