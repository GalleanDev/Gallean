namespace Gallean.Engine.Contracts;

public sealed record BattleUnitWardModifier(
    BattleUnitId CasterId,
    UnitModifierDefinitionId ModifierId,
    UnitWard Ward);
