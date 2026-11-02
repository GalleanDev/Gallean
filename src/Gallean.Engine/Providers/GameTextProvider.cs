using System.Collections.Immutable;

namespace Gallean.Engine;

public sealed class GameTextProvider : IGameTextProvider
{
    private readonly ImmutableDictionary<GlobalTextId, GlobalText> globalTexts;
    private readonly ImmutableDictionary<ScenarioTextId, string> scenarioTexts;
    private readonly ImmutableDictionary<(WorldRace Race, bool IsMale), ImmutableArray<string>> leaderNames;

    public GameTextProvider(
        IGlobalDatabaseIndex globalDb,
        IScenarioDatabaseIndex scenarioDb,
        IEnumerable<Db.TextScenario> scenarioTexts,
        LanguageDatabase language)
    {
        IEnumerable<Db.TextScenario> scenarioInfoTexts = scenarioDb.ScenarioInfos.SelectMany(ScenarioInfoTextDecoder.DecodeTexts);
        IEnumerable<Db.TextScenario> languageInfoTexts = language.ScenarioInfos.SelectMany(ScenarioInfoTextDecoder.DecodeTexts);
        ImmutableArray<Db.TextScenario> rows = OverlayScenarioTexts(
            scenarioInfoTexts.Concat(scenarioTexts),
            languageInfoTexts.Concat(language.TextScenarios));

        globalTexts = LoadGlobalTexts(globalDb, language.TextGlobals);
        this.scenarioTexts = LoadScenarioTexts(rows);
        leaderNames = LoadLeaderNames(globalDb, language.TextLeaders);
        Names = ScenarioNameLoader.Load(globalDb, globalTexts, rows);
    }

    public ImmutableArray<ScenarioName> Names { get; }

    public GlobalText GetGlobalText(GlobalTextId id)
    {
        if (!globalTexts.TryGetValue(id, out GlobalText? text))
        {
            throw new KeyNotFoundException($"Global text '{id}' was not found.");
        }

        if (string.IsNullOrEmpty(text.Text))
        {
            throw new InvalidDataException($"Global text '{id}' is empty.");
        }

        return text;
    }

    public string GetScenarioText(ScenarioTextId id)
    {
        if (!scenarioTexts.TryGetValue(id, out string? text))
        {
            throw new KeyNotFoundException($"Scenario text '{id}' was not found.");
        }

        return text;
    }

    public string GetLeaderName(LeaderNameId id)
    {
        if (!leaderNames.TryGetValue((id.Race, id.IsMale), out ImmutableArray<string> names) || id.Index < 0 || id.Index >= names.Length)
        {
            throw new KeyNotFoundException($"Leader name '{id}' was not found.");
        }

        return names[id.Index];
    }

    public string GetUnitName(UnitName name)
    {
        return name.Accept(new UnitNameFormatter(this));
    }

    private static ImmutableArray<Db.TextScenario> OverlayScenarioTexts(
        IEnumerable<Db.TextScenario> texts,
        IEnumerable<Db.TextScenario> languageTexts)
    {
        List<Db.TextScenario> rows = [];
        Dictionary<ScenarioTextId, int> indexes = [];

        foreach (Db.TextScenario text in texts)
        {
            indexes.Add(MakeId(text), rows.Count);
            rows.Add(text);
        }

        foreach (Db.TextScenario text in languageTexts)
        {
            if (string.IsNullOrEmpty(ScenarioTextNormalizer.Normalize(text.Text)))
            {
                continue;
            }

            if (indexes.TryGetValue(MakeId(text), out int index))
            {
                rows[index] = rows[index] with { Text = text.Text };
            }
            else
            {
                indexes.Add(MakeId(text), rows.Count);
                rows.Add(text);
            }
        }

        return [.. rows];
    }

