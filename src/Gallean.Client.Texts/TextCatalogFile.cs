namespace Gallean.Client.Texts;

internal sealed record TextCatalogFile(int FormatVersion, Dictionary<string, string> Translations);
