using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gallean.Client;

internal sealed partial class BriefingScreenView : UserControl
{
    public BriefingScreenView()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