    private static ImmutableDictionary<ScenarioTextId, string> LoadScenarioTexts(ImmutableArray<Db.TextScenario> texts)
    {
        ImmutableDictionary<ScenarioTextId, string>.Builder result = ImmutableDictionary.CreateBuilder<ScenarioTextId, string>();

        foreach (Db.TextScenario text in texts)
        {
            result.Add(MakeId(text), ScenarioTextNormalizer.Normalize(text.Text) ?? string.Empty);
        }

        return result.ToImmutable();
    }

    private static ScenarioTextId MakeId(Db.TextScenario text)
    {
        return ScenarioTextId.MakeInternal(text.IdObject, text.IdString, text.SeqNum);
    }

    private static ImmutableDictionary<GlobalTextId, GlobalText> LoadGlobalTexts(
        IGlobalDatabaseIndex globalDb,
        IEnumerable<Db.TextGlobal> languageTexts)
    {
        ImmutableDictionary<GlobalTextId, GlobalText>.Builder result = ImmutableDictionary.CreateBuilder<GlobalTextId, GlobalText>();

        foreach (Db.TextGlobal text in globalDb.TextGlobals.Values)
        {
            GlobalTextId id = new(text.TextId);

            result.Add(id, new GlobalText(id, text.Text ?? string.Empty, text.Verified, text.Context));
        }

        foreach (Db.TextGlobal text in languageTexts)
        {
            if (!string.IsNullOrEmpty(text.Text))
            {
                GlobalTextId id = new(text.TextId);

                result[id] = new GlobalText(id, text.Text.Replace("\\n", "\n"), text.Verified, text.Context);
            }
        }

        return result.ToImmutable();
    }

    private static ImmutableDictionary<(WorldRace Race, bool IsMale), ImmutableArray<string>> LoadLeaderNames(
        IGlobalDatabaseIndex globalDb,
        IEnumerable<Db.TextLeader> languageNames)
    {
        Dictionary<Db.Id, WorldRace> races = [];

        foreach (Db.Race race in globalDb.Races.Values)
        {
            races.Add(race.RaceId, ConvertRace(race.RaceType));
        }

        Dictionary<(WorldRace Race, bool IsMale), List<string>> names = GroupLeaderNames(globalDb.TextLeaders, races);
        Dictionary<(WorldRace Race, bool IsMale), List<string>> translations = GroupLeaderNames(languageNames, races);

        foreach ((WorldRace Race, bool IsMale) key in names.Keys)
        {
            if (translations.TryGetValue(key, out List<string>? translated))
            {
                List<string> group = names[key];

                for (int index = 0; index < group.Count && index < translated.Count; index++)
                {
                    if (translated[index].Length > 0)
                    {
                        group[index] = translated[index];
                    }
                }
            }
        }

        return names.ToImmutableDictionary(pair => pair.Key, pair => pair.Value.ToImmutableArray());
    }

    private static WorldRace ConvertRace(Db.RaceType race)
    {
        return race switch
        {
            Db.RaceType.Human => WorldRace.Human,
            Db.RaceType.Undead => WorldRace.Undead,
            Db.RaceType.Heretic => WorldRace.Heretic,
            Db.RaceType.Dwarf => WorldRace.Dwarf,
            Db.RaceType.Neutral => WorldRace.Neutral,
            Db.RaceType.Elf => WorldRace.Elf
        };
    }

    private static Dictionary<(WorldRace Race, bool IsMale), List<string>> GroupLeaderNames(
        IEnumerable<Db.TextLeader> leaderNames,
        IReadOnlyDictionary<Db.Id, WorldRace> races)
    {
        Dictionary<(WorldRace Race, bool IsMale), List<string>> result = [];

        foreach (Db.TextLeader leaderName in leaderNames)
        {
            if (races.TryGetValue(leaderName.RaceId, out WorldRace race))
            {
                (WorldRace Race, bool IsMale) key = (race, leaderName.IsMale == true);

                if (!result.TryGetValue(key, out List<string>? group))
                {
                    group = [];
                    result.Add(key, group);
                }

                group.Add(leaderName.Text);
            }
        }

        return result;
    }
}
