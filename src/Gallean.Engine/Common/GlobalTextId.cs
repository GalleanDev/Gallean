namespace Gallean.Engine;

public readonly record struct GlobalTextId(string Value) : IComparable<GlobalTextId>, ISerializableString
{
    public int CompareTo(GlobalTextId other)
    {
        return string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(GlobalTextId other)
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
