using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Gallean.Client;

internal sealed partial class DevToolsView : UserControl
{
    private readonly TranslateTransform offset = new();
    private Point? dragAnchor;

    public DevToolsView()
    {
        InitializeComponent();

        Frame.RenderTransform = offset;
        Frame.PointerPressed += OnFramePointerPressed;
        Frame.PointerMoved += OnFramePointerMoved;
        Frame.PointerReleased += OnFramePointerReleased;
        Frame.PointerCaptureLost += OnFramePointerCaptureLost;
        Switches.PointerPressed += OnSwitchesPointerPressed;
    }

    private void OnFramePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        PointerPoint point = e.GetCurrentPoint(this);

        if (point.Properties.IsLeftButtonPressed)
        {
            dragAnchor = point.Position - new Point(offset.X, offset.Y);
            e.Pointer.Capture(Frame);
            e.Handled = true;
        }
    }

    private void OnFramePointerMoved(object? sender, PointerEventArgs e)
    {
        if (dragAnchor is { } anchor)
        {
            Point position = e.GetPosition(this);
            Rect bounds = Frame.Bounds;
            double minimumX = -bounds.X;
            double minimumY = -bounds.Y;
            double maximumX = Math.Max(minimumX, Bounds.Width - bounds.Right);
            double maximumY = Math.Max(minimumY, Bounds.Height - bounds.Bottom);
            offset.X = Math.Clamp(position.X - anchor.X, minimumX, maximumX);
            offset.Y = Math.Clamp(position.Y - anchor.Y, minimumY, maximumY);
            e.Handled = true;
        }
    }

    private void OnFramePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (dragAnchor is not null)
        {
            dragAnchor = null;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    private void OnFramePointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        dragAnchor = null;
    }

    private void OnSwitchesPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
    }
}
