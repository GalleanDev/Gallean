using Avalonia.Controls;

namespace Gallean.Client;

internal sealed partial class MercenaryTabView : UserControl
{
    public MercenaryTabView()
    {
        InitializeComponent();

        DragDropBehavior.Attach(this);
    }
}
