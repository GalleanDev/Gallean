using System.Diagnostics.CodeAnalysis;

namespace Gallean.Engine;

public static class WorldInventoryExtensions
{
    extension(WorldInventory inventory)
    {
        public bool TryGetItem(WorldItemId itemId, [NotNullWhen(true)] out WorldItem? item)
        {
            foreach (WorldItem inventoryItem in inventory.Items)
            {
                if (inventoryItem.Id == itemId)
                {
                    item = inventoryItem;

                    return true;
                }
            }

            item = null;

            return false;
        }

        public WorldInventory Remove(WorldItem item)
        {
            int index = inventory.Items.IndexOf(item);

            if (index < 0)
            {
                throw new InvalidOperationException($"Inventory has no item '{item.Id}'.");
            }

            return new WorldInventory(inventory.Items.RemoveAt(index));
        }

        public WorldInventory Add(WorldItem item)
        {
            return new WorldInventory(inventory.Items.Add(item));
        }

        public WorldInventory AddRange(IEnumerable<WorldItem> items)
        {
            return new WorldInventory(inventory.Items.AddRange(items));
        }
    }
}
