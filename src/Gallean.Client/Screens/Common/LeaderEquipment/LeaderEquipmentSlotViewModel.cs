namespace Gallean.Client;

internal sealed record LeaderEquipmentSlotViewModel : IDragSource, IDropTarget, IDetailsSource
{
    private readonly LeaderEquipmentViewModel equipment;

    public LeaderEquipmentSlotViewModel(
        LeaderEquipmentSlot slot,
        string label,
        ItemDefinitionId? itemId,
        AssetSpriteKey.ItemIcon? spriteKey,
        ItemDetailsViewModel? details,
        bool isHighlighted,
        LeaderEquipmentViewModel equipment)
    {
        this.equipment = equipment;

        Slot = slot;
        Label = label;
        ItemId = itemId;
        SpriteKey = spriteKey;
        Details = details;
        IsHighlighted = isHighlighted;
    }

    public LeaderEquipmentSlot Slot { get; }
    public string Label { get; }
    public ItemDefinitionId? ItemId { get; }

    public IconMoniker Icon => Slot switch
    {
        LeaderEquipmentSlot.Banner => IconMoniker.Banner,
        LeaderEquipmentSlot.Tome => IconMoniker.Tome,
        LeaderEquipmentSlot.Battle1 => IconMoniker.BattleItem,
        LeaderEquipmentSlot.Battle2 => IconMoniker.BattleItem,
        LeaderEquipmentSlot.Artifact1 => IconMoniker.Artifact,
        LeaderEquipmentSlot.Artifact2 => IconMoniker.Artifact,
        LeaderEquipmentSlot.Boots => IconMoniker.Boot
    };

    public LeaderEquipmentSlotReference Reference => new(equipment, Slot);

    public AssetSpriteKey.ItemIcon? SpriteKey { get; }
    public ItemDetailsViewModel? Details { get; }
    public bool IsHighlighted { get; }

    public DragPreview? DragPreview
    {
        get
        {
            if (SpriteKey is not null && ItemId is not null)
            {
                return new DragPreview(SpriteKey, DragPreviewType.Item);
            }
            else
            {
                return null;
            }
        }
    }

    IDragSourceReference IDragSource.DragReference => Reference;

    object? IDetailsSource.DetailsContent => Details;

    IDragTargetReference IDropTarget.DropReference => Reference;

    public bool Equals(LeaderEquipmentSlotViewModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return equipment == other.equipment &&
               Slot == other.Slot &&
               Label == other.Label &&
               ItemId == other.ItemId &&
               SpriteKey == other.SpriteKey &&
               Details == other.Details &&
               IsHighlighted == other.IsHighlighted;
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(equipment);
        hash.Add(Slot);
        hash.Add(Label);
        hash.Add(ItemId);
        hash.Add(SpriteKey);
        hash.Add(Details);
        hash.Add(IsHighlighted);

        return hash.ToHashCode();
    }
}
