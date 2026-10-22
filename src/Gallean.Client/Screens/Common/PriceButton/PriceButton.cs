using Avalonia;
using Avalonia.Controls;

namespace Gallean.Client;

internal sealed class PriceButton : Button
{
    private const int MaxPriceComponents = 2;

    public static readonly StyledProperty<ResourceSetViewModel?> PriceProperty =
        AvaloniaProperty.Register<PriceButton, ResourceSetViewModel?>(nameof(Price));

    public static readonly StyledProperty<OfferStatusViewModel?> StatusProperty =
        AvaloniaProperty.Register<PriceButton, OfferStatusViewModel?>(nameof(Status));

    public static readonly DirectProperty<PriceButton, object?> PriceContentProperty =
        AvaloniaProperty.RegisterDirect<PriceButton, object?>(nameof(PriceContent), button => button.PriceContent);

    public ResourceSetViewModel? Price
    {
        get => GetValue(PriceProperty);
        set => SetValue(PriceProperty, value);
    }

    public OfferStatusViewModel? Status
    {
        get => GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    public object? PriceContent
    {
        get;
        private set => SetAndRaise(PriceContentProperty, ref field, value);
    }

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
