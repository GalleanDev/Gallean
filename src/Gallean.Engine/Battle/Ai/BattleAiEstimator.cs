namespace Gallean.Engine;

internal sealed class BattleAiEstimator(IEngineContext context)
{
    public int EstimateUnitPower(BattleState state, BattleUnit unit)
    {
        UnitDefinition definition = context.Definitions.GetUnit(unit.DefinitionId);
        UnitAttackStats attackStats = context.UnitStats.GetBattleAttackStats(state, unit.Id, definition.Attack);

        int power = attackStats.Damage;

        if (definition.AttacksTwice)
        {
            power *= 2;
        }

        if (definition.Attack.Reach == UnitAttackReach.All)
        {
            power *= 3;
        }

        return power;
    }
}
