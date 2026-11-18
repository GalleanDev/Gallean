namespace Gallean.Client;

internal interface IInventoryProvider
{
    async Task InvokeItemAsync(InventorySlotReference source)
    {
    }
}
