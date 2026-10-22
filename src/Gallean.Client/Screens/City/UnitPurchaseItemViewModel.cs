using CommunityToolkit.Mvvm.Input;

namespace Gallean.Client;

internal sealed class UnitPurchaseItemViewModel : IDetailsSource
{
    public UnitPurchaseItemViewModel(
        UnitPurchaseViewModel owner,
        UnitDefinitionId unitDefinitionId,
        string name,
        string description,
        OfferStatusViewModel status,
        UnitDetailsViewModel details)
    {
        UnitDefinitionId = unitDefinitionId;
        PortraitKey = new AssetSpriteKey.UnitPortrait(unitDefinitionId);
        Name = name;
        Description = description;
        Status = status;
        Details = details;
        PurchaseCommand = new AsyncRelayCommand(() => owner.PurchaseAsync(this), () => Status.IsAvailable);
    }

    public UnitDefinitionId UnitDefinitionId { get; }
    public AssetSpriteKey.UnitPortrait PortraitKey { get; }
    public string Name { get; }
    public string Description { get; }
    public OfferStatusViewModel Status { get; }
    public UnitDetailsViewModel Details { get; }

    public IAsyncRelayCommand PurchaseCommand { get; }

    object? IDetailsSource.DetailsContent => Details;
}
