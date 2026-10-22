using System.Text;

namespace Gallean.Client;

internal static class ScenarioFormatter
{
    private static IReadOnlyList<string> ObjectiveLabels =>
    [
        Tr("{0} defined", Tr("Scenario objective")),
        Tr("No scenario objective"),
        Tr("Scenario objective"),
        Tr("New objective"),
        Tr("Objective")
    ];

    public static string FormatObjective(string text)
    {
        string[] lines = text.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            lines[i] = RemoveObjectiveLabel(lines[i]);
        }

        string objective = string.Join('\n', lines);

        return objective.Trim();
    }

    public static string FormatBriefing(string text)
    {
        StringBuilder sb = new(text.Length);
        int lineBreaks = 0;

        foreach (char ch in text)
        {
            if (ch == '\n')
            {
                lineBreaks++;
            }
            else
            {
                sb.Append(FormatLineBreaks(lineBreaks, ch));
                sb.Append(ch);
                lineBreaks = 0;
            }
        }

        sb = sb.Replace("...", "…");

        return sb.ToString();
    }

    private static string FormatLineBreaks(int lineBreaks, char next)
    {
        return lineBreaks switch
        {
            0 => string.Empty,
            1 when char.IsLower(next) => " ",
            1 => "\n",
            _ => "\n\n"
        };
    }

    private static string RemoveObjectiveLabel(string line)
    {
        string content = line.TrimStart();

        foreach (string label in ObjectiveLabels)
        {
            if (content.StartsWith(label, StringComparison.OrdinalIgnoreCase))
            {
                string rest = content[label.Length..].TrimStart();

                if (rest.StartsWith(':'))
                {
                    return rest[1..].TrimStart();
                }

                if (rest.Length > 0 && char.IsUpper(rest[0]))
                {
                    return rest;
                }
            }
        }

        return line;
    }
}
