namespace Gallean.Engine.Contracts;

public readonly record struct WorldSubraceId(string Value) : IComparable<WorldSubraceId>, ISerializableString
{
    public int CompareTo(WorldSubraceId other)
    {
        return string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(WorldSubraceId other)
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
