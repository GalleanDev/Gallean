namespace Gallean.Engine.Contracts;

public readonly record struct WorldPlayerBuildingDefinitionId(string Value) : IComparable<WorldPlayerBuildingDefinitionId>, ISerializableString
{
    public int CompareTo(WorldPlayerBuildingDefinitionId other)
    {
        return string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(WorldPlayerBuildingDefinitionId other)
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
