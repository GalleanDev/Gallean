using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gallean.Client;

internal sealed partial class MainMenuBackgroundView : UserControl
{
    public MainMenuBackgroundView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
