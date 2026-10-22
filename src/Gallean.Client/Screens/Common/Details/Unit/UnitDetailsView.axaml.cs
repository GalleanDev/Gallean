using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gallean.Client;

internal sealed partial class UnitDetailsView : UserControl
{
    public UnitDetailsView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
