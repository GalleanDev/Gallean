namespace Gallean.Engine.Contracts;

public readonly record struct UnitAttackDefinitionId(string Value) : IComparable<UnitAttackDefinitionId>, ISerializableString
{
    public int CompareTo(UnitAttackDefinitionId other)
    {
        return string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(UnitAttackDefinitionId other)
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
