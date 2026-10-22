namespace Gallean.Engine.Contracts;

public readonly record struct ScenarioStackTemplateId(string Value) : IComparable<ScenarioStackTemplateId>, ISerializableString
{
    public int CompareTo(ScenarioStackTemplateId other)
    {
        return string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(ScenarioStackTemplateId other)
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
