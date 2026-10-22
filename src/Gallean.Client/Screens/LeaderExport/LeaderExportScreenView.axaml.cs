using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gallean.Client;

internal sealed partial class LeaderExportScreenView : UserControl
{
    public LeaderExportScreenView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
