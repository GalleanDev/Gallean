namespace Gallean.Client;

internal sealed class OfferStatusViewModel
{
    public OfferStatusViewModel(string? unavailableReason)
    {
        Price = null;
        UnavailableReason = unavailableReason;
    }

    public OfferStatusViewModel(OfferPriceViewModel price, string? unavailableReason)
    {
        Price = price;
        UnavailableReason = unavailableReason;
    }

    public OfferPriceViewModel? Price { get; }
    public string? UnavailableReason { get; }

    public bool IsAvailable => UnavailableReason is null && (Price is null || !Price.IsShort);
    public bool IsReasonVisible => UnavailableReason is not null;
    public bool IsPriceVisible => Price is not null && UnavailableReason is null;
    public bool IsVisible => IsPriceVisible || IsReasonVisible;
}
