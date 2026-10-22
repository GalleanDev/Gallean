using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Gallean.Client;

internal sealed partial class MerchantPurchaseItemViewModel : ObservableObject, IDragSource, IDetailsSource
{
    public MerchantPurchaseItemViewModel(
        MerchantTabViewModel owner,
        ItemDefinitionId itemId,
        AssetSpriteKey.ItemIcon iconKey,
        string name,
        string description,
        TextSpans countText,
        OfferStatusViewModel status,
        ItemDetailsViewModel details)
    {
        ItemId = itemId;
        IconKey = iconKey;
        Name = name;
        Description = description;
        CountText = countText;
        Status = status;
        Details = details;
        SelectCommand = new RelayCommand(() => owner.SelectPurchaseItem(this));
    }

    public ItemDefinitionId ItemId { get; }
    public AssetSpriteKey.ItemIcon IconKey { get; }
    public string Name { get; }
    public string Description { get; }
    public TextSpans CountText { get; }
    public OfferStatusViewModel Status { get; }
    public ItemDetailsViewModel Details { get; }

    public IRelayCommand SelectCommand { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public DragPreview? DragPreview => new(IconKey, DragPreviewType.Item);

    public MerchantPurchaseItemReference Reference => new(ItemId);

    IDragSourceReference IDragSource.DragReference => Reference;

    object? IDetailsSource.DetailsContent => Details;
}
