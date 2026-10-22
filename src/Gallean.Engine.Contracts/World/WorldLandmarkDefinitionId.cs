namespace Gallean.Engine.Contracts;

public readonly record struct WorldLandmarkDefinitionId(string Value) : IComparable<WorldLandmarkDefinitionId>, ISerializableString
{
    public int CompareTo(WorldLandmarkDefinitionId other)
    {
        return string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(WorldLandmarkDefinitionId other)
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
