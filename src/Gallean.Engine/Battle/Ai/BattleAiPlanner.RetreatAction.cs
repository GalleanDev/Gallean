namespace Gallean.Engine;

internal sealed partial class BattleAiPlanner
{
    private sealed record RetreatAction(IEngineContext Context, int Priority) : AbstractAction(Context, Priority)
    {
        private static readonly UtilityScore.ResponseCurve PowerRatioCurve =
            new UtilityScore.ResponseCurve.Polynomial(Slope: -7 * MaxScore, Exponent: 1, XShift: 333, YShift: 0);

        private WorldAiEstimator Estimator { get; } = new(Context);

        public override BattleAiActionPlan? Plan(BattleState state, BattleUnit unit)
        {
            BattleRetreatCommand command = new(unit.Id);
            BattleCommandResult commandResult = command.Execute(state, Context);

            if (commandResult.Failed)
            {
                return null;
            }

            int score = UtilityScore.Combine(
            [
                NotNeutral(state, unit.Side),
                Leader(unit),
                AliveAllies(state, unit),
                PowerRatio(state, unit.Side)
            ]);

            if (score > 0)
            {
                return MakePlan(score, command);
            }
            else
            {
                return null;
            }
        }

        private static UtilityScore.Consideration NotNeutral(BattleState state, BattleSide side)
        {
            BattleSideInfo info = side switch
            {
                BattleSide.Attacker => state.Info.Attacker,
                BattleSide.Defender => state.Info.Defender
            };

            return UtilityScore.Flag(!info.IsNeutral);
        }

        private UtilityScore.Consideration Leader(BattleUnit unit)
        {
            UnitDefinition definition = Context.Definitions.GetUnit(unit.DefinitionId);

            return UtilityScore.Flag(definition.Leadership is > 0);
        }

        private static UtilityScore.Consideration AliveAllies(BattleState state, BattleUnit unit)
        {
            foreach ((BattleUnit ally, _) in state.GetGrid(unit.Side).GetUnits())
            {
                if (ally.Id != unit.Id && ally.IsActive())
                {
                    return UtilityScore.Full;
                }
            }

            return UtilityScore.None;
        }

        private UtilityScore.Consideration PowerRatio(BattleState state, BattleSide side)
        {
            WorldAiEstimator.PowerEstimate estimate = Estimator.EstimatePower(state);
            int power = CanDamage(state, side) ? estimate.GetPower(side) : 0;
            int enemyPower = estimate.GetPower(side.GetOpposite());

            return UtilityScore.Ratio(power, power + enemyPower, PowerRatioCurve);
        }

        private bool CanDamage(BattleState state, BattleSide side)
        {
            foreach ((BattleUnit unit, _) in state.GetGrid(side).GetUnits())
            {
                foreach ((BattleUnit enemy, _) in state.GetGrid(side.GetOpposite()).GetUnits())
                {
                    if (unit.IsActive() && enemy.IsActive() && CanDamage(unit, enemy))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool CanDamage(BattleUnit unit, BattleUnit target)
        {
            UnitAttackDefinition attack = Context.Definitions.GetUnit(unit.DefinitionId).Attack;

            return attack.Type switch
            {
                UnitAttackType.Damage => !IsImmune(target, attack),
                UnitAttackType.DrainLife => !IsImmune(target, attack),
                UnitAttackType.DrainLifeOverflow => !IsImmune(target, attack),
                UnitAttackType.Poison => !IsImmune(target, attack),
                UnitAttackType.Frostbite => !IsImmune(target, attack),
                UnitAttackType.Blister => !IsImmune(target, attack),
                UnitAttackType.Summon => true,
                UnitAttackType.Doppelganger => true,
                UnitAttackType.TransformSelf => true,
                UnitAttackType.ReduceDamage => false,
                UnitAttackType.ReduceInitiative => false,
                UnitAttackType.ReduceLevel => false,
                UnitAttackType.ReduceArmor => false,
                UnitAttackType.Paralyze => false,
                UnitAttackType.Petrify => false,
                UnitAttackType.Fear => false,
                UnitAttackType.TransformEnemy => false,
                UnitAttackType.GiveAdditionalAttack => false,
                UnitAttackType.Heal => false,
                UnitAttackType.Cure => false,
                UnitAttackType.Revive => false,
                UnitAttackType.GiveProtection => false,
                UnitAttackType.IncreaseDamage => false
            };
        }

        private static bool IsImmune(BattleUnit target, UnitAttackDefinition attack)
        {
            UnitWard? ward = BattleUnitWardCalculator.FindBlockingWard(target, attack);

            return ward is { Category: UnitWardCategory.Always };
        }
    }
}
