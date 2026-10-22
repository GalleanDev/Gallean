using System.Text;

namespace Extractor;

internal sealed class ScenarioTextEncoding(Encoding encoding)
{
    public const int DefaultCodePage = 1252;

    public static ScenarioTextEncoding FromCodePage(int codePage)
    {
        ScenarioTextEncoding? scenarioTextEncoding = TryFromCodePage(codePage);

        if (scenarioTextEncoding is null)
        {
            throw new InvalidDataException($"Unknown scenario text code page '{codePage}'.");
        }

        return scenarioTextEncoding;
    }

    public static ScenarioTextEncoding? TryFromCodePage(int codePage)
    {
        if (codePage is >= 0 and <= 65535)
        {
            Encoding? encoding = CodePagesEncodingProvider.Instance.GetEncoding(codePage);

            if (encoding is not null)
            {
                return new ScenarioTextEncoding(encoding);
            }
        }

        return null;
    }

    public string MakeNullTerminatedString(ReadOnlySpan<byte> bytes)
    {
        int end = bytes.IndexOf((byte)0);

        if (end >= 0)
        {
            return GetString(bytes[..end]);
        }
        else
        {
            return GetString(bytes);
        }
    }

    public string GetString(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
        {
            return string.Empty;
        }
        else
        {
            return encoding.GetString(bytes);
        }
    }
}
