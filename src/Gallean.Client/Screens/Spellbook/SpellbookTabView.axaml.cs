using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace Gallean.Client;

internal sealed partial class SpellbookTabView : UserControl
{
    public SpellbookTabView()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private SpellbookTabViewModel ViewModel => (SpellbookTabViewModel)DataContext!;

    private void OnContentPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (ViewModel.SelectedSpell is not null)
        {
            ViewModel.ClearSelectionCommand.Execute(null);
        }
    }

    private void OnSpellDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (ViewModel.CastSelectedCommand.CanExecute(null))
        {
            ViewModel.CastSelectedCommand.Execute(null);
            e.Handled = true;
        }
    }
}
