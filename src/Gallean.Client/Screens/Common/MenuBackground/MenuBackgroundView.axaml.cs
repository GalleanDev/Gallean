using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gallean.Client;

internal sealed partial class MenuBackgroundView : UserControl
{
    public MenuBackgroundView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
