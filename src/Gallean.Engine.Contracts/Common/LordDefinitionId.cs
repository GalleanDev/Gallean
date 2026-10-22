namespace Gallean.Engine.Contracts;

public readonly record struct LordDefinitionId(string Value) : IComparable<LordDefinitionId>, ISerializableString
{
    public int CompareTo(LordDefinitionId other)
    {
        return string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(LordDefinitionId other)
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
