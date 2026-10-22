using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gallean.Client;

internal sealed partial class CapitalSceneView : UserControl
{
    public CapitalSceneView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
