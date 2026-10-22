namespace Umbra.Prompts;

public readonly record struct UmbraPromptHash(ReadOnlyMemory<byte> Value)
{
    public bool Equals(UmbraPromptHash other)
    {
        return Value.Span.SequenceEqual(other.Value.Span);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.AddBytes(Value.Span);

        return hash.ToHashCode();
    }

    public override string ToString()
    {
        return Convert.ToHexStringLower(Value.Span);
    }
}
