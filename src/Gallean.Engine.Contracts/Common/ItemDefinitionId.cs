namespace Gallean.Engine.Contracts;

public readonly record struct ItemDefinitionId(string Value) : IComparable<ItemDefinitionId>, ISerializableString
{
    public int CompareTo(ItemDefinitionId other)
    {
        return string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(ItemDefinitionId other)
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
