using Gallean.Configuration;

namespace Gallean.Client;

internal sealed record BattlePositions(BattlePosition Attacker, BattlePosition Defender)
{
    public BattlePosition GetPosition(BattleSide side)
    {
        return side switch
        {
            BattleSide.Attacker => Attacker,
            BattleSide.Defender => Defender
        };
    }

    public BattleSide GetSide(BattlePosition position)
    {
        if (position == Attacker)
        {
            return BattleSide.Attacker;
        }
        else
        {
            return BattleSide.Defender;
        }
    }
}
