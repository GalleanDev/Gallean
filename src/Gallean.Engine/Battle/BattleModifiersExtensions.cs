namespace Gallean.Engine;

public static class BattleModifiersExtensions
{
    extension(BattleModifiers modifiers)
    {
        public BattleSideModifiers GetModifiers(BattleSide side)
        {
            if (side == BattleSide.Attacker)
            {
                return modifiers.Attacker;
            }
            else
            {
                return modifiers.Defender;
            }
        }
    }
}
