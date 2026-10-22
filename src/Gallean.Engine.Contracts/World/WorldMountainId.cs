namespace Gallean.Engine.Contracts;

public readonly record struct WorldMountainId(int Value) : IComparable<WorldMountainId>, ISerializableInt
{
    public int CompareTo(WorldMountainId other)
    {
        return Value.CompareTo(other.Value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
