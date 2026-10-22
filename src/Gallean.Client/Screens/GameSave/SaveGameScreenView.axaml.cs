using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace Gallean.Client;

internal sealed partial class SaveGameScreenView : UserControl
{
    public SaveGameScreenView()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private SaveGameScreenViewModel ViewModel => (SaveGameScreenViewModel)DataContext!;

    private void OnSaveDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is Control { DataContext: GameSaveViewModel } &&
            ViewModel.SaveCommand.CanExecute(null))
        {
            ViewModel.SaveCommand.Execute(null);
            e.Handled = true;
        }
    }
}
