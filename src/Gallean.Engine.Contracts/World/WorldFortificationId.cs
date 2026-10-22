namespace Gallean.Engine.Contracts;

public readonly record struct WorldFortificationId(string Value) : IComparable<WorldFortificationId>, ISerializableString
{
    public int CompareTo(WorldFortificationId other)
    {
        return string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(WorldFortificationId other)
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
