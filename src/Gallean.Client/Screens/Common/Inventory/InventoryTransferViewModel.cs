namespace Gallean.Client;

internal sealed class InventoryTransferViewModel(GameModel gameModel, IInventoryTransferProvider provider)
{
    public InventoryViewModel Inventory { get; } = new(gameModel);
    public InventoryViewModel OtherInventory { get; } = new(gameModel);

    public InventoryTransferDirection? GetTransferDirection(InventorySlotReference source)
    {
        if (source.Inventory == Inventory)
        {
            return InventoryTransferDirection.ToOther;
        }
        else if (source.Inventory == OtherInventory)
        {
            return InventoryTransferDirection.FromOther;
        }
        else
        {
            return null;
        }
    }

    public async Task DropItemAsync(InventorySlotReference source, InventoryReference target)
    {
        WorldItem? item = source.Inventory.GetItem(source.Index);
        InventoryTransferDirection? direction = GetTransferDirection(source);

        if (item is not null && direction is not null && target.Inventory != source.Inventory)
        {
            await provider.TransferItemAsync(item.Id, direction.Value);
            provider.OnItemTransferred();
        }
    }

    public void Update(
        WorldInventory inventory,
        WorldInventory otherInventory,
        WorldResourceSet resources,
        InventoryTransferDirection? highlight)
    {
        Inventory.Update(inventory, resources, highlight == InventoryTransferDirection.FromOther);
        OtherInventory.Update(otherInventory, resources, highlight == InventoryTransferDirection.ToOther);
    }

    public void Reset()
    {
        Inventory.Reset();
        OtherInventory.Reset();
    }
}
