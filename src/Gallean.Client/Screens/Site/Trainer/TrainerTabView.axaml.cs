using Avalonia.Controls;

namespace Gallean.Client;

internal sealed partial class TrainerTabView : UserControl
{
    public TrainerTabView()
    {
        InitializeComponent();

        DragDropBehavior.Attach(this);
    }
}
