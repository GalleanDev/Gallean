namespace Gallean.Engine.Contracts;

public readonly record struct WorldSpellDefinitionId(string Value) : IComparable<WorldSpellDefinitionId>, ISerializableString
{
    public int CompareTo(WorldSpellDefinitionId other)
    {
        return string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(WorldSpellDefinitionId other)
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
