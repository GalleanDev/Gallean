namespace Gallean.Engine.Contracts;

public readonly record struct ScenarioEventId(string Value) : IComparable<ScenarioEventId>, ISerializableString
{
    public int CompareTo(ScenarioEventId other)
    {
        return string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(ScenarioEventId other)
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
