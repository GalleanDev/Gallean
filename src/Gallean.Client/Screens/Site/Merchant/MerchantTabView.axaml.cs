using Avalonia.Controls;

namespace Gallean.Client;

internal sealed partial class MerchantTabView : UserControl
{
    public MerchantTabView()
    {
        InitializeComponent();

        DragDropBehavior.Attach(this);
    }
}
