namespace Gallean.Engine;

public sealed record UnitDynamicUpgradeDefinition(
    UnitDefinitionId UnitDefinitionId,
    int UpgradeChangeLevel,
    UnitDynamicUpgrade LowLevelUpgrade,
    UnitDynamicUpgrade HighLevelUpgrade);
