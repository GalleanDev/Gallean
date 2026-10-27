using System.Collections.Immutable;

namespace Gallean.Client;

internal sealed record ItemGroup(ItemDefinitionId DefinitionId, int? Charges, ImmutableArray<WorldItem> Items)
{
    public static ImmutableArray<ItemGroup> Build(ImmutableArray<WorldItem> items)
    {
        OrderedDictionary<(ItemDefinitionId DefinitionId, int? Charges), List<WorldItem>> groups = [];

        foreach (WorldItem item in items)
        {
            (ItemDefinitionId DefinitionId, int? Charges) key = (item.DefinitionId, item.Charges);

            if (!groups.TryGetValue(key, out List<WorldItem>? group))
            {
                group = [];
                groups.Add(key, group);
            }

            group.Add(item);
        }

        List<ItemGroup> result = [];

        foreach (((ItemDefinitionId DefinitionId, int? Charges) key, List<WorldItem> group) in groups)
        {
            result.Add(new ItemGroup(key.DefinitionId, key.Charges, [.. group]));
        }

        return [.. result];
    }
}
