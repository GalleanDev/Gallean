using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gallean.Client;

internal sealed partial class SummonView : UserControl
{
    public SummonView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
