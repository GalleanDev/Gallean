using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gallean.Client;

internal sealed partial class StackLeaderView : UserControl
{
    public StackLeaderView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
