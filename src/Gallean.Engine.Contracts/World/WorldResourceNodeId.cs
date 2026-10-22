namespace Gallean.Engine.Contracts;

public readonly record struct WorldResourceNodeId(string Value) : IComparable<WorldResourceNodeId>, ISerializableString
{
    public int CompareTo(WorldResourceNodeId other)
    {
        return string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(WorldResourceNodeId other)
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
