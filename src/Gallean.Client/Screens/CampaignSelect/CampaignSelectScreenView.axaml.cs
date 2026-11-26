using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Gallean.Common;

namespace Gallean.Client;

internal sealed partial class CampaignSelectScreenView : UserControl
{
    public CampaignSelectScreenView()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private CampaignSelectScreenViewModel ViewModel => (CampaignSelectScreenViewModel)DataContext!;

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is not null)
        {
            ViewModel.RevealBackgroundCommand.Execute(null);
        }
    }

    private void OnImportClick(object? sender, RoutedEventArgs e)
    {
        ImportAsync().FileAndForget();
    }

    private async Task ImportAsync()
    {
        IReadOnlyList<ScenarioImportFile> files = await ScenarioImportFilePicker.PickAsync(
            this,
            Tr("Import sagas"),
            Tr("Disciples II sagas"),
            ["*.csg", "*.CSG"]);

        await ViewModel.ImportCommand.ExecuteAsync(files);
    }

    private void OnCampaignDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is Control { DataContext: CampaignSelectItemViewModel } &&
            ViewModel.StartCommand.CanExecute(null))
        {
            ViewModel.StartCommand.Execute(null);
            e.Handled = true;
        }
    }
}
