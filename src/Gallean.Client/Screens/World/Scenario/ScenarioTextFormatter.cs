using System.Collections.Immutable;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed class ScenarioTextFormatter(IReadOnlyList<ScenarioName> names)
{
    private readonly ImmutableArray<ScenarioName> orderedNames = [.. names.OrderByDescending(name => name.Text.Length)];

    public ImmutableArray<ScenarioTextSpan> Format(string text)
    {
        ImmutableArray<ScenarioTextSpan>.Builder spans = ImmutableArray.CreateBuilder<ScenarioTextSpan>();
        int pos = 0;

        while (pos < text.Length)
        {
            ScenarioTextSpan span = ReadSpan(text, pos);

            spans.Add(span);
            pos += span.Text.Length;
        }

        return spans.ToImmutable();
    }

    private ScenarioTextSpan ReadSpan(string text, int pos)
    {
        ScenarioName? name = FindName(text, pos);

        if (name is not null)
        {
            string data = text.Substring(pos, name.Text.Length);

            return new ScenarioTextSpan(data, GetSpanType(name.Type));
        }
        else
        {
            int end = pos + 1;

            while (end < text.Length && FindName(text, end) is null)
            {
                end++;
            }

            return new ScenarioTextSpan(text[pos..end], ScenarioTextSpanType.Text);
        }
    }

    private ScenarioName? FindName(string text, int pos)
    {
        foreach (ScenarioName name in orderedNames)
        {
            if (IsName(text, pos, name.Text))
            {
                return name;
            }
        }

        return null;
    }

    private static bool IsName(string text, int pos, string name)
    {
        ReadOnlySpan<char> rest = text.AsSpan(pos);

        return char.IsUpper(text[pos]) &&
               rest.StartsWith(name, StringComparison.OrdinalIgnoreCase) &&
               IsWordBoundary(text, pos) &&
               IsWordBoundary(text, pos + name.Length);
    }

    private static bool IsWordBoundary(string text, int pos)
    {
        if (pos > 0 && pos < text.Length)
        {
            return !char.IsLetterOrDigit(text[pos - 1]) || !char.IsLetterOrDigit(text[pos]);
        }
        else
        {
            return true;
        }
    }

    private static ScenarioTextSpanType GetSpanType(ScenarioNameType type)
    {
        return type switch
        {
            ScenarioNameType.Character => ScenarioTextSpanType.Character,
            ScenarioNameType.Place => ScenarioTextSpanType.Place,
            ScenarioNameType.Faction => ScenarioTextSpanType.Faction,
            ScenarioNameType.Item => ScenarioTextSpanType.Item
        };
    }
}
