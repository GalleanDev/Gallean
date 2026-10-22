namespace Gallean.Client;

internal sealed class UnitGridSelectionViewModel(GameModel gameModel, IUnitGridSelectionProvider provider)
{
    private UnitGridSlotReference? selected;

    public UnitGridSlotReference? Selected => selected;

    public async Task SelectSlotAsync(UnitGridSlotViewModel slot)
    {
        if (gameModel.IsBusy)
        {
            return;
        }

        if (slot.HasUnit)
        {
            if (!await provider.TryInvokeUnitSlotAsync(slot.Reference))
            {
                selected = slot.Reference;
                provider.OnUnitSelectionChanged();
            }
        }
        else
        {
            await provider.InvokeEmptySlotAsync(slot.Reference);
        }
    }

    public async Task DropUnitAsync(UnitGridSlotReference source, UnitGridSlotReference target)
    {
        if (source == target)
        {
            return;
        }

        if (await provider.MoveUnitAsync(source, target))
        {
            selected = target;
        }

        provider.OnUnitMoved();
    }

    public void ClearSelection()
    {
        selected = null;
    }

    public void ReconcileWith(UnitGridViewModel grid, UnitGrid<WorldUnit> units)
    {
        if (selected is null)
        {
            return;
        }

        UnitGridSlotReference current = selected.Value;

        if (current.Grid != grid)
        {
            return;
        }

        if (units[current.Position] is null)
        {
            selected = null;
        }
    }

    public bool IsSelected(UnitGridViewModel grid, GridPosition position)
    {
        return selected == new UnitGridSlotReference(grid, position);
    }
}
