using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gallean.Client;

internal sealed partial class LoadingScreenView : UserControl
{
    public LoadingScreenView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
