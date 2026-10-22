namespace Gallean.Engine.Contracts;

public readonly record struct WorldUnitId(string Value) : IComparable<WorldUnitId>, ISerializableString
{
    public int CompareTo(WorldUnitId other)
    {
        return string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(WorldUnitId other)
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
