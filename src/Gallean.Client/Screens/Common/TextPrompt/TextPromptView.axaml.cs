using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gallean.Client;

internal sealed partial class TextPromptView : UserControl
{
    public TextPromptView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
