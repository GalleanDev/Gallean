using System.Security.Cryptography;
using Gallean.Engine.Contracts;

namespace Gallean.Configuration;

public readonly record struct CustomScenarioId(string Value) : ISerializableString
{
    private const int Length = SHA256.HashSizeInBytes * 2;

    public static CustomScenarioId FromContent(ReadOnlySpan<byte> content)
    {
        byte[] hash = SHA256.HashData(content);

        return new CustomScenarioId(Convert.ToHexStringLower(hash));
    }

    public static CustomScenarioId? TryParse(string value)
    {
        if (value.Length == Length && value.All(char.IsAsciiHexDigitLower))
        {
            return new CustomScenarioId(value);
        }
        else
        {
            return null;
        }
    }

    public override string ToString()
    {
        return Value;
    }
}
