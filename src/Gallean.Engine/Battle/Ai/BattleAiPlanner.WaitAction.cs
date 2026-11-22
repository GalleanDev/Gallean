using System.Collections.Immutable;

namespace Gallean.Engine;

internal sealed partial class BattleAiPlanner
{
    private sealed record WaitAction(IEngineContext Context, int Priority) : AbstractAction(Context, Priority)
    {
        private const int MinTargetValue = MaxScore / 10;

        private static readonly UtilityScore.ResponseCurve WeaknessCurve =
            new UtilityScore.ResponseCurve.Polynomial(Slope: MinTargetValue - MaxScore, Exponent: 1, XShift: 0, YShift: MaxScore);

        private AttackAction Attack { get; } = new(Context, Priority);
        private BattleAiTargetProvider TargetProvider { get; } = new(Context);

        public override BattleAiActionPlan? Plan(BattleState state, BattleUnit unit)
        {
            BattleWaitCommand command = new(unit.Id);
            BattleCommandResult commandResult = command.Execute(state, Context);

            if (commandResult.Failed)
            {
                return null;
            }

            BattleAiActionPlan? attackPlan = Attack.Plan(state, unit);

            if (attackPlan?.Command is BattleAttackCommand { Target: BattleAttackUnitTarget attackTarget })
            {
                BattleUnit target = state.GetUnit(attackTarget.UnitId);

                int score = UtilityScore.Combine(
                [
                    Weakness(state, target),
                    Surviving(state, unit, target),
                    Killable(state, unit, target),
                    Healed(state, unit, target),
                    Safe(state, unit)
                ]);

                if (score > 0)
                {
                    return MakePlan(score, command);
                }
            }

            return null;
        }

        private UtilityScore.Consideration Weakness(BattleState state, BattleUnit target)
        {
            int effectiveHitpoints = BattleAiStatCalculator.GetEffectiveHitpoints(Context, state, target);

            return UtilityScore.Of(Math.Min(effectiveHitpoints, UtilityScore.MaxScore), WeaknessCurve);
        }

        private UtilityScore.Consideration Surviving(BattleState state, BattleUnit unit, BattleUnit target)
        {
            int damage = GetUnitDamage(state, unit, target);
            int hitpoints = BattleAiStatCalculator.GetEffectiveHitpoints(Context, state, target);
            bool isSurviving = damage < hitpoints;

            return UtilityScore.Flag(isSurviving);
        }

        private UtilityScore.Consideration Killable(BattleState state, BattleUnit unit, BattleUnit target)
        {
            UnitStats targetStats = Context.UnitStats.GetBattleUnitStats(state, target.Id);
            BattleUnit healthyTarget = target with { Hitpoints = targetStats.MaxHitpoints };
            int damage = GetUnitDamage(state, unit, target) * 2;
            int hitpoints = BattleAiStatCalculator.GetEffectiveHitpoints(Context, state, healthyTarget);
            bool isKillable = damage >= hitpoints;

            return UtilityScore.Flag(isKillable);
        }

        private UtilityScore.Consideration Healed(BattleState state, BattleUnit unit, BattleUnit target)
        {
            int heal = EstimateRoundHeal(state, target.Side);
            BattleUnit healedTarget = target with { Hitpoints = target.Hitpoints + heal };
            int damage = GetUnitDamage(state, unit, target) * 2;
            int hitpoints = BattleAiStatCalculator.GetEffectiveHitpoints(Context, state, healedTarget);
            bool isHealed = damage < hitpoints;

            return UtilityScore.Flag(isHealed);
        }

        private UtilityScore.Consideration Safe(BattleState state, BattleUnit unit)
        {
            int damage = EstimateRoundDamage(state, unit);
            int hitpoints = BattleAiStatCalculator.GetEffectiveHitpoints(Context, state, unit);
            bool isSafe = damage < hitpoints;

            return UtilityScore.Flag(isSafe);
        }

        private int EstimateRoundHeal(BattleState state, BattleSide side)
        {
            ImmutableArray<BattleUnitTurn> turns = state.TurnQueue.AddRange(state.WaitingQueue);
            int heal = 0;

            foreach (BattleUnitTurn turn in turns)
            {
                BattleUnit? unit = state.TryGetUnit(turn.UnitId);

                if (unit is not null &&
                    unit.Side == side &&
                    unit.IsActive() &&
                    !unit.IsDisabled() &&
                    !unit.IsRetreating())
                {
                    heal += GetUnitHeal(state, unit);
                }
            }

            return heal;
        }

        private int EstimateRoundDamage(BattleState state, BattleUnit target)
        {
            ImmutableArray<BattleUnitTurn> turns = state.TurnQueue.AddRange(state.WaitingQueue);
            int damage = 0;

            foreach (BattleUnitTurn turn in turns)
            {
                BattleUnit? unit = state.TryGetUnit(turn.UnitId);

                if (unit is not null &&
                    unit.Side != target.Side &&
                    unit.IsActive() &&
                    !unit.IsDisabled() &&
                    !unit.IsRetreating() &&
                    CanAttack(state, unit, target))
                {
                    damage += GetUnitDamage(state, unit, target);
                }
            }

            return damage;
        }

        private int GetUnitHeal(BattleState state, BattleUnit unit)
        {
            UnitDefinition definition = Context.Definitions.GetUnit(unit.DefinitionId);
            int heal = 0;

            if (definition.Attack.Type == UnitAttackType.Heal)
            {
                UnitAttackStats attackStats = Context.UnitStats.GetBattleAttackStats(state, unit.Id, definition.Attack);

                heal += attackStats.Heal;
            }

            if (definition.SecondaryAttack is not null && definition.SecondaryAttack.Type == UnitAttackType.Heal)
            {
                UnitAttackStats attackStats = Context.UnitStats.GetBattleAttackStats(state, unit.Id, definition.SecondaryAttack);

                heal += attackStats.Heal;
            }

            return heal;
        }

        private int GetUnitDamage(BattleState state, BattleUnit unit, BattleUnit target)
        {
            UnitDefinition definition = Context.Definitions.GetUnit(unit.DefinitionId);
            UnitWard? ward = BattleUnitWardCalculator.FindBlockingWard(target, definition.Attack);

            if (ward is null && definition.HasAnyAttackType(UnitAttackType.Damage, UnitAttackType.DrainLife, UnitAttackType.DrainLifeOverflow))
            {
                UnitAttackStats attackStats = Context.UnitStats.GetBattleAttackStats(state, unit.Id, definition.Attack);
                int damage = attackStats.Damage;

                if (definition.AttacksTwice)
                {
                    damage *= 2;
                }

                return damage;
            }
            else
            {
                return 0;
            }
        }

        private bool CanAttack(BattleState state, BattleUnit unit, BattleUnit target)
        {
            foreach ((BattleAttackTarget attackTarget, _) in TargetProvider.GetAttackTargets(state, unit))
            {
                if (attackTarget is BattleAttackUnitTarget unitTarget && unitTarget.UnitId == target.Id)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
