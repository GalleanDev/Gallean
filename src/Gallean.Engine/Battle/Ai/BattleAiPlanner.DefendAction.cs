namespace Gallean.Engine;

internal sealed partial class BattleAiPlanner
{
    private sealed record DefendAction(IEngineContext Context, int Priority) : AbstractAction(Context, Priority)
    {
        public override BattleAiActionPlan? Plan(BattleState state, BattleUnit unit)
        {
            BattleDefendCommand command = new(unit.Id);

            if (IsLastFrontline(state, unit) &&
                HasActiveBackLine(state, unit) &&
                GetDefendDamage(state, unit) > GetAttackDamage(state, unit))
            {
                return MakePlan(MaxScore / 10 + 1, command);
            }
            else
            {
                return MakePlan(0, command);
            }
        }

        private static bool IsLastFrontline(BattleState state, BattleUnit unit)
        {
            bool isFrontLine = false;

            foreach ((BattleUnit ally, GridPosition position) in state.GetGrid(unit.Side).GetUnits())
            {
                if (ally.Id == unit.Id)
                {
                    isFrontLine = position.IsFrontLine;
                }
                else if (ally.IsActive() && position.IsFrontLine)
                {
                    return false;
                }
            }

            return isFrontLine;
        }

        private static bool HasActiveBackLine(BattleState state, BattleUnit unit)
        {
            foreach ((BattleUnit ally, GridPosition position) in state.GetGrid(unit.Side).GetUnits())
            {
                if (ally.Id != unit.Id && ally.IsActive() && position.IsBackLine)
                {
                    return true;
                }
            }

            return false;
        }

        private int GetAttackDamage(BattleState state, BattleUnit unit)
        {
            UnitDefinition definition = Context.Definitions.GetUnit(unit.DefinitionId);
            UnitAttackStats stats = Context.UnitStats.GetBattleAttackStats(state, unit.Id, definition.Attack);

            if (definition.AttacksTwice)
            {
                return stats.Damage * 2;
            }
            else
            {
                return stats.Damage;
            }
        }

        private int GetDefendDamage(BattleState state, BattleUnit unit)
        {
            BattleUnit defendingUnit = unit with { StatusEffects = [.. unit.StatusEffects, new BattleDefendStatusEffect()] };
            int incomingDamage = EstimateIncomingDamage(state, unit);
            int damage = CalculateDamage(state, unit, incomingDamage);
            int defendDamage = CalculateDamage(state.WithUnit(defendingUnit), defendingUnit, incomingDamage);

            return damage - defendDamage;
        }

        private int CalculateDamage(BattleState state, BattleUnit unit, int value)
        {
            int armor = Math.Clamp(Context.UnitStats.GetBattleUnitStats(state, unit.Id).Armor, 0, Context.Globals.MaxArmor);
            int damage = value * (100 - armor) / 100;

            return Math.Min(damage, unit.Hitpoints);
        }

        private int EstimateIncomingDamage(BattleState state, BattleUnit unit)
        {
            int damage = 0;

            foreach ((BattleUnit enemy, _) in state.GetGrid(unit.Side.GetOpposite()).GetUnits())
            {
                UnitDefinition definition = Context.Definitions.GetUnit(enemy.DefinitionId);

                if (enemy.IsActive() &&
                    !enemy.HasAnyStatusEffect<BattleAbstractDisableEffect, BattleRetreatStatusEffect>() &&
                    ReachCalculator.IsTargetReachable(state, enemy, unit, definition.Attack))
                {
                    UnitAttackStats stats = Context.UnitStats.GetBattleAttackStats(state, enemy.Id, definition.Attack);

                    if (definition.AttacksTwice)
                    {
                        damage += stats.Damage * 2;
                    }
                    else
                    {
                        damage += stats.Damage;
                    }
                }
            }

            return damage;
        }
    }
}
