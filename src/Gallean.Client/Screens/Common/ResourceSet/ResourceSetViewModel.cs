namespace Gallean.Client;

internal sealed record ResourceSetViewModel(WorldResourceSet Resources, WorldResourceSet PreviousResources, ResourceSetProjection Projection)
{
    public ResourceSetViewModel(WorldResourceSet resources, ResourceSetProjection projection)
        : this(resources, resources, projection)
    {
    }

    public IReadOnlyList<ResourceSetItemViewModel> Items => GetItems();

    public ResourceSetViewModel Update(WorldResourceSet resources)
    {
        if (resources == Resources)
        {
            return this;
        }
        else
        {
            return new ResourceSetViewModel(resources, Resources, Projection);
        }
    }

    private IReadOnlyList<ResourceSetItemViewModel> GetItems()
    {
        List<ResourceSetItemViewModel> items = [];

        foreach (WorldResourceType type in Projection.Types)
        {
            int value = GetValue(Resources, type);

            if (value > 0)
            {
                items.Add(new ResourceSetItemViewModel(type, value, GetValue(PreviousResources, type)));
            }
        }

        if (items.Count > 0)
        {
            return items;
        }
        else
        {
            WorldResourceType type = Projection.Types[0];

            return [new ResourceSetItemViewModel(type, 0, GetValue(PreviousResources, type))];
        }
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
