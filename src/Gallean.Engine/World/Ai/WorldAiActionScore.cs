namespace Gallean.Engine;

public readonly record struct WorldAiActionScore(int Priority, int Value) : IComparable<WorldAiActionScore>
{
    public int CompareTo(WorldAiActionScore other)
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

    public static bool operator >(WorldAiActionScore left, WorldAiActionScore right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator <(WorldAiActionScore left, WorldAiActionScore right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator >=(WorldAiActionScore left, WorldAiActionScore right)
    {
        return left.CompareTo(right) >= 0;
    }

    public static bool operator <=(WorldAiActionScore left, WorldAiActionScore right)
    {
        return left.CompareTo(right) <= 0;
    }
}
