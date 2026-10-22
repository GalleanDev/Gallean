using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gallean.Client;

internal sealed partial class OfferStatusView : UserControl
{
    public OfferStatusView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
