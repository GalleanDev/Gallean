using Avalonia.Controls;

namespace Gallean.Client;

internal sealed partial class StackTabView : UserControl
{
    public StackTabView()
    {
        InitializeComponent();

        DragDropBehavior.Attach(this);
    }
}
