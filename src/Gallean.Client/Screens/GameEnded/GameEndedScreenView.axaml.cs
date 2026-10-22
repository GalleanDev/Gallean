using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gallean.Client;

internal sealed partial class GameEndedScreenView : UserControl
{
    public GameEndedScreenView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
