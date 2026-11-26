using CommunityToolkit.Mvvm.ComponentModel;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class SpellItemViewModel : ObservableObject, IDetailsSource
{
    public SpellItemViewModel(
        WorldItemId itemId,
        ItemCategory category,
        InventoryItemType type,
        int count,
        OfferStatusViewModel status,
        ItemDetailsViewModel details)
    {
        ItemId = itemId;
        Category = category;
        Type = type;
        Count = count;
        Status = status;
        Details = details;
    }

    public WorldItemId ItemId { get; }
    public ItemCategory Category { get; }
    public InventoryItemType Type { get; }
    public int Count { get; }
    public OfferStatusViewModel Status { get; }
    public ItemDetailsViewModel Details { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    object? IDetailsSource.DetailsContent => Details;
}
