namespace Gallean.Engine.Contracts;

public readonly record struct ScenarioId(string Value) : IComparable<ScenarioId>, ISerializableString
{
    public int CompareTo(ScenarioId other)
    {
        return string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(ScenarioId other)
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
