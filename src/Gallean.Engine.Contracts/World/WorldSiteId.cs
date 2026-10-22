namespace Gallean.Engine.Contracts;

public readonly record struct WorldSiteId(string Value) : IComparable<WorldSiteId>, ISerializableString
{
    public int CompareTo(WorldSiteId other)
    {
        return string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(WorldSiteId other)
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
