using Avalonia.Controls;

namespace Gallean.Client;

internal sealed partial class MageTabView : UserControl
{
    public MageTabView()
    {
        InitializeComponent();

        DragDropBehavior.Attach(this);
    }
}
