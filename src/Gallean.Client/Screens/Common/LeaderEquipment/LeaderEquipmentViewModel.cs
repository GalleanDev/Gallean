using System.Collections.ObjectModel;
using Gallean.Common;

namespace Gallean.Client;

internal sealed class LeaderEquipmentViewModel(GameModel gameModel)
{
    private static readonly IReadOnlyList<LeaderEquipmentSlot> SlotLayout =
    [
        LeaderEquipmentSlot.Banner,
        LeaderEquipmentSlot.Tome,
        LeaderEquipmentSlot.Battle1,
        LeaderEquipmentSlot.Battle2,
        LeaderEquipmentSlot.Artifact1,
        LeaderEquipmentSlot.Artifact2,
        LeaderEquipmentSlot.Boots
    ];

    public ObservableCollection<LeaderEquipmentSlotViewModel> Slots { get; } = [];

    public void Reset()
    {
        Slots.Clear();
    }

    public void Update(WorldStack stack, IReadOnlySet<LeaderEquipmentSlot> highlight)
    {
        List<LeaderEquipmentSlotViewModel> newItems = [];

        foreach (LeaderEquipmentSlot slot in SlotLayout)
        {
            newItems.Add(BuildSlot(stack, slot, LeaderEquipmentSlotFormatter.FormatName(slot), highlight));
        }

        int index = 0;

        foreach (CollectionJoin.Item<LeaderEquipmentSlotViewModel> item in CollectionJoin.Make(Slots, newItems, slot => (int)slot.Slot))
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
            else if (!Equals(item.Left, item.Right))
            {
                Slots[index] = item.Right;
                index++;
            }
            else
            {
                index++;
            }
        }
    }

    private LeaderEquipmentSlotViewModel BuildSlot(
        WorldStack stack,
        LeaderEquipmentSlot slot,
        string label,
        IReadOnlySet<LeaderEquipmentSlot> highlight)
    {
        WorldItem? equippedItem = stack.Equipment[slot];

        ItemDefinitionId? itemId = null;
        AssetSpriteKey.ItemIcon? spriteKey = null;
        ItemDetailsViewModel? details = null;

        if (equippedItem is not null)
        {
            itemId = equippedItem.DefinitionId;
            spriteKey = ItemIconResolver.Resolve(gameModel.Definitions, equippedItem.DefinitionId);
            details = ItemDetailsViewModel.Create(equippedItem.DefinitionId, equippedItem.Charges, gameModel.Definitions, gameModel.Texts);
        }

        return new LeaderEquipmentSlotViewModel(slot, label, itemId, spriteKey, details, highlight.Contains(slot), this);
    }
}
