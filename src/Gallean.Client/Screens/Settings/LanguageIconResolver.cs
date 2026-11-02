using Gallean.Configuration;

namespace Gallean.Client;

internal static class LanguageIconResolver
{
    public static IconMoniker Resolve(Language language)
    {
        return language switch
        {
            Language.English => IconMoniker.LanguageEnglish,
            Language.Czech => IconMoniker.LanguageCzech,
            Language.French => IconMoniker.LanguageFrench,
            Language.German => IconMoniker.LanguageGerman,
            Language.Italian => IconMoniker.LanguageItalian,
            Language.Polish => IconMoniker.LanguagePolish,
            Language.Russian => IconMoniker.LanguageRussian,
            Language.Spanish => IconMoniker.LanguageSpanish
        };
    }
}
