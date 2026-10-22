namespace Gallean.Client;

internal sealed class TextExtension(string text) : TextKey(text)
{
    public string ProvideValue()
    {
        return Translate();
    }
}
