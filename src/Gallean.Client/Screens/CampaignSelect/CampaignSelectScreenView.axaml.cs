using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

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
