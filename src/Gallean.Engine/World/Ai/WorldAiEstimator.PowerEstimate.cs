namespace Gallean.Engine;

internal sealed partial class WorldAiEstimator
{
    public sealed record PowerEstimate(int Attacker, int Defender)
    {
        public bool AtLeast(BattleSide side, int percent)
        {
            int power = GetPower(side);
            int oppositePower = GetPower(side.GetOpposite());

            return (long)power * 100 >= (long)oppositePower * percent;
        }

        private int GetPower(BattleSide side) => side switch
        {
            BattleSide.Attacker => Attacker,
            BattleSide.Defender => Defender
        };
    }
}
