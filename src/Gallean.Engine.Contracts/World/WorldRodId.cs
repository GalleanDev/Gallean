namespace Gallean.Engine.Contracts;

public readonly record struct WorldRodId(string Value) : IComparable<WorldRodId>, ISerializableString
{
    public int CompareTo(WorldRodId other)
    {
        return string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(WorldRodId other)
    {
        return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        return StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
    }

    public override string ToString()
    {
        return Value;
    }
}
