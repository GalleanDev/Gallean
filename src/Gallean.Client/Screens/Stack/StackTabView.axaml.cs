using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace Gallean.Client;

internal sealed partial class StackTabView : UserControl
{
    public StackTabView()
    {
        InitializeComponent();

        DragDropBehavior.Attach(this);
    }

    private void OnInventoryDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is StyledElement { DataContext: InventoryItemViewModel item })
        {
            e.Handled = true;

            item.InvokeAsync().FileAndForget();
        }
    }
}
