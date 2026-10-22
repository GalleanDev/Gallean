namespace Gallean.Engine.Contracts;

public readonly record struct BattleUnitId(int Value) : IComparable<BattleUnitId>, ISerializableInt
{
    public int CompareTo(BattleUnitId other)
    {
        return Value.CompareTo(other.Value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
