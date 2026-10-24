using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Gallean.Common;

namespace Gallean.Client;

internal sealed partial class InventoryViewModel(GameModel gameModel) : ObservableObject, IDropTarget
{
    private WorldInventory inventory = WorldInventory.Empty;

    public ObservableCollection<InventoryItemViewModel> Items { get; } = [];

    public IDragTargetReference DropReference => new InventoryReference(this);

    [ObservableProperty]
    public partial bool IsHighlighted { get; private set; }

    public WorldItem? GetItem(int index)
    {
        if (index < inventory.Items.Length)
        {
            return inventory.Items[index];
        }
        else
        {
            return null;
        }
    }

    public void Update(WorldInventory newInventory, WorldResourceSet resources, bool isHighlighted)
    {
        inventory = newInventory;
        IsHighlighted = isHighlighted;

        List<InventoryItemViewModel> newItems = [];

        for (int itemIndex = 0; itemIndex < newInventory.Items.Length; itemIndex++)
        {
            WorldItem item = newInventory.Items[itemIndex];

            ItemDetailsViewModel details = ItemDetailsViewModel.Create(
                item.DefinitionId,
                item.Charges,
                resources,
                gameModel.Definitions,
                gameModel.Texts);

            newItems.Add(new InventoryItemViewModel(
                itemIndex,
                item.Id,
                ItemIconResolver.Resolve(gameModel.Definitions, item.DefinitionId),
                ItemFormatter.FormatName(gameModel.Definitions, gameModel.Texts, item.DefinitionId, item.Charges),
                ItemFormatter.FormatDescription(gameModel.Definitions, gameModel.Texts, item.DefinitionId),
                details,
                this));
        }

        int slotIndex = 0;

        foreach (CollectionJoin.Item<InventoryItemViewModel> item in CollectionJoin.Make(Items, newItems, slot => slot.Index))
        {
            if (item.Right is null)
            {
                Items.RemoveAt(slotIndex);
            }
            else if (item.Left is null)
            {
                Items.Insert(slotIndex, item.Right);
                slotIndex++;
            }
            else if (!Equals(item.Left, item.Right))
            {
                Items[slotIndex] = item.Right;
                slotIndex++;
            }
            else
            {
                slotIndex++;
            }
        }
    }

    public void Reset()
    {
        inventory = WorldInventory.Empty;
        IsHighlighted = false;
        Items.Clear();
    }
}
