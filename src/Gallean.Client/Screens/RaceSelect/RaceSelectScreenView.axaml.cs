using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gallean.Client;

internal sealed partial class RaceSelectScreenView : UserControl
{
    public RaceSelectScreenView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
