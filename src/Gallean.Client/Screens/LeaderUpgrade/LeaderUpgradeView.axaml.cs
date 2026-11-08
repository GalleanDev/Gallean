using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace Gallean.Client;

internal sealed partial class LeaderUpgradeView : UserControl
{
    public LeaderUpgradeView()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private LeaderUpgradeViewModel ViewModel => (LeaderUpgradeViewModel)DataContext!;

    private void OnUpgradeDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (ViewModel.LearnCommand.CanExecute(null))
        {
            ViewModel.LearnCommand.Execute(null);
            e.Handled = true;
        }
    }
}
