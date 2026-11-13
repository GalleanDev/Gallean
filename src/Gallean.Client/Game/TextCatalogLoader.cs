using System.Reflection;

namespace Gallean.Client;

internal static class TextCatalogLoader
{
    public static TextCatalog Load(Language language)
    {
        string name = $"Translations/{language.Code}.json";

        Assembly assembly = typeof(TextCatalogLoader).Assembly;

        using Stream? stream = assembly.GetManifestResourceStream(name);

        if (stream is not null)
        {
            return TextCatalogReader.Read(stream);
        }
        else
        {
            return new TextCatalog([]);
        }
    }
}
