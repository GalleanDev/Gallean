using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;

namespace Gallean.Client;

internal sealed partial class AssetPackErrorWindow : Window
{
    public AssetPackErrorWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private AssetPackErrorViewModel ViewModel => (AssetPackErrorViewModel)DataContext!;

    private void OnOpenFolderClick(object? sender, RoutedEventArgs e)
    {
        DirectoryInfo directory = new(ViewModel.Location);

        Launcher.LaunchDirectoryInfoAsync(directory).FileAndForget();
    }
}
