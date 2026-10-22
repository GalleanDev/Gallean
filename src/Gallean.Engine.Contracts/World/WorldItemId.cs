namespace Gallean.Engine.Contracts;

public readonly record struct WorldItemId(string Value) : IComparable<WorldItemId>, ISerializableString
{
    public int CompareTo(WorldItemId other)
    {
        return string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(WorldItemId other)
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
