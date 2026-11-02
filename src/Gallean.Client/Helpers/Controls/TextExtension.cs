using Avalonia.Data;

namespace Gallean.Client;

internal sealed class TextExtension(string text)
{
    public BindingBase ProvideValue()
    {
        TextKey source = TextKey.For(text);

        return CompiledBinding.Create<TextKey, string>(key => key.Translation, source);
    }
}
