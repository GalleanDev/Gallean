using CommunityToolkit.Mvvm.Input;

namespace Gallean.Client;

internal sealed class LeaderExportItemViewModel : IDetailsSource
{
    public LeaderExportItemViewModel(
        LeaderExportScreenViewModel owner,
        IReadOnlyList<WorldItem> items,
        AssetSpriteKey.ItemIcon iconKey,
        string name,
        int selected,
        bool canAdd,
        ItemDetailsViewModel details)
    {
        Items = items;
        IconKey = iconKey;
        Name = name;
        Selected = selected;
        Details = details;
        AddCommand = new RelayCommand(() => owner.AddItem(this), () => canAdd);
        RemoveCommand = new RelayCommand(() => owner.RemoveItem(this), () => IsSelected);
    }

    public IReadOnlyList<WorldItem> Items { get; }
    public AssetSpriteKey.ItemIcon IconKey { get; }
    public string Name { get; }
    public int Selected { get; }
    public ItemDetailsViewModel Details { get; }

    public string AvailableText => $"×{Items.Count}";
    public string SelectedText => Tr("{0} selected", Selected);
    public bool IsSelected => Selected > 0;

    public IRelayCommand AddCommand { get; }
    public IRelayCommand RemoveCommand { get; }

    object? IDetailsSource.DetailsContent => Details;
}
