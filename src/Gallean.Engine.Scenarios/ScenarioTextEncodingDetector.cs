using System.Text;

namespace Gallean.Engine.Scenarios;

internal static class ScenarioTextEncodingDetector
{
    private const int CentralEuropeanCodePage = 1250;
    private const int CyrillicCodePage = 1251;

    private const double CyrillicConsecutiveShare = 0.5;
    private const double CentralEuropeanLetterShare = 0.1;

    public static ScenarioTextEncoding DetectionEncoding { get; } = new(Encoding.Latin1);

    public static ScenarioTextEncoding Detect(IEnumerable<string> texts)
    {
        int nonAsciiCount = 0;
        int sequentialNonAsciiCount = 0;
        int centralEuropeanCount = 0;

        foreach (string text in texts)
        {
            char prevCh = '\0';

            foreach (char ch in text)
            {
                if (!char.IsAscii(ch))
                {
                    nonAsciiCount++;

                    if (!char.IsAscii(prevCh))
                    {
                        sequentialNonAsciiCount++;
                    }

                    if (IsCentralEuropeanLetter(ch))
                    {
                        centralEuropeanCount++;
                    }
                }

                prevCh = ch;
            }
        }

        if (sequentialNonAsciiCount > nonAsciiCount * CyrillicConsecutiveShare)
        {
            return ScenarioTextEncoding.FromCodePage(CyrillicCodePage);
        }
        else if (centralEuropeanCount > nonAsciiCount * CentralEuropeanLetterShare)
        {
            return ScenarioTextEncoding.FromCodePage(CentralEuropeanCodePage);
        }
        else
        {
            return ScenarioTextEncoding.FromCodePage(ScenarioTextEncoding.DefaultCodePage);
        }
    }

    public static IEnumerable<string> GetTexts(ScenarioFileTables tables)
    {
        foreach (Db.ScenarioInfo info in tables.ScenarioInfos)
        {
            yield return info.Name;
            yield return info.Briefing;

            if (info.Description is not null)
            {
                yield return info.Description;
            }
        }

        foreach (Db.TextScenario text in tables.TextScenarios)
        {
            if (text.Text is not null)
            {
                yield return text.Text;
            }
        }
    }

    private static bool IsCentralEuropeanLetter(char character)
    {
        return (int)character switch
        {
            0x8a => true,
            0x8c => true,
            0x8d => true,
            0x8e => true,
            0x8f => true,
            0x9a => true,
            0x9c => true,
            0x9d => true,
            0x9e => true,
            0x9f => true,
            0xa3 => true,
            0xa5 => true,
            0xaf => true,
            0xb3 => true,
            0xb9 => true,
            0xd8 => true,
            0xe6 => true,
            0xf8 => true,
            0xfd => true,
            _ => false
        };
    }
}
