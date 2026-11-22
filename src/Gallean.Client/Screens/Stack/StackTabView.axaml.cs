using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Gallean.Client;

internal sealed partial class StackTabView : UserControl
{
    public StackTabView()
    {
        InitializeComponent();

        DragDropBehavior.Attach(this);
        AddHandler(PointerPressedEvent, OnTargetingPointerPressed, RoutingStrategies.Bubble);
    }

    private StackTabViewModel ViewModel => (StackTabViewModel)DataContext!;

    private void OnTargetingPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ViewModel.TargetedAction is not null)
        {
            e.Handled = true;

            ViewModel.CancelTargetingCommand.Execute(null);
        }
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
