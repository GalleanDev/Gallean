using Avalonia.Controls;

namespace Gallean.Client;

internal sealed partial class ExchangeTabView : UserControl
{
    public ExchangeTabView()
    {
        InitializeComponent();

        DragDropBehavior.Attach(this);
    }
}
