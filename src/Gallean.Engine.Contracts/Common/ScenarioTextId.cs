namespace Gallean.Engine.Contracts;

public readonly record struct ScenarioTextId(string Value) : IComparable<ScenarioTextId>, ISerializableString
{
    public int CompareTo(ScenarioTextId other)
    {
        return string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(ScenarioTextId other)
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
