namespace Gallean.Engine.Contracts;

public readonly record struct WorldPlayerId(int Value) : IComparable<WorldPlayerId>, ISerializableInt
{
    public int CompareTo(WorldPlayerId other)
    {
        return Value.CompareTo(other.Value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
