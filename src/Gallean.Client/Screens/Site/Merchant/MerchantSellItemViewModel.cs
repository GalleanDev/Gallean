using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Gallean.Client;

internal sealed partial class MerchantSellItemViewModel : ObservableObject, IDragSource, IDetailsSource
{
    public MerchantSellItemViewModel(
        MerchantTabViewModel owner,
        ItemDefinitionId itemId,
        int? charges,
        int count,
        InventoryItemType type,
        AssetSpriteKey.ItemIcon iconKey,
        string name,
        string description,
        WorldResourceSet value,
        string? unavailableReason,
        ItemDetailsViewModel details)
    {
        ItemId = itemId;
        Charges = charges;
        Count = count;
        Type = type;
        IconKey = iconKey;
        Name = name;
        Description = description;
        Value = new ResourceSetViewModel(value, ResourceSetProjection.All);
        UnavailableReason = unavailableReason;
        Details = details;
        SelectCommand = new RelayCommand(() => owner.SelectSellItem(this));
    }

    public ItemDefinitionId ItemId { get; }
    public int? Charges { get; }
    public int Count { get; }
    public InventoryItemType Type { get; }
    public AssetSpriteKey.ItemIcon IconKey { get; }
    public string Name { get; }
    public string Description { get; }
    public ResourceSetViewModel Value { get; }
    public string? UnavailableReason { get; }
    public ItemDetailsViewModel Details { get; }

    public IRelayCommand SelectCommand { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public DragPreview? DragPreview => new(IconKey, DragPreviewType.Item);

    public MerchantSellItemReference Reference => new(ItemId, Charges);

    IDragSourceReference IDragSource.DragReference => Reference;

    object? IDetailsSource.DetailsContent => Details;
}
