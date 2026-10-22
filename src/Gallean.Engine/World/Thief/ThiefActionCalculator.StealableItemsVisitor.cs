using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

internal static partial class ThiefCalculator
{
    private sealed class StealableItemsVisitor :
        IThiefTargetVisitor<Empty, ImmutableArray<ItemDefinitionId>>,
        IStaticInstance<StealableItemsVisitor>
    {
        public ImmutableArray<ItemDefinitionId> Visit(ThiefStackTarget target, Empty context)
        {
            ImmutableArray<WorldItem> inventoryItems = target.Stack.Inventory.Items;

            return [.. inventoryItems.Select(item => item.DefinitionId)];
        }

        public ImmutableArray<ItemDefinitionId> Visit(ThiefFortificationTarget target, Empty context)
        {
            return [];
        }

        public ImmutableArray<ItemDefinitionId> Visit(ThiefMerchantSiteTarget target, Empty context)
        {
            ImmutableArray<WorldMerchantItem> siteItems = target.Site.Items;

            return [.. siteItems.Select(item => item.ItemId)];
        }

        public ImmutableArray<ItemDefinitionId> Visit(ThiefMageSiteTarget target, Empty context)
        {
            return [];
        }

        public ImmutableArray<ItemDefinitionId> Visit(ThiefRuinTarget target, Empty context)
        {
            return [];
        }
    }
}
