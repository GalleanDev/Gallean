namespace Gallean.Client;

internal interface IInventoryTransferProvider
{
    Task<bool> TransferItemAsync(WorldItemId itemId, InventoryTransferDirection direction);

    void OnItemTransferred();
}
