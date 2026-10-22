using System.Collections.Immutable;

namespace Gallean.Client.Texts;

public sealed record TextSpans(ImmutableArray<TextSpan> Spans)
{
    public static TextSpans Empty { get; } = new([]);

    public string Text => string.Concat(Spans.Select(span => span.Text));

    public static TextSpans FromText(string text)
    {
        return new TextSpans([new TextSpan(text, TextStyle.None)]);
    }

    public bool Equals(TextSpans? other)
    {
        if (other is null)
        {
            return false;
        }

        return Spans.SequenceEqual(other.Spans);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        foreach (TextSpan span in Spans)
        {
            hash.Add(span);
        }

        return hash.ToHashCode();
    }
}
