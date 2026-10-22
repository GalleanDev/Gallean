using CommunityToolkit.Mvvm.Input;

namespace Gallean.Client;

internal sealed class DiplomacyActionViewModel
{
    public DiplomacyActionViewModel(
        DiplomacyTabViewModel owner,
        DiplomacyActionType type,
        string name,
        string description,
        string? unavailableReason)
    {
        Type = type;
        Name = name;
        Description = description;
        Status = new OfferStatusViewModel(unavailableReason);
        SelectCommand = new RelayCommand(() => owner.SelectAction(this), () => Status.IsAvailable);
    }

    public DiplomacyActionType Type { get; }
    public string Name { get; }
    public string Description { get; }
    public OfferStatusViewModel Status { get; }

    public IRelayCommand SelectCommand { get; }
}
