using System.Collections.Immutable;

namespace Gallean.Engine;

public sealed class GameTextProvider : IGameTextProvider
{
    private readonly ImmutableDictionary<GlobalTextId, GlobalText> globalTexts;
    private readonly ImmutableDictionary<ScenarioTextId, string> scenarioTexts;

    public GameTextProvider(
        IGlobalDatabaseIndex globalDb,
        IScenarioDatabaseIndex scenarioDb,
        IEnumerable<Db.TextScenario> scenarioTexts)
    {
        IEnumerable<Db.TextScenario> scenarioInfoTexts = scenarioDb.ScenarioInfos.SelectMany(ScenarioInfoTextDecoder.DecodeTexts);

        globalTexts = LoadGlobalTexts(globalDb);
        this.scenarioTexts = LoadScenarioTexts(scenarioInfoTexts.Concat(scenarioTexts));
    }

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

    private static ImmutableDictionary<ScenarioTextId, string> LoadScenarioTexts(IEnumerable<Db.TextScenario> texts)
    {
        return texts.ToImmutableDictionary(
            text => ScenarioTextId.MakeInternal(text.IdObject, text.IdString, text.SeqNum),
            text => ScenarioTextNormalizer.Normalize(text.Text) ?? string.Empty);
    }

    private static ImmutableDictionary<GlobalTextId, GlobalText> LoadGlobalTexts(IGlobalDatabaseIndex globalDb)
    {
        ImmutableDictionary<GlobalTextId, GlobalText>.Builder result = ImmutableDictionary.CreateBuilder<GlobalTextId, GlobalText>();

        foreach (Db.TextGlobal text in globalDb.TextGlobals.Values)
        {
            GlobalTextId id = new(text.TextId);

            result.Add(id, new GlobalText(id, text.Text ?? string.Empty, text.Verified, text.Context));
        }

        return result.ToImmutable();
    }
}
