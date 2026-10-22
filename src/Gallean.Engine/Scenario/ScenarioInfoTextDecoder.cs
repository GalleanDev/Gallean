namespace Gallean.Engine;

internal static class ScenarioInfoTextDecoder
{
    public const string NameTextId = "NAME";
    public const string DescriptionTextId = "DESC";
    public const string ObjectiveTextId = "BRIEFING";
    public const string BriefingTextId = "BRIEFLONG";
    public const string WinDebriefingTextId = "DEBUNKW";
    public const string LoseDebriefingTextId = "DEBUNKL";
    public const string CreatorTextId = "CREATOR";

    public static IEnumerable<Db.TextScenario> DecodeTexts(Db.ScenarioInfo info)
    {
        string briefing = NormalizeParts(
            [info.BriefingLong1, info.BriefingLong2, info.BriefingLong3, info.BriefingLong4, info.BriefingLong5]);

        string winDebriefing = NormalizeParts(
            [info.DebunkWin, info.DebunkWin2, info.DebunkWin3, info.DebunkWin4, info.DebunkWin5]);

        yield return MakeText(info, NameTextId, NormalizeName(info.Name));
        yield return MakeText(info, DescriptionTextId, info.Description);
        yield return MakeText(info, ObjectiveTextId, info.Briefing);
        yield return MakeText(info, BriefingTextId, briefing);
        yield return MakeText(info, WinDebriefingTextId, winDebriefing);
        yield return MakeText(info, LoseDebriefingTextId, info.DebunkLose);
        yield return MakeText(info, CreatorTextId, info.Creator);
    }

    private static Db.TextScenario MakeText(Db.ScenarioInfo info, string idString, string? text)
    {
        return new Db.TextScenario(info.ScenarioId, info.InfoId, idString, text, 0);
    }

    private static string NormalizeName(string name)
    {
        string text = name.Trim();

        if (text is ['\'', _, .., '\''])
        {
            return text[1..^1];
        }
        else
        {
            return text;
        }
    }

    private static string NormalizeParts(IEnumerable<string?> parts)
    {
        string text = string.Concat(parts).Replace("_", string.Empty);

        if (text.Length >= 4 &&
            text[0] is '%' &&
            char.IsAsciiDigit(text[1]) &&
            char.IsAsciiDigit(text[2]) &&
            text[3] is '%')
        {
            text = text[4..];
        }

        for (int lineBreaks = 3; lineBreaks <= 7; lineBreaks++)
        {
            text = text.Replace($"%{lineBreaks}%", new string('\n', lineBreaks));
        }

        return text;
    }
}
