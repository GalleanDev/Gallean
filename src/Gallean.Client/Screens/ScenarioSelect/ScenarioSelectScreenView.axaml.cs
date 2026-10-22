using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace Gallean.Client;

internal sealed partial class ScenarioSelectScreenView : UserControl
{
    public ScenarioSelectScreenView()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private ScenarioSelectScreenViewModel ViewModel => (ScenarioSelectScreenViewModel)DataContext!;

    private void OnScenarioDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is Control { DataContext: GameScenario } &&
            ViewModel.StartCommand.CanExecute(null))
        {
            ViewModel.StartCommand.Execute(null);
            e.Handled = true;
        }
    }
}
