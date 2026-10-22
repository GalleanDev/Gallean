using CommunityToolkit.Mvvm.Input;

namespace Gallean.Client;

internal sealed class DiplomacyOfferViewModel
{
    public DiplomacyOfferViewModel(
        DiplomacyTabViewModel owner,
        WorldDiplomacyTradeAction action,
        IconViewModel icon,
        string name,
        string description,
        int value,
        string? unavailableReason)
    {
        Action = action;
        Icon = icon;
        Name = name;
        Description = description;
        Value = value;
        Cost = new ResourceSetViewModel(WorldResourceSet.Empty with { Gold = value }, ResourceSetProjection.All);
        Status = new OfferStatusViewModel(unavailableReason);
        SelectCommand = new RelayCommand(() => owner.SelectOffer(this), () => Status.IsAvailable);
    }

    public WorldDiplomacyTradeAction Action { get; }
    public IconViewModel Icon { get; }
    public string Name { get; }
    public string Description { get; }
    public int Value { get; }
    public ResourceSetViewModel Cost { get; }
    public OfferStatusViewModel Status { get; }

    public IRelayCommand SelectCommand { get; }
}
