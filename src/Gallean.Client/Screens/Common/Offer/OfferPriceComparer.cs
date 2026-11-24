namespace Gallean.Client;

internal sealed class OfferPriceComparer(bool descending) : IComparer<WorldResourceSet>
{
    public static OfferPriceComparer Ascending { get; } = new(descending: false);
    public static OfferPriceComparer Descending { get; } = new(descending: true);

    public int Compare(WorldResourceSet left, WorldResourceSet right)
    {
        if (descending)
        {
            return GetTotal(right).CompareTo(GetTotal(left));
        }
        else
        {
            return GetTotal(left).CompareTo(GetTotal(right));
        }
    }

    private static int GetTotal(WorldResourceSet set)
    {
        return set.Gold + set.DeathMana + set.RuneMana + set.LifeMana + set.InfernalMana + set.GroveMana;
    }
}
