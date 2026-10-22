namespace Gallean.Engine;

internal static class BattleAiStatCalculator
{
    public static int GetEffectiveHitpoints(IEngineContext context, BattleState state, BattleUnit unit)
    {
        int armor = Math.Clamp(context.UnitStats.GetBattleUnitStats(state, unit.Id).Armor, 0, context.Globals.MaxArmor);

        if (armor < 100)
        {
            return unit.Hitpoints * 100 / (100 - armor);
        }
        else
        {
            return context.Globals.MaxHitpoints;
        }
    }
}
