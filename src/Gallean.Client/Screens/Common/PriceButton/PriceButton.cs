using Avalonia;
using Avalonia.Controls;

namespace Gallean.Client;

internal sealed partial class PriceButton : Button
{
    private const int MaxPriceComponents = 2;

    [GeneratedStyledProperty]
    public partial ResourceSetViewModel? Price { get; set; }

    [GeneratedStyledProperty]
    public partial OfferStatusViewModel? Status { get; set; }

    [GeneratedDirectProperty]
    public partial object? PriceContent { get; private set; }

    protected override Type StyleKeyOverride => typeof(Button);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == PriceProperty || e.Property == StatusProperty)
        {
            UpdatePriceContent();
        }
    }

    private void UpdatePriceContent()
    {
        int componentCount;

        if (Status is not null)
        {
            PriceContent = Status.Price;
            componentCount = GetComponentCount(Status.Price);
        }
        else
        {
            PriceContent = Price;
            componentCount = GetComponentCount(Price);
        }

        PseudoClasses.Set(":has-price", PriceContent is not null);
        PseudoClasses.Set(":long-price", componentCount > MaxPriceComponents);
    }

    private static int GetComponentCount(OfferPriceViewModel? price)
    {
        if (price is not null)
        {
            return price.Items.Count;
        }
        else
        {
            return 0;
        }
    }

    private static int GetComponentCount(ResourceSetViewModel? price)
    {
        if (price is not null)
        {
            return price.Items.Count;
        }
        else
        {
            return 0;
        }
    }
}
