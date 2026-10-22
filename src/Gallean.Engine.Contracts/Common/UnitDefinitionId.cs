namespace Gallean.Engine.Contracts;

public readonly record struct UnitDefinitionId(string Value) : IComparable<UnitDefinitionId>, ISerializableString
{
    public int CompareTo(UnitDefinitionId other)
    {
        return string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(UnitDefinitionId other)
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
