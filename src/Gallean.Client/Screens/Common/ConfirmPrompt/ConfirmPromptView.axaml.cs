using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gallean.Client;

internal sealed partial class ConfirmPromptView : UserControl
{
    public ConfirmPromptView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
