using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gallean.Client;

internal sealed partial class SettingsScreenView : UserControl
{
    public SettingsScreenView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
