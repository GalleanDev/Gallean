namespace Gallean.Client;

internal sealed record InventoryItemViewModel : IDragSource, IDetailsSource
{
    public InventoryItemViewModel(
        int index,
        WorldItemId itemId,
        AssetSpriteKey.ItemIcon? spriteKey,
        string name,
        string description,
        ItemDetailsViewModel details,
        InventoryViewModel inventory)
    {
        Inventory = inventory;

        Index = index;
        ItemId = itemId;
        SpriteKey = spriteKey;
        Name = name;
        Description = description;
        Details = details;
    }

    public InventoryViewModel Inventory { get; }
    public int Index { get; }
    public WorldItemId ItemId { get; }

    public InventorySlotReference Reference => new(Inventory, Index);

    public AssetSpriteKey.ItemIcon? SpriteKey { get; }
    public string Name { get; }
    public string Description { get; }
    public ItemDetailsViewModel Details { get; }

    public DragPreview? DragPreview
    {
        get
        {
            if (SpriteKey is not null)
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

    public bool Equals(InventoryItemViewModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Inventory == other.Inventory &&
               Index == other.Index &&
               ItemId == other.ItemId &&
               SpriteKey == other.SpriteKey &&
               Name == other.Name &&
               Description == other.Description &&
               Details == other.Details;
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(Inventory);
        hash.Add(Index);
        hash.Add(ItemId);
        hash.Add(SpriteKey);
        hash.Add(Name);
        hash.Add(Description);
        hash.Add(Details);

        return hash.ToHashCode();
    }
}
