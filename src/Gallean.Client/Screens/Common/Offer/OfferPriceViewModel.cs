namespace Gallean.Client;

internal sealed class OfferPriceViewModel
{
    public OfferPriceViewModel(WorldResourceSet value)
    {
        ResourceSetViewModel valueView = new(value, ResourceSetProjection.All);
        List<OfferPriceItemViewModel> items = [];

        foreach (ResourceSetItemViewModel item in valueView.Items)
        {
            items.Add(new OfferPriceItemViewModel(item, IsShort: false));
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

    public bool IsShort => Items.Any(item => item.IsShort);

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
