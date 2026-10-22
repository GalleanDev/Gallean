namespace Gallean.Engine;

internal static class WorldMerchantSiteExtensions
{
    extension(WorldMerchantSite site)
    {
        public bool Contains(ItemDefinitionId itemId)
        {
            foreach (WorldMerchantItem merchantItem in site.Items)
            {
                if (merchantItem.ItemId == itemId)
                {
                    return merchantItem.Count > 0;
                }
            }

            return false;
        }

        public WorldMerchantSite Take(ItemDefinitionId itemId)
        {
            for (int index = 0; index < site.Items.Length; index++)
            {
                WorldMerchantItem merchantItem = site.Items[index];

                if (merchantItem.ItemId != itemId)
                {
                    continue;
                }

                if (merchantItem.Count > 1)
                {
                    return site with { Items = site.Items.SetItem(index, merchantItem with { Count = merchantItem.Count - 1 }) };
                }
                else
                {
                    return site with { Items = site.Items.RemoveAt(index) };
                }
            }

            throw new InvalidOperationException($"Merchant has no item '{itemId}'.");
        }
    }
}
