using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace Gallean.Client;

internal sealed partial class LeaderExportSelectScreenView : UserControl
{
    public LeaderExportSelectScreenView()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private LeaderExportSelectScreenViewModel ViewModel => (LeaderExportSelectScreenViewModel)DataContext!;

    private void OnLeaderDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is Control { DataContext: LeaderExportLeaderViewModel leader })
        {
            ViewModel.SelectedLeader = leader;

            if (ViewModel.ExportCommand.CanExecute(null))
            {
                ViewModel.ExportCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
