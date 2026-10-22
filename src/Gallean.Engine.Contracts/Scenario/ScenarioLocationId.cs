namespace Gallean.Engine.Contracts;

public readonly record struct ScenarioLocationId(string Value) : IComparable<ScenarioLocationId>, ISerializableString
{
    public int CompareTo(ScenarioLocationId other)
    {
        return string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(ScenarioLocationId other)
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
