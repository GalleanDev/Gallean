using System.Collections.Frozen;

namespace Gallean.Client.Texts;

public sealed class TextCatalog(IEnumerable<KeyValuePair<string, string>> translations)
{
    private readonly FrozenDictionary<string, string> entries = translations.ToFrozenDictionary(StringComparer.Ordinal);

    internal static TextCatalog Current { get; private set; } = new([]);

    public static string Tr(string text)
    {
        return Current.Translate(text);
    }

    public static string Tr(string text, object? argument)
    {
        return string.Format(Current.Translate(text), argument);
    }

    public static string Tr(string text, object? first, object? second)
    {
        return string.Format(Current.Translate(text), first, second);
    }

    public static string Tr(string text, object? first, object? second, object? third)
    {
        return string.Format(Current.Translate(text), first, second, third);
    }

    public static string Tr(string text, params object?[] arguments)
    {
        return string.Format(Current.Translate(text), arguments);
    }

    public static TextSpans TrSpans(string text, params object?[] arguments)
    {
        return TextSpansFormatter.Format(Current.Translate(text), arguments);
    }

    public void Activate()
    {
        Current = this;
    }

    public string Translate(string text)
    {
        return entries.GetValueOrDefault(text, text);
    }
}
