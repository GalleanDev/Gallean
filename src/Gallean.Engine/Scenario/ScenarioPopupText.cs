using System.Text;

namespace Gallean.Engine;

public sealed record ScenarioPopupText(string? Speaker, string Speech)
{
    public static ScenarioPopupText Parse(string text)
    {
        StringBuilder nameData = new();
        StringBuilder lineData = new();
        int i = 0;

        text = text.Trim();

        for (; i < text.Length; i++)
        {
            if (text[i] is '\r' or '\n')
            {
                break;
            }

            if (text[i] == '"' && i == 0)
            {
                break;
            }

            nameData.Append(text[i]);
        }

        bool hasName = i < text.Length && text[i] != '"';

        for (; i < text.Length; i++)
        {
            lineData.Append(text[i]);
        }

        string name = nameData.ToString().Trim();
        string line = lineData.ToString().Trim();

        if (line.StartsWith('"') && (name.Length == 0 || hasName))
        {
            line = RemoveFinalQuote(line[1..]);

            string? speaker = name.Length > 0 ? name : null;

            return new ScenarioPopupText(speaker, line.Trim());
        }
        else
        {
            int quote = name.IndexOf('"');

            if (quote > 0)
            {
                string quotedLine = text[(quote + 1)..];
                string unquotedLine = RemoveFinalQuote(quotedLine);

                if (unquotedLine.Length < quotedLine.Length)
                {
                    string speaker = name[..quote].TrimEnd(' ', '\t', ':');

                    return new ScenarioPopupText(speaker, unquotedLine.Trim());
                }
                else
                {
                    return new ScenarioPopupText(Speaker: null, text);
                }
            }
            else
            {
                return new ScenarioPopupText(Speaker: null, text);
            }
        }
    }

    private static string RemoveFinalQuote(string line)
    {
        int index = line.LastIndexOf('"');

        if (index < 0)
        {
            return line;
        }

        for (int i = index + 1; i < line.Length; i++)
        {
            if (line[i] is '.' or '!' or '?' or '…')
            {
                continue;
            }
            else
            {
                return line;
            }
        }

        return line.Remove(index, 1);
    }
}
