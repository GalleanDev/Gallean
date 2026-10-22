using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Gallean.Client;

[InterpolatedStringHandler]
internal readonly ref struct EventLogInterpolatedStringHandler
{
    private readonly ImmutableArray<EventLogSpan>.Builder spans;

    public EventLogInterpolatedStringHandler(int literalLength, int formattedCount)
    {
        spans = ImmutableArray.CreateBuilder<EventLogSpan>((2 * formattedCount) + 1);
    }

    public void AppendLiteral(string value)
    {
        spans.Add(new EventLogSpan(value, EventLogSpanType.Text));
    }

    public void AppendFormatted(EventLogSpan span)
    {
        spans.Add(span);
    }

    public void AppendFormatted(ImmutableArray<EventLogSpan> value)
    {
        spans.AddRange(value);
    }

    public ImmutableArray<EventLogSpan> ToImmutable()
    {
        return spans.ToImmutable();
    }
}
