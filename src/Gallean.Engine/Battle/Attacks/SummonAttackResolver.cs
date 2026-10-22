using Gallean.Common;

namespace Gallean.Engine;

internal sealed class SummonAttackResolver : IBattleAttackResolver, IStaticInstance<SummonAttackResolver>
{
    public IReadOnlyList<BattleAttackTarget> GetTargets(
        BattleState state,
        BattleUnitId attackerId,
        BattleAttackTarget target,
        UnitAttackDefinition attack)
    {
        BattleAttackTileTarget? tileTarget = ResolveTargetTile(state, target);

        if (tileTarget is null || tileTarget.Side != state.GetUnit(attackerId).Side)
        {
            return [];
        }

        List<BattleAttackTarget> targets = [];

        foreach (GridPosition position in GetTargetPositions(state, tileTarget.Side, tileTarget.Position, attack))
        {
            targets.Add(new BattleAttackTileTarget(tileTarget.Side, position));
        }

        return targets;
    }

    public BattleAttackResult Resolve(
        BattleState state,
        IEngineContext context,
        BattleUnitId attackerId,
        BattleAttackTarget target,
        UnitAttackDefinition attack,
        UnitAttackStats attackStats)
    {
        if (attack.SummonUnitIds.IsEmpty)
        {
            throw new InvalidOperationException("A summon attack must define a summon unit.");
        }

        BattleAttackTileTarget? tileTarget = ResolveTargetTile(state, target);
        if (tileTarget is null)
        {
            return BattleAttackResult.Fail(BattleCommandError.SummonTargetInvalid);
        }

        BattleUnit attacker = state.GetUnit(attackerId);

        if (tileTarget.Side != attacker.Side)
        {
            return BattleAttackResult.Fail(BattleCommandError.SummonTargetInvalid);
        }

        if (!SummonCalculator.CanSummon(attack, state, attacker.Side, tileTarget.Position, context.Definitions))
        {
            return BattleAttackResult.Fail(BattleCommandError.SummonTargetOccupied);
        }

        IReadOnlyList<GridPosition> positions = GetTargetPositions(state, attacker.Side, tileTarget.Position, attack);
        IReadOnlyList<BattleSummonPlacement> placements = SummonCalculator.GetSummonPlacements(
            attack,
            state,
            attacker.Side,
            positions,
            context.Definitions,
            context.Random);

        List<BattleEvent> events = [];
        int nextUnitId = state.GetNextUnitId().Value;

        foreach (BattleSummonPlacement placement in placements)
        {
            UnitDefinition summonDefinition = context.Definitions.GetUnit(placement.UnitDefinitionId);

            events.Add(new BattleUnitSummonedEvent(
                new BattleUnitId(nextUnitId),
                placement.UnitDefinitionId,
                attacker.Side,
                placement.Position,
                summonDefinition.MaxHitpoints,
                summonDefinition.Level,
                attackerId));

            nextUnitId++;
        }

        return new BattleAttackResult(events, DidHit: false, Error: null);
    }

    private static IReadOnlyList<GridPosition> GetTargetPositions(
        BattleState state,
        BattleSide side,
        GridPosition targetPosition,
        UnitAttackDefinition attack)
    {
        if (attack.Reach == UnitAttackReach.All)
        {
            return SummonCalculator.GetSummonPositions(state, side);
        }
        else
        {
            return [targetPosition];
        }
    }

    private static BattleAttackTileTarget? ResolveTargetTile(BattleState state, BattleAttackTarget target)
    {
        BattleAttackTileTarget? tileTarget = target.GetTileTarget();

        if (tileTarget is not null)
        {
            return tileTarget;
        }

        BattleUnitId? targetUnitId = target.GetUnitId();

        if (targetUnitId is null)
        {
            return null;
        }

        BattleUnit targetUnit = state.GetUnit(targetUnitId.Value);

        foreach ((BattleUnit unit, GridPosition position) in state.GetGrid(targetUnit.Side).GetUnits())
        {
            if (unit.Id == targetUnit.Id)
            {
                return new BattleAttackTileTarget(targetUnit.Side, position);
            }
        }

        return null;
    }
}
