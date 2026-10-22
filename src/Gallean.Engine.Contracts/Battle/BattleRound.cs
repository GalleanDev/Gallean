namespace Gallean.Engine.Contracts;

public readonly record struct BattleRound(BattleRoundType Type, int Number)
{
    public static BattleRound Opening(int number)
    {
        return new BattleRound(BattleRoundType.Opening, number);
    }

    public static BattleRound Normal(int number)
    {
        return new BattleRound(BattleRoundType.Normal, number);
    }

    public static BattleRound Recovery(int number)
    {
        return new BattleRound(BattleRoundType.Recovery, number);
    }
}
