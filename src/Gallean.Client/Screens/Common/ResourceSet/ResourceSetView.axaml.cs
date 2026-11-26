using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gallean.Client;

internal sealed partial class ResourceSetView : UserControl
{
    private const double IconScale = 1.3;

    public ResourceSetView()
    {
        AvaloniaXamlLoader.Load(this);
        UpdateIconSize();
    }

    [GeneratedDirectProperty]
    public partial double IconSize { get; private set; }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == FontSizeProperty)
        {
            UpdateIconSize();
        }
    }

    private void UpdateIconSize()
    {
        IconSize = Math.Round(FontSize * IconScale);
    }
}
