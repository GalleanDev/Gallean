namespace Gallean.Client.Texts;

public abstract class TextKey(string text)
{
    public string Text { get; } = text;

    protected string Translate()
    {
        return TextCatalog.Current.Translate(Text);
    }
}
