using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace Gallean.Client;

internal sealed partial class SpellItemsView : UserControl
{
    public SpellItemsView()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private SpellItemsViewModel ViewModel => (SpellItemsViewModel)DataContext!;

    private void OnContentPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ViewModel.SelectedItem is not null)
        {
            ViewModel.ClearSelectionCommand.Execute(null);
        }
    }

    private void OnItemDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (ViewModel.UseSelectedCommand.CanExecute(null))
        {
            ViewModel.UseSelectedCommand.Execute(null);
            e.Handled = true;
        }
    }
}
