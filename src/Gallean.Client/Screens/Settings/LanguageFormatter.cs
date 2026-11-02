using Gallean.Configuration;

namespace Gallean.Client;

internal static class LanguageFormatter
{
    public static string Format(Language language)
    {
        return language switch
        {
            Language.English => "English",
            Language.Czech => "Čeština",
            Language.French => "Français",
            Language.German => "Deutsch",
            Language.Italian => "Italiano",
            Language.Polish => "Polski",
            Language.Russian => "Русский",
            Language.Spanish => "Español"
        };
    }
}
