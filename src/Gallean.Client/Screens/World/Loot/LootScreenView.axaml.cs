using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gallean.Client;

internal sealed partial class LootScreenView : UserControl
{
    public LootScreenView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
