namespace Gallean.Engine.Contracts;

public readonly record struct ScenarioEventPortraitId(string Value) : IComparable<ScenarioEventPortraitId>, ISerializableString
{
    public static ScenarioEventPortraitId Unit(UnitDefinitionId unitDefinitionId)
    {
        return new ScenarioEventPortraitId(unitDefinitionId.Value);
    }

    public int CompareTo(ScenarioEventPortraitId other)
    {
        return string.Compare(Value, other.Value, StringComparison.OrdinalIgnoreCase);
    }

    public bool Equals(ScenarioEventPortraitId other)
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
