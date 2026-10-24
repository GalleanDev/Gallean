using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Gallean.Client;

internal sealed partial class QuestTabView : UserControl
{
    private bool isPinnedToEnd = true;

    public QuestTabView()
    {
        InitializeComponent();

        HistoryScroll.AddHandler(PointerWheelChangedEvent, OnHistoryPointerWheelChanged, RoutingStrategies.Bubble, true);
    }

    private void OnHistoryScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (isPinnedToEnd && e.ExtentDelta.Y > 0)
        {
            HistoryScroll.ScrollToEnd();
        }
    }

    private void OnHistoryPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        isPinnedToEnd = false;
    }
}
