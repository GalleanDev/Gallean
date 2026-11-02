using System.Text;

namespace Gallean.Engine;

public sealed record ScenarioPopupText(string? Speaker, string Speech)
{
    public static ScenarioPopupText Parse(string text)
    {
        StringBuilder nameData = new();
        StringBuilder lineData = new();
        int i = 0;

        for (; i < text.Length; i++)
        {
            if (text[i] is '"' or '\r' or '\n')
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
            return new ScenarioPopupText(Speaker: null, text.Trim());
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
