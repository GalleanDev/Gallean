using System.Globalization;

namespace Gallean.Engine.Contracts;

public static class LanguageExtensions
{
    extension(Language language)
    {
        public string Code => language switch
        {
            Language.English => "en",
            Language.Czech => "cs",
            Language.French => "fr",
            Language.German => "de",
            Language.Italian => "it",
            Language.Polish => "pl",
            Language.Russian => "ru",
            Language.Spanish => "es"
        };
    }

    extension(Language)
    {
        public static Language FromCulture(CultureInfo culture)
        {
            foreach (Language language in Enum.GetValues<Language>())
            {
                if (language.Code == culture.TwoLetterISOLanguageName)
                {
                    return language;
                }
            }

            return Language.English;
        }
    }
}
