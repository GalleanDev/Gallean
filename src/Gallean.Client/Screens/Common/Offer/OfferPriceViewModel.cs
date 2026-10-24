using Gallean.Common;

namespace Gallean.Client;

internal sealed record OfferPriceViewModel
{
    public OfferPriceViewModel(WorldResourceSet value)
    {
        ResourceSetViewModel valueView = new(value, ResourceSetProjection.All);
        List<OfferPriceItemViewModel> items = [];

        foreach (ResourceSetItemViewModel item in valueView.Items)
        {
            items.Add(new OfferPriceItemViewModel(item, IsUnaffordable: false));
        }

        Items = items;
    }

    public OfferPriceViewModel(WorldResourceSet cost, WorldResourceSet resources)
    {
        ResourceSetViewModel costView = new(cost, ResourceSetProjection.All);
        List<OfferPriceItemViewModel> items = [];

        foreach (ResourceSetItemViewModel item in costView.Items)
        {
            items.Add(new OfferPriceItemViewModel(item, GetValue(resources, item.Type) < item.Value));
        }

        Items = items;
    }

    public IReadOnlyList<OfferPriceItemViewModel> Items { get; }

    public bool IsUnaffordable => Items.Any(item => item.IsUnaffordable);

    public bool Equals(OfferPriceViewModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Items.SequenceEqual(other.Items);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.AddSequence(Items);

        return hash.ToHashCode();
    }

    private static int GetValue(WorldResourceSet resources, WorldResourceType type)
    {
        return type switch
        {
            WorldResourceType.Gold => resources.Gold,
            WorldResourceType.LifeMana => resources.LifeMana,
            WorldResourceType.DeathMana => resources.DeathMana,
            WorldResourceType.RuneMana => resources.RuneMana,
            WorldResourceType.InfernalMana => resources.InfernalMana,
            WorldResourceType.GroveMana => resources.GroveMana
        };
    }
}
