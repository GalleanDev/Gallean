using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace Gallean.Client;

internal sealed partial class LeaderImportScreenView : UserControl
{
    public LeaderImportScreenView()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private LeaderImportScreenViewModel ViewModel => (LeaderImportScreenViewModel)DataContext!;

    private void OnLeaderDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is Control { DataContext: LeaderImportLeaderViewModel leader })
        {
            ViewModel.SelectedLeader = leader;

            if (ViewModel.ImportCommand.CanExecute(null))
            {
                ViewModel.ImportCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
