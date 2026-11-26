using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Gallean.Common;

namespace Gallean.Client;

internal sealed partial class ScenarioSelectScreenView : UserControl
{
    public ScenarioSelectScreenView()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private ScenarioSelectScreenViewModel ViewModel => (ScenarioSelectScreenViewModel)DataContext!;

    private void OnImportClick(object? sender, RoutedEventArgs e)
    {
        ImportAsync().FileAndForget();
    }

    private async Task ImportAsync()
    {
        IReadOnlyList<ScenarioImportFile> files = await ScenarioImportFilePicker.PickAsync(
            this,
            Tr("Import quests"),
            Tr("Disciples II quests"),
            ["*.sg", "*.SG"]);

        await ViewModel.ImportCommand.ExecuteAsync(files);
    }

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
