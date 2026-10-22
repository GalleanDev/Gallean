namespace Gallean.Engine.Contracts;

public readonly record struct WorldStackId(string Value) : IComparable<WorldStackId>, ISerializableString
{
    public int CompareTo(WorldStackId other)
    {
        return string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(WorldStackId other)
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
