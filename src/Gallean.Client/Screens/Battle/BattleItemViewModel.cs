using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Gallean.Client;

internal sealed partial class BattleItemViewModel(
    BattleScreenViewModel owner,
    LeaderEquipmentSlot slot,
    string hotkey,
    string name,
    AssetSpriteKey.ItemIcon spriteKey,
    bool isActive,
    bool canUse) : ObservableObject
{
    public LeaderEquipmentSlot Slot { get; } = slot;

    public string Hotkey { get; } = hotkey;

    [ObservableProperty]
    public partial string Name { get; private set; } = name;

    [ObservableProperty]
    public partial AssetSpriteKey.ItemIcon SpriteKey { get; private set; } = spriteKey;

    [ObservableProperty]
    public partial bool IsActive { get; private set; } = isActive;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SelectCommand))]
    public partial bool CanUse { get; private set; } = canUse;

    public void Update(BattleItemViewModel other)
    {
        Name = other.Name;
        SpriteKey = other.SpriteKey;
        IsActive = other.IsActive;
        CanUse = other.CanUse;
    }

    [RelayCommand(CanExecute = nameof(CanUse))]
    private void Select()
    {
        owner.SelectItem(Slot);
    }
}
