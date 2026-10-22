namespace Umbra;

public readonly record struct UmbraSequenceId(string Value)
{
    public override string ToString()
    {
        return Value;
    }
}
