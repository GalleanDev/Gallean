using System.Collections.Immutable;

namespace Gallean.Engine;

internal sealed class BattleAiTargetProvider(IEngineContext context)
{
    private readonly IEngineContext engineContext =
        new EngineContext(new FixedRandomProvider(), context.Definitions, context.UnitStats, context.Scenario, context.Globals);

    public IReadOnlyList<(BattleAttackTarget Target, ImmutableArray<UnitAttackDefinition> Attacks)> GetAttackTargets(BattleState state, BattleUnit attacker)
    {
        List<(BattleAttackTarget Target, ImmutableArray<UnitAttackDefinition> Attacks)> targets = [];

        foreach (BattleUnitId id in state.GetAllUnitIds())
        {
            BattleAttackUnitTarget target = new(id);
            BattleAttackCalculator.AttackInfo info = BattleAttackCalculator.GetUnitAttackInfo(state, engineContext, attacker.Id, target);

            if (!info.Failed)
            {
                targets.Add((target, info.Attacks));
            }
        }

        foreach (GridPosition position in GridPosition.All)
        {
            BattleAttackTileTarget target = new(attacker.Side, position);
            BattleAttackCalculator.AttackInfo info = BattleAttackCalculator.GetUnitAttackInfo(state, engineContext, attacker.Id, target);

            if (!info.Failed)
            {
                targets.Add((target, info.Attacks));
            }
        }

        return targets;
    }
}
