using System.Collections.ObjectModel;
using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed class UnitGridViewModel(GameModel gameModel, UnitGridSelectionViewModel selection, bool isFacingLeft)
{
    public bool IsFacingLeft { get; } = isFacingLeft;

    public ObservableCollection<UnitGridSlotViewModel> Slots { get; } = [];

    public void Update(UnitGrid<WorldUnit> units, bool showsPurchaseHints, IReadOnlySet<GridPosition>? highlight = null)
    {
        selection.ReconcileWith(this, units);

        List<UnitGridSlotViewModel> newItems = [];

        foreach (GridPosition position in GridPosition.All)
        {
            newItems.Add(BuildSlot(units, position, showsPurchaseHints, highlight));
        }

        int index = 0;

        foreach (CollectionJoin.Item<UnitGridSlotViewModel> item in CollectionJoin.Make(Slots, newItems, slot => slot.Position.ToIndex()))
        {
            if (item.Right is null)
            {
                Slots.RemoveAt(index);
            }
            else if (item.Left is null)
            {
                Slots.Insert(index, item.Right);
                index++;
            }
            else if (Equals(item.Left, item.Right))
            {
                index++;
            }
            else if (item.Left.ShowsSameUnit(item.Right))
            {
                item.Left.Update(item.Right);
                index++;
            }
            else
            {
                Slots[index] = item.Right;
                index++;
            }
        }
    }

    private UnitGridSlotViewModel BuildSlot(
        UnitGrid<WorldUnit> units,
        GridPosition position,
        bool showsPurchaseHints,
        IReadOnlySet<GridPosition>? highlight)
    {
        WorldUnit? unit = units[position];
        bool isLarge = units.IsLargeAt(position);
        bool isLargeSlot = position.IsBackLine && isLarge;

        int row = GridPosition.FlankCount - 1 - position.Flank;

        int column;
        if (isLarge)
        {
            column = 0;
        }
        else if (IsFacingLeft)
        {
            column = position.Line;
        }
        else
        {
            column = GridPosition.LineCount - 1 - position.Line;
        }

        if (unit is null || isLargeSlot)
        {
            return new UnitGridSlotViewModel(
                this,
                position,
                row,
                column,
                isLarge,
                unitId: null,
                faceKey: null,
                hitpoints: 0,
                maxHitpoints: 0,
                isAlive: true,
                isPendingUpgrade: false,
                isVisible: !isLargeSlot,
                isSelected: false,
                isHighlighted: !isLargeSlot && (highlight?.Contains(position) ?? false),
                showsPurchaseHint: showsPurchaseHints,
                selection,
                details: null);
        }

        UnitStats stats = gameModel.Stats.GetWorldUnitStats(unit);
        UnitExperience experience = gameModel.Stats.GetWorldUnitExperience(unit);
        AssetSpriteKey.UnitFace faceKey = new(unit.DefinitionId);
        GameSnapshot snapshot = gameModel.Snapshot;
        WorldPlayer owner = snapshot.WorldState.Players[snapshot.ViewPlayerId];

        UnitDetailsViewModel details = UnitDetailsViewModel.CreateFromWorld(
            unit,
            owner,
            gameModel.Definitions,
            gameModel.Texts,
            gameModel.Stats);

        return new UnitGridSlotViewModel(
            this,
            position,
            row,
            column,
            isLarge,
            unit.Id,
            faceKey,
            unit.Hitpoints,
            stats.MaxHitpoints,
            unit.IsAlive,
            experience.IsPendingUpgrade,
            isVisible: true,
            isSelected: selection.IsSelected(this, position),
            isHighlighted: highlight?.Contains(position) ?? false,
            showsPurchaseHint: false,
            selection,
            details);
    }
}
