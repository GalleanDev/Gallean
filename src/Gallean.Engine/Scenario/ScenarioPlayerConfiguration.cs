namespace Gallean.Engine;

public sealed record ScenarioPlayerConfiguration(
    WorldRace Race,
    LordCategory LordCategory,
    WorldObjectVariant LordFaceVariant,
    UnitDefinitionId? StartingLeaderId,
    UnitName? LeaderName);
