namespace Gallean.Entities;

public sealed record LanguageDatabase(
    IReadOnlyList<Db.TextGlobal> TextGlobals,
    IReadOnlyList<Db.TextLeader> TextLeaders,
    IReadOnlyList<Db.ScenarioInfo> ScenarioInfos,
    IReadOnlyList<Db.TextScenario> TextScenarios)
{
    public static LanguageDatabase Empty { get; } = new([], [], [], []);
}
