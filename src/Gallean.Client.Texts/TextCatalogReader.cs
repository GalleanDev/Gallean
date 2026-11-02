using Gallean.Common;

namespace Gallean.Client.Texts;

public static class TextCatalogReader
{
    public const int CurrentFormatVersion = 1;

    public static TextCatalog Read(Stream stream)
    {
        TextCatalogFile file = TextCatalogJsonSerializerContext.Default.TextCatalogFile.DeserializeRequired(stream);

        if (file.FormatVersion != CurrentFormatVersion)
        {
            throw new InvalidDataException($"Unsupported text catalog format '{file.FormatVersion}'.");
        }

        return new TextCatalog(file.Translations);
    }
}
