using CommunityToolkit.Mvvm.Input;

namespace Gallean.Client;

internal sealed class LeaderPurchaseItemViewModel : IDetailsSource
{
    public LeaderPurchaseItemViewModel(
        LeaderPurchaseViewModel owner,
        UnitDefinitionId leaderId,
        string name,
        string description,
        OfferStatusViewModel status,
        UnitDetailsViewModel details)
    {
        LeaderId = leaderId;
        PortraitKey = new AssetSpriteKey.UnitPortrait(leaderId);
        Name = name;
        Description = description;
        Status = status;
        Details = details;
        PurchaseCommand = new AsyncRelayCommand(() => owner.PurchaseAsync(this), () => Status.IsAvailable);
    }

    public UnitDefinitionId LeaderId { get; }
    public AssetSpriteKey.UnitPortrait PortraitKey { get; }
    public string Name { get; }
    public string Description { get; }
    public OfferStatusViewModel Status { get; }
    public UnitDetailsViewModel Details { get; }

    public IAsyncRelayCommand PurchaseCommand { get; }

    object? IDetailsSource.DetailsContent => Details;
}
