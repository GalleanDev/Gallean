using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace Gallean.Client;

internal sealed partial class LoadGameScreenView : UserControl
{
    public LoadGameScreenView()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private LoadGameScreenViewModel ViewModel => (LoadGameScreenViewModel)DataContext!;

    private void OnSaveDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is Control { DataContext: GameSaveViewModel save } &&
            ViewModel.Game.LoadGameCommand.CanExecute(save.Entry))
        {
            ViewModel.Game.LoadGameCommand.Execute(save.Entry);
            e.Handled = true;
        }
    }
}
