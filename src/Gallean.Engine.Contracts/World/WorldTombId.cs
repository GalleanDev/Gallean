namespace Gallean.Engine.Contracts;

public readonly record struct WorldTombId(string Value) : IComparable<WorldTombId>, ISerializableString
{
    public int CompareTo(WorldTombId other)
    {
        return string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(WorldTombId other)
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
