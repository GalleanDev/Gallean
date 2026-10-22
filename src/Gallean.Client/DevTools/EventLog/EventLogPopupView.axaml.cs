using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Gallean.Client;

internal sealed partial class EventLogPopupView : UserControl
{
    public EventLogPopupView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        EntriesScrollViewer.ScrollToEnd();
    }
}
