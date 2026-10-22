namespace Gallean.Engine.Contracts;

public readonly record struct WorldObjectVariant(int Value) : IComparable<WorldObjectVariant>, ISerializableInt
{
    public int CompareTo(WorldObjectVariant other)
    {
        return Value.CompareTo(other.Value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
