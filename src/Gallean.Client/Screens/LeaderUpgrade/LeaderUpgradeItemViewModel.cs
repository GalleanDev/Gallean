using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Gallean.Client;

internal sealed partial class LeaderUpgradeItemViewModel : ObservableObject
{
    public LeaderUpgradeItemViewModel(
        LeaderUpgradeViewModel owner,
        UnitModifierDefinitionId modifierId,
        IconMoniker icon,
        string name,
        string description,
        bool isPreferred,
        string? unavailableReason)
    {
        ModifierId = modifierId;
        Icon = icon;
        Name = name;
        Description = description;
        IsPreferred = isPreferred;
        Status = new OfferStatusViewModel(unavailableReason);
        SelectCommand = new RelayCommand(() => owner.SelectUpgrade(this), () => Status.IsAvailable);
    }

    public UnitModifierDefinitionId ModifierId { get; }
    public IconMoniker Icon { get; }
    public string Name { get; }
    public string Description { get; }
    public bool IsPreferred { get; }
    public OfferStatusViewModel Status { get; }

    public IRelayCommand SelectCommand { get; }

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
