using System.Collections.Immutable;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Gallean.Common;

namespace Gallean.Client;

internal sealed partial class InventoryViewModel(GameModel gameModel) : ObservableObject, IDropTarget
{
    private ImmutableArray<ItemGroup> slots = [];

    public ObservableCollection<InventoryItemViewModel> Items { get; } = [];

    public IDragTargetReference DropReference => new InventoryReference(this);

    [ObservableProperty]
    public partial bool IsHighlighted { get; private set; }

    public WorldItem? GetItem(int index)
    {
        if (index < slots.Length)
        {
            return slots[index].Items[^1];
        }
        else
        {
            return null;
        }
    }

    public ImmutableArray<WorldItem> GetItems(int index, int count)
    {
        if (index < slots.Length)
        {
            ImmutableArray<WorldItem> items = slots[index].Items;

            return items[^Math.Min(count, items.Length)..];
        }
        else
        {
            return [];
        }
    }

    public void Update(WorldInventory newInventory, WorldResourceSet resources, bool isHighlighted)
    {
        IsHighlighted = isHighlighted;

        slots = ItemGroup.Build(newInventory.Items);

        List<InventoryItemViewModel> newItems = [];

        for (int slotIndex = 0; slotIndex < slots.Length; slotIndex++)
        {
            ItemGroup slot = slots[slotIndex];
            WorldItem item = slot.Items[^1];

            ItemDetailsViewModel details = ItemDetailsViewModel.Create(
                item.DefinitionId,
                item.Charges,
                resources,
                gameModel.Definitions,
                gameModel.Texts);

            newItems.Add(new InventoryItemViewModel(
                slotIndex,
                item.Id,
                slot.Items.Length,
                ItemIconResolver.Resolve(gameModel.Definitions, item.DefinitionId),
                ItemFormatter.FormatName(gameModel.Definitions, gameModel.Texts, item.DefinitionId, item.Charges),
                ItemFormatter.FormatDescription(gameModel.Definitions, gameModel.Texts, item.DefinitionId),
                details,
                this));
        }

        int itemIndex = 0;

        foreach (CollectionJoin.Item<InventoryItemViewModel> item in CollectionJoin.Make(Items, newItems, slot => slot.Index))
        {
            if (item.Right is null)
            {
                Items.RemoveAt(itemIndex);
            }
            else if (item.Left is null)
            {
                Items.Insert(itemIndex, item.Right);
                itemIndex++;
            }
            else if (!Equals(item.Left, item.Right))
            {
                Items[itemIndex] = item.Right;
                itemIndex++;
            }
            else
            {
                itemIndex++;
            }
        }
    }

    public void Reset()
    {
        slots = [];
        IsHighlighted = false;
        Items.Clear();
    }
}
