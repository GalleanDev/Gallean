using Gallean.Configuration;

namespace Gallean.Client;

internal static class BattlePositionExtensions
{
    public static BattlePosition GetOpposite(this BattlePosition position)
    {
        return position switch
        {
            BattlePosition.Left => BattlePosition.Right,
            BattlePosition.Right => BattlePosition.Left
        };
    }

    public static BattleSide GetFacing(this BattlePosition position)
    {
        return position switch
        {
            BattlePosition.Left => BattleSide.Attacker,
            BattlePosition.Right => BattleSide.Defender
        };
    }
}
