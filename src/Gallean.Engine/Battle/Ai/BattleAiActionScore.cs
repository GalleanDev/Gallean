namespace Gallean.Engine;

public readonly record struct BattleAiActionScore(int Priority, int Value) : IComparable<BattleAiActionScore>
{
    public int CompareTo(BattleAiActionScore other)
    {
        int rv;

        rv = Priority.CompareTo(other.Priority);
        if (rv != 0)
        {
            return rv;
        }

        rv = Value.CompareTo(other.Value);
        if (rv != 0)
        {
            return rv;
        }

        return 0;
    }

    public override string ToString()
    {
        return $"P{Priority} {Value}";
    }

    public static bool operator >(BattleAiActionScore left, BattleAiActionScore right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator <(BattleAiActionScore left, BattleAiActionScore right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator >=(BattleAiActionScore left, BattleAiActionScore right)
    {
        return left.CompareTo(right) >= 0;
    }

    public static bool operator <=(BattleAiActionScore left, BattleAiActionScore right)
    {
        return left.CompareTo(right) <= 0;
    }
}
