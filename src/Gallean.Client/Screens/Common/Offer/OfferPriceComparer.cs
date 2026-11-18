namespace Gallean.Client;

internal sealed class OfferPriceComparer(bool descending) : IComparer<WorldResourceSet>
{
    public static OfferPriceComparer Ascending { get; } = new(descending: false);
    public static OfferPriceComparer Descending { get; } = new(descending: true);

    public int Compare(WorldResourceSet left, WorldResourceSet right)
    {
        if (descending)
        {
            return right.Gold.CompareTo(left.Gold);
        }
        else
        {
            return left.Gold.CompareTo(right.Gold);
        }
    }
}
