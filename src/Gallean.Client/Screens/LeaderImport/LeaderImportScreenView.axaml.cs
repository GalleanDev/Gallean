using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gallean.Client;

internal sealed partial class LeaderImportScreenView : UserControl
{
    public LeaderImportScreenView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
