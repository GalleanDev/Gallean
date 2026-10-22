namespace Gallean.Client;

internal sealed partial record BattleAnimationState
{
    public sealed record FormEntry(
        TimeSpan At,
        UnitDefinitionId DefinitionId,
        BattleSide Facing,
        BattleUnitState.Auras Auras) : IBattleAnimationEntry;
}
