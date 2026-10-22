using Avalonia.Controls;

namespace Gallean.Client;

internal sealed partial class CityTabView : UserControl
{
    public CityTabView()
    {
        InitializeComponent();

        DragDropBehavior.Attach(this);
    }
}
