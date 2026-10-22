using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gallean.Client;

internal sealed partial class ErrorScreenView : UserControl
{
    public ErrorScreenView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
