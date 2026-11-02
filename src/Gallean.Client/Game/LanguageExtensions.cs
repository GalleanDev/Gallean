using Gallean.Configuration;

namespace Gallean.Client;

internal static class LanguageExtensions
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
}
