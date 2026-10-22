namespace Gallean.Client;

internal interface IUnitGridSelectionProvider
{
    Task<bool> MoveUnitAsync(UnitGridSlotReference source, UnitGridSlotReference target);

    async Task InvokeEmptySlotAsync(UnitGridSlotReference target)
    {
    }

    async Task<bool> TryInvokeUnitSlotAsync(UnitGridSlotReference target)
    {
        return false;
    }

    void OnUnitSelectionChanged();
    void OnUnitMoved();
}
