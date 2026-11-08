using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;

namespace Gallean.Client;

internal sealed partial class UpdatePromptView : UserControl
{
    public UpdatePromptView()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private UpdatePromptViewModel ViewModel => (UpdatePromptViewModel)DataContext!;

    private void OnDownloadClick(object? sender, RoutedEventArgs e)
    {
        TopLevel? topLevel = TopLevel.GetTopLevel(this);

        if (topLevel is not null)
        {
            ILauncher launcher = topLevel.Launcher;
            launcher.LaunchUriAsync(ViewModel.ReleaseUri).FileAndForget();
        }

        ViewModel.Close();
    }
}
