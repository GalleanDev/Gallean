using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Gallean.Client;

internal sealed partial class BattleLogPopupView : UserControl
{
    public BattleLogPopupView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        EntriesScrollViewer.ScrollToEnd();
    }
}
