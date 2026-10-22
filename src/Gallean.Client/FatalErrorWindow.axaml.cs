using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;

namespace Gallean.Client;

internal sealed partial class FatalErrorWindow : Window
{
    public FatalErrorWindow()
    {
        InitializeComponent();
    }

    public FatalErrorWindow(string title, string message, string? logLocation)
        : this()
    {
        TitleText.Text = title;
        MessageText.Text = message;
        LogLocationText.Text = logLocation ?? Tr("<No log>");
    }

    private void OnExitClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
        else
        {
            Close();
        }
    }
}
