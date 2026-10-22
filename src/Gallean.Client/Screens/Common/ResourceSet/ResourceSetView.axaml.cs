using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Gallean.Client;

internal sealed partial class ResourceSetView : UserControl
{
    private const double IconScale = 1.3;

    public static readonly DirectProperty<ResourceSetView, double> IconSizeProperty =
        AvaloniaProperty.RegisterDirect<ResourceSetView, double>(nameof(IconSize), view => view.IconSize);

    private double iconSize;

    public ResourceSetView()
    {
        AvaloniaXamlLoader.Load(this);
        UpdateIconSize();
    }

    public double IconSize
    {
        get => iconSize;
        private set => SetAndRaise(IconSizeProperty, ref iconSize, value);
    }

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
