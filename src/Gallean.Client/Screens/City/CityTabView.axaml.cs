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

    private CityTabViewModel ViewModel => (CityTabViewModel)DataContext!;

    private void OnTargetingPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ViewModel.TargetedAction is not null)
        {
            e.Handled = true;

            ViewModel.CancelTargetingCommand.Execute(null);
        }
    }

    private void OnItemPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Left &&
            e.KeyModifiers.HasFlag(KeyModifiers.Control) &&
            e.Source is StyledElement { DataContext: InventoryItemViewModel item })
        {
            e.Handled = true;

            ViewModel.InventoryTransfer.TransferItemAsync(item.GroupReference).FileAndForget();
        }
    }
}
