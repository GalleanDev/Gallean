namespace Gallean.Engine.Contracts;

public readonly record struct ScenarioVariableId(int Value) : IComparable<ScenarioVariableId>, ISerializableInt
{
    public int CompareTo(ScenarioVariableId other)
    {
        return Value.CompareTo(other.Value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
