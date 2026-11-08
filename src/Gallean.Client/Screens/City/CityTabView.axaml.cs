using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Gallean.Client;

internal sealed partial class CityTabView : UserControl
{
    public CityTabView()
    {
        InitializeComponent();

        DragDropBehavior.Attach(this);
        AddHandler(PointerReleasedEvent, OnItemPointerReleased, RoutingStrategies.Bubble, true);
        AddHandler(PointerPressedEvent, OnTargetingPointerPressed, RoutingStrategies.Bubble);
    }

    private void OnTargetingPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is CityTabViewModel { TargetedAction: not null } viewModel)
        {
            e.Handled = true;

            viewModel.CancelTargetingCommand.Execute(null);
        }
    }

    private void OnItemPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (DataContext is CityTabViewModel viewModel &&
            e.InitialPressMouseButton == MouseButton.Left &&
            e.KeyModifiers.HasFlag(KeyModifiers.Control) &&
            e.Source is StyledElement { DataContext: InventoryItemViewModel item })
        {
            e.Handled = true;

            viewModel.InventoryTransfer.TransferItemAsync(item.GroupReference).FileAndForget();
        }
    }
}
