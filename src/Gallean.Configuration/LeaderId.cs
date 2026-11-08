using Gallean.Engine.Contracts;

namespace Gallean.Configuration;

public readonly record struct LeaderId(int Value) : ISerializableInt
{
    public override string ToString()
    {
        return Value.ToString();
    }
}
