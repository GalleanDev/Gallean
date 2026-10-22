namespace Gallean.Engine.Contracts;

public readonly record struct UnitModifierDefinitionId(string Value) : IComparable<UnitModifierDefinitionId>, ISerializableString
{
    public int CompareTo(UnitModifierDefinitionId other)
    {
        return string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(UnitModifierDefinitionId other)
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
