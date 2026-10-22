namespace Gallean.Engine.Contracts;

public readonly record struct WorldBagId(string Value) : IComparable<WorldBagId>, ISerializableString
{
    public int CompareTo(WorldBagId other)
    {
        return string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(WorldBagId other)
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
