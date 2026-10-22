namespace Gallean.Engine;

internal static class BattleOutcomeCalculator
{
    public static BattleOutcome? GetOutcome(BattleState state)
    {
        bool attackerActive = false;
        bool defenderActive = false;

        foreach (BattleUnitId id in state.GetAllUnitIds())
        {
            BattleUnit unit = state.GetUnit(id);

            if (unit.IsActive())
            {
                if (unit.Side == BattleSide.Attacker)
                {
                    attackerActive = true;
                }

                if (unit.Side == BattleSide.Defender)
                {
                    defenderActive = true;
                }
            }
        }

        if (attackerActive && defenderActive)
        {
            return null;
        }
        else if (attackerActive)
        {
            return new BattleOutcome(BattleSide.Attacker);
        }
        else
        {
            return new BattleOutcome(BattleSide.Defender);
        }
    }
}
