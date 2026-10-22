using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gallean.Client;

internal sealed partial class ScenarioConfigurationScreenView : UserControl
{
    public ScenarioConfigurationScreenView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
