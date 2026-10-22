namespace Gallean.Engine;

public sealed record ScenarioInfoTexts(
    ScenarioTextId Name,
    ScenarioTextId? Description,
    ScenarioTextId Objective,
    ScenarioTextId? Briefing,
    ScenarioTextId WinDebriefing,
    ScenarioTextId LoseDebriefing,
    ScenarioTextId? Creator);
