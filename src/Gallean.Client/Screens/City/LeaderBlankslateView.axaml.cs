using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gallean.Client;

internal sealed partial class LeaderBlankslateView : UserControl
{
    public LeaderBlankslateView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
