using CommunityToolkit.Mvvm.Input;

namespace Gallean.Client;

internal sealed class SpellbookBuildingViewModel
{
    public SpellbookBuildingViewModel(
        SpellbookTabViewModel owner,
        WorldPlayerBuildingDefinitionId buildingId,
        string name,
        OfferStatusViewModel status)
    {
        BuildingId = buildingId;
        Name = name;
        Status = status;
        ConstructCommand = new AsyncRelayCommand(() => owner.ConstructAsync(this), () => Status.IsAvailable);
    }

    public WorldPlayerBuildingDefinitionId BuildingId { get; }
    public string Name { get; }
    public OfferStatusViewModel Status { get; }

    public string MessageText => Tr("Your capital has no {0} yet. Construct it to research spells.", Name);
    public string ConstructText => Tr("Construct {0}", Name);

    public IAsyncRelayCommand ConstructCommand { get; }
}
