using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gallean.Client;

internal sealed partial class ExitPromptView : UserControl
{
    public ExitPromptView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
