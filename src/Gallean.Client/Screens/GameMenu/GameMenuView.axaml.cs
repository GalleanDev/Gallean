using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gallean.Client;

internal sealed partial class GameMenuView : UserControl
{
    public GameMenuView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
