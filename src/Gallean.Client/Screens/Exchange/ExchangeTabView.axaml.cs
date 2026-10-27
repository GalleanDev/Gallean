using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Gallean.Client;

internal sealed partial class ExchangeTabView : UserControl
{
    public ExchangeTabView()
    {
        InitializeComponent();

        DragDropBehavior.Attach(this);
        AddHandler(PointerReleasedEvent, OnItemPointerReleased, RoutingStrategies.Bubble, true);
    }

    private void OnItemPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (DataContext is ExchangeTabViewModel viewModel &&
            e.InitialPressMouseButton == MouseButton.Left &&
            e.KeyModifiers.HasFlag(KeyModifiers.Control) &&
            e.Source is StyledElement { DataContext: InventoryItemViewModel item })
        {
            e.Handled = true;

            viewModel.InventoryTransfer.TransferItemAsync(item.GroupReference).FileAndForget();
        }
    }
}
