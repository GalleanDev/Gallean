using System.Collections.Immutable;

namespace Gallean.Engine;

internal static class ScenarioNameLoader
{
    public static ImmutableArray<ScenarioName> Load(
        GlobalDatabase globalDb,
        ScenarioDatabase scenarioDb,
        Db.ScenarioId scenarioId)
    {
        ImmutableArray<Db.TextScenario> scenarioTexts = [.. scenarioDb.TextScenariosByScenario[scenarioId]];
        ImmutableDictionary<GlobalTextId, string> globalTexts = LoadGlobalTexts(globalDb);
        HashSet<string> commonWords = LoadCommonWords(scenarioDb, scenarioTexts, globalTexts);

        List<ScenarioName> names = [];
        HashSet<string> nameTexts = [];

        foreach (ScenarioName name in LoadLabels(globalDb, scenarioTexts, globalTexts))
        {
            if (IsName(name.Text, commonWords) && nameTexts.Add(name.Text))
            {
                names.Add(name);
            }
        }

        foreach (ScenarioName name in LoadTitles(globalDb, scenarioTexts, globalTexts))
        {
            if (name.Text.Length > 0 && nameTexts.Add(name.Text))
            {
                names.Add(name);
            }
        }

        return [.. names];
    }

    private static ImmutableDictionary<GlobalTextId, string> LoadGlobalTexts(GlobalDatabase globalDb)
    {
        return globalDb.TextGlobals.Values.ToImmutableDictionary(
            text => new GlobalTextId(text.TextId),
            text => text.Text ?? string.Empty);
    }

    private static HashSet<string> LoadCommonWords(
        ScenarioDatabase scenarioDb,
        ImmutableArray<Db.TextScenario> scenarioTexts,
        ImmutableDictionary<GlobalTextId, string> globalTexts)
    {
        HashSet<string> words = [];
        HashSet<string>.AlternateLookup<ReadOnlySpan<char>> lookup = words.GetAlternateLookup<ReadOnlySpan<char>>();

        foreach (string? text in LoadTexts(scenarioDb, scenarioTexts, globalTexts))
        {
            if (text is not null)
            {
                foreach (Range range in FindWords(text))
                {
                    ReadOnlySpan<char> word = text.AsSpan()[range];

                    if (char.IsLower(word[0]))
                    {
                        lookup.Add(word);
                    }
                }
            }
        }

        return words;
    }

    private static IEnumerable<string?> LoadTexts(
        ScenarioDatabase scenarioDb,
        ImmutableArray<Db.TextScenario> scenarioTexts,
        ImmutableDictionary<GlobalTextId, string> globalTexts)
    {
        foreach (string text in globalTexts.Values)
        {
            yield return text;
        }

        foreach (Db.ScenarioInfo info in scenarioDb.ScenarioInfos)
        {
            foreach (Db.TextScenario text in ScenarioInfoTextDecoder.DecodeTexts(info))
            {
                yield return text.Text;
            }
        }

        foreach (Db.TextScenario text in scenarioTexts)
        {
            if (!IsTitle(text))
            {
                yield return text.Text;
            }
        }
    }

    private static bool IsTitle(Db.TextScenario text)
    {
        return text.IdString is Db.TextScenario.NameTextId or Db.TextScenario.RuinTitleTextId or Db.TextScenario.SiteTitleTextId;
    }

    private static IEnumerable<ScenarioName> LoadLabels(
        GlobalDatabase globalDb,
        ImmutableArray<Db.TextScenario> scenarioTexts,
        ImmutableDictionary<GlobalTextId, string> globalTexts)
    {
        foreach (Db.TextScenario text in scenarioTexts)
        {
            ScenarioName? name = MapLabel(text);

            if (name is not null)
            {
                yield return name;
            }
        }

        foreach (Db.Unit unit in globalDb.Units.Values)
        {
            yield return new ScenarioName(globalTexts[new GlobalTextId(unit.NameText)], ScenarioNameType.Character);
        }
    }

    private static ScenarioName? MapLabel(Db.TextScenario text)
    {
        return (text.IdObject.Type, text.IdString) switch
        {
            (Db.IdType.Unit or Db.IdType.StackTemplate or Db.IdType.Player, Db.TextScenario.NameTextId) =>
                new ScenarioName(Normalize(text.Text), ScenarioNameType.Character),
            (Db.IdType.Subrace, Db.TextScenario.NameTextId) =>
                new ScenarioName(Normalize(text.Text), ScenarioNameType.Faction),
            (Db.IdType.Event, Db.TextScenario.PopupTextId) =>
                MapSpeaker(text),
            _ => null
        };
    }

    private static ScenarioName? MapSpeaker(Db.TextScenario text)
    {
        ScenarioPopupText popupText = ScenarioPopupText.Parse(Normalize(text.Text));

        if (popupText.Speaker is not null)
        {
            return new ScenarioName(popupText.Speaker, ScenarioNameType.Character);
        }
        else
        {
            return null;
        }
    }

    private static IEnumerable<ScenarioName> LoadTitles(
        GlobalDatabase globalDb,
        ImmutableArray<Db.TextScenario> scenarioTexts,
        ImmutableDictionary<GlobalTextId, string> globalTexts)
    {
        foreach (Db.TextScenario text in scenarioTexts)
        {
            if (IsPlaceTitle(text))
            {
                yield return new ScenarioName(Normalize(text.Text), ScenarioNameType.Place);
            }
        }

        foreach (Db.Race race in globalDb.Races.Values)
        {
            yield return new ScenarioName(globalTexts[new GlobalTextId(race.NameText)], ScenarioNameType.Faction);
        }

        foreach (Db.Item item in globalDb.Items.Values)
        {
            yield return new ScenarioName(globalTexts[new GlobalTextId(item.NameText)], ScenarioNameType.Item);
        }
    }

    private static bool IsPlaceTitle(Db.TextScenario text)
    {
        return (text.IdObject.Type, text.IdString) switch
        {
            (Db.IdType.Fortification, Db.TextScenario.NameTextId) => true,
            (Db.IdType.Ruin, Db.TextScenario.RuinTitleTextId) => true,
            (Db.IdType.Site, Db.TextScenario.SiteTitleTextId) => true,
            _ => false
        };
    }

    private static string Normalize(string? text)
    {
        return ScenarioTextNormalizer.Normalize(text) ?? string.Empty;
    }

    private static bool IsName(string name, HashSet<string> commonWords)
    {
        foreach (Range range in FindWords(name))
        {
            string word = name[range];

            if (char.IsUpper(word[0]) && !commonWords.Contains(word.ToLowerInvariant()))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<Range> FindWords(string text)
    {
        int start = 0;

        while (start < text.Length)
        {
            if (char.IsLetter(text[start]))
            {
                int end = start + 1;

                while (end < text.Length && IsWordCharacter(text, end))
                {
                    end++;
                }

                yield return start..end;

                start = end;
            }
            else
            {
                start++;
            }
        }
    }

    private static bool IsWordCharacter(string text, int index)
    {
        if (text[index] == '\'')
        {
            return index + 1 < text.Length && char.IsLetter(text[index + 1]);
        }
        else
        {
            return char.IsLetter(text[index]);
        }
    }
}
