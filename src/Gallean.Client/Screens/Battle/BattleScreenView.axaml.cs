using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Gallean.Client;

internal sealed partial class BattleScreenView : UserControl
{
    private const double ItemDetailsGap = 12;
    private const double UnitDetailsGap = 16;

    public BattleScreenView()
    {
        InitializeComponent();

        Battlefield.AddHandler(PointerPressedEvent, OnBattlefieldPointerPressed, RoutingStrategies.Bubble, true);
        Battlefield.AddHandler(PointerReleasedEvent, OnBattlefieldPointerReleased, RoutingStrategies.Bubble, true);
        Battlefield.AddHandler(PointerCaptureLostEvent, OnBattlefieldPointerCaptureLost, RoutingStrategies.Bubble, true);
    }

    private BattleScreenViewModel ViewModel => (BattleScreenViewModel)DataContext!;

    private void OnBattlefieldPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        PointerPoint point = e.GetCurrentPoint(this);

        if (point.Properties.IsRightButtonPressed && ViewModel.InspectedUnit is not null)
        {
            BattlefieldUnitDetails.IsOpen = true;
        }
    }

    private void OnBattlefieldPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Right)
        {
            BattlefieldUnitDetails.IsOpen = false;
        }
    }

    private void OnBattlefieldPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        BattlefieldUnitDetails.IsOpen = false;
    }

    private void OnGridUnitPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: BattleGridUnitViewModel unit } control)
        {
            PointerPoint point = e.GetCurrentPoint(this);

            if (point.Properties.IsRightButtonPressed)
            {
                ViewModel.ShowUnitDetails(unit.UnitId);
                SetDetailsOpen(control, true);
                e.Pointer.Capture(control);
                e.Handled = true;
            }
            else if (point.Properties.IsLeftButtonPressed)
            {
                ViewModel.ClickAsync(unit.Tile).FileAndForget();
                e.Handled = true;
            }
        }
    }

    private void OnGridUnitPointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is Control { DataContext: BattleGridUnitViewModel unit })
        {
            ViewModel.Hover(unit.Tile);
        }
    }

    private void OnGridTilePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: BattleGridTileViewModel tile })
        {
            PointerPoint point = e.GetCurrentPoint(this);

            if (point.Properties.IsLeftButtonPressed)
            {
                ViewModel.ClickAsync(tile.Tile).FileAndForget();
                e.Handled = true;
            }
        }
    }

    private void OnGridTilePointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is Control { DataContext: BattleGridTileViewModel tile })
        {
            ViewModel.Hover(tile.Tile);
        }
    }

    private void OnGridPointerExited(object? sender, PointerEventArgs e)
    {
        if (e.Pointer.Captured is null)
        {
            ViewModel.Hover(null);
        }
    }

    private void OnGridUnitPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Right)
        {
            ViewModel.HideUnitDetails();
            SetDetailsOpen(sender, false);
        }
    }

    private void OnGridUnitPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        ViewModel.HideUnitDetails();
        SetDetailsOpen(sender, false);
    }

    private void OnItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: BattleItemViewModel item } control)
        {
            PointerPoint point = e.GetCurrentPoint(this);

            if (point.Properties.IsRightButtonPressed)
            {
                ViewModel.ShowItemDetails(item.Slot);
                SetDetailsOpen(control, true);
                e.Pointer.Capture(control);
                e.Handled = true;
            }
        }
    }

    private void OnItemPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Right)
        {
            ViewModel.HideItemDetails();
            SetDetailsOpen(sender, false);
        }
    }

    private void OnItemPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        ViewModel.HideItemDetails();
        SetDetailsOpen(sender, false);
    }

    private static void SetDetailsOpen(object? sender, bool isOpen)
    {
        if (sender is Panel panel)
        {
            foreach (Popup popup in panel.Children.OfType<Popup>())
            {
                popup.IsOpen = isOpen;
            }
        }
    }

    private static void PlaceItemDetails(CustomPopupPlacement placement)
    {
        placement.ConstraintAdjustment = PopupPositionerConstraintAdjustment.SlideX;
        placement.Anchor = PopupAnchor.Top;
        placement.Gravity = PopupGravity.Top;
        placement.Offset = new Point(0, -ItemDetailsGap * UiScaleDecorator.GetScalingFactor(placement.Target));
    }

    private static void PlaceUnitDetails(CustomPopupPlacement placement)
    {
        placement.ConstraintAdjustment = PopupPositionerConstraintAdjustment.SlideY;

        Visual target = placement.Target;
        TopLevel? topLevel = TopLevel.GetTopLevel(target);
        Visual? grid = target.FindAncestorOfType<ItemsControl>();

        if (topLevel is not null && grid is not null)
        {
            Matrix? unitTransform = target.TransformToVisual(topLevel);
            Matrix? gridTransform = grid.TransformToVisual(topLevel);

            if (unitTransform is not null && gridTransform is not null)
            {
                Rect unitRect = new(target.Bounds.Size);
                Rect gridRect = new(grid.Bounds.Size);
                Rect unitBounds = unitRect.TransformToAABB(unitTransform.Value);
                Rect gridBounds = gridRect.TransformToAABB(gridTransform.Value);
                double gap = UnitDetailsGap * UiScaleDecorator.GetScalingFactor(target);

                if (unitBounds.Center.X < topLevel.ClientSize.Width / 2.0)
                {
                    placement.Anchor = PopupAnchor.Right;
                    placement.Gravity = PopupGravity.Right;
                    placement.Offset = new Point(gridBounds.Right - unitBounds.Right + gap, 0);
                }
                else
                {
                    placement.Anchor = PopupAnchor.Left;
                    placement.Gravity = PopupGravity.Left;
                    placement.Offset = new Point(gridBounds.Left - unitBounds.Left - gap, 0);
                }
            }
        }
    }
}
