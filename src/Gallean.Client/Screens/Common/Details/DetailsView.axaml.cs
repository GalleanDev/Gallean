using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gallean.Client;

internal sealed partial class DetailsView : UserControl
{
    public DetailsView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
