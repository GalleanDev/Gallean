namespace Gallean.Engine;

internal partial class BattleEngine
{
    BattleState IBattleEventVisitor<BattleState, BattleState>.Visit(BattleUnitTransformAppliedEvent ev, BattleState state)
    {
        return BattleTransformationCalculator.Transform(
            state,
            Context,
            ev.TargetId,
            ev.Type,
            ev.DefinitionId,
            ev.Level,
            ev.HitpointsAfter,
            ev.RemainingTurns);
    }
}
