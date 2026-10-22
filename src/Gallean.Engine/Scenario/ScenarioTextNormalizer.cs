using System.Text;

namespace Gallean.Engine;

internal static class ScenarioTextNormalizer
{
    public static string? Normalize(string? text)
    {
        if (text is not null)
        {
            return NormalizeInternal(text);
        }
        else
        {
            return null;
        }
    }

    private static string? NormalizeInternal(string text)
    {
        StringBuilder result = new(text.Length);
        bool hasSpace = false;
        int lineBreaks = 0;

        foreach (char ch in text.ReplaceLineEndings("\n"))
        {
            if (ch == '\n')
            {
                lineBreaks++;
            }
            else if (char.IsWhiteSpace(ch))
            {
                hasSpace = true;
            }
            else if (char.IsControl(ch))
            {
                continue;
            }
            else
            {
                if (result.Length > 0)
                {
                    if (lineBreaks > 0)
                    {
                        result.Append('\n', lineBreaks);
                    }
                    else if (hasSpace)
                    {
                        result.Append(' ');
                    }
                }

                result.Append(ch);
                lineBreaks = 0;
                hasSpace = false;
            }
        }

        if (result.Length > 0)
        {
            return result.ToString();
        }
        else
        {
            return null;
        }
    }
}
