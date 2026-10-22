using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Gallean.Client;

internal sealed partial class ScenarioEventPopupView : UserControl
{
    public ScenarioEventPopupView()
    {
        InitializeComponent();

        MessagesScroll.AddHandler(PointerWheelChangedEvent, OnMessagesPointerWheelChanged, RoutingStrategies.Bubble, true);
    }

    private void OnMessagesScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (e.ExtentDelta.Y > 0)
        {
            MessagesScroll.ScrollToEnd();
            HideHistory();
        }
        else if (e.OffsetDelta.Y < 0)
        {
            ShowHistory();
        }
        else if (e.OffsetDelta.Y > 0 && IsScrollAtEnd())
        {
            HideHistory();
        }
    }

    private void OnMessagesPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (e.Delta.Y > 0)
        {
            ShowHistory();
        }
        else if (e.Delta.Y < 0 && IsScrollAtEnd())
        {
            HideHistory();
        }
    }

    private void ShowHistory()
    {
        MessageLines.Classes.Add("history");
    }

    private void HideHistory()
    {
        MessageLines.Classes.Remove("history");
    }

    private bool IsScrollAtEnd()
    {
        return MessagesScroll.Offset.Y >= MessagesScroll.Extent.Height - MessagesScroll.Viewport.Height - 1;
    }
}
