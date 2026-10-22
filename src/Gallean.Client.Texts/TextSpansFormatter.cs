using System.Collections.Immutable;

namespace Gallean.Client.Texts;

internal sealed class TextSpansFormatter : IFormatProvider, ICustomFormatter
{
    private const char Placeholder = '\0';

    private readonly List<TextSpan> styledValues = [];

    public static TextSpans Format(string template, object?[] arguments)
    {
        TextSpansFormatter formatter = new();
        string[] literals = string.Format(formatter, template, arguments).Split(Placeholder);

        ImmutableArray<TextSpan>.Builder result = ImmutableArray.CreateBuilder<TextSpan>();

        for (int index = 0; index < literals.Length; index++)
        {
            if (literals[index].Length > 0)
            {
                result.Add(new TextSpan(literals[index], TextStyle.None));
            }

            if (index < formatter.styledValues.Count)
            {
                result.Add(formatter.styledValues[index]);
            }
        }

        return new TextSpans(result.ToImmutable());
    }

    public object? GetFormat(Type? formatType)
    {
        if (formatType == typeof(ICustomFormatter))
        {
            return this;
        }
        else
        {
            return null;
        }
    }

    public string Format(string? format, object? argument, IFormatProvider? formatProvider)
    {
        string text = argument?.ToString() ?? string.Empty;

        TextStyle style = ParseStyle(format);

        if (style != TextStyle.None)
        {
            styledValues.Add(new TextSpan(text, style));

            return new string([Placeholder]);
        }
        else
        {
            return text;
        }
    }

    private static TextStyle ParseStyle(string? format)
    {
        TextStyle style = TextStyle.None;

        foreach (char specifier in format ?? string.Empty)
        {
            style |= specifier switch
            {
                '*' => TextStyle.Highlight,
                'b' => TextStyle.Bold,
                'i' => TextStyle.Italic,
                _ => throw new FormatException($"Unexpected text style in format specifier '{format}'.")
            };
        }

        return style;
    }
}
