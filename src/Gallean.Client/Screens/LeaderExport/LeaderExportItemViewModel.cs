using CommunityToolkit.Mvvm.Input;

namespace Gallean.Client;

internal sealed class LeaderExportItemViewModel : IDetailsSource
{
    public LeaderExportItemViewModel(
        LeaderExportScreenViewModel owner,
        IReadOnlyList<WorldItem> items,
        AssetSpriteKey.ItemIcon iconKey,
        string name,
        OfferPriceViewModel price,
        int selected,
        bool canAdd,
        ItemDetailsViewModel details)
    {
        Items = items;
        IconKey = iconKey;
        Name = name;
        CountText = TrSpans("Available: {0:*}", items.Count);
        Price = price;
        Selected = selected;
        Details = details;
        AddCommand = new RelayCommand(() => owner.AddItem(this), () => canAdd);
        RemoveCommand = new RelayCommand(() => owner.RemoveItem(this), () => IsSelected);
    }

    public IReadOnlyList<WorldItem> Items { get; }
    public AssetSpriteKey.ItemIcon IconKey { get; }
    public string Name { get; }
    public TextSpans CountText { get; }
    public OfferPriceViewModel Price { get; }
    public int Selected { get; }
    public ItemDetailsViewModel Details { get; }

    public bool IsSelected => Selected > 0;

    public IRelayCommand AddCommand { get; }
    public IRelayCommand RemoveCommand { get; }

    object? IDetailsSource.DetailsContent => Details;
}
