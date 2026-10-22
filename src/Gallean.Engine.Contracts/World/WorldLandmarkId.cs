namespace Gallean.Engine.Contracts;

public readonly record struct WorldLandmarkId(string Value) : IComparable<WorldLandmarkId>, ISerializableString
{
    public int CompareTo(WorldLandmarkId other)
    {
        return string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(WorldLandmarkId other)
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
