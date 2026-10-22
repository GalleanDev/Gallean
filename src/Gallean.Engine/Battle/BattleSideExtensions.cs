namespace Gallean.Engine;

public static class BattleSideExtensions
{
    extension(BattleSide side)
    {
        public BattleSide GetOpposite() => side switch
        {
            BattleSide.Attacker => BattleSide.Defender,
            BattleSide.Defender => BattleSide.Attacker
        };
    }
}
