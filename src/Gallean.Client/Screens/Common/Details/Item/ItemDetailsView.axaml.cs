using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gallean.Client;

internal sealed partial class ItemDetailsView : UserControl
{
    public ItemDetailsView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
