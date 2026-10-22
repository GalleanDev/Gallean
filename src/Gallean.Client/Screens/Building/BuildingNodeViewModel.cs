using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Gallean.Client;

internal sealed partial class BuildingNodeViewModel : ObservableObject, BuildingStatus.IVisitor<string?>, IDetailsSource
{
    public BuildingNodeViewModel(
        WorldPlayerBuildingDefinitionId buildingId,
        string name,
        string description,
        string levelText,
        WorldResourceSet cost,
        WorldResourceSet resources,
        BuildingStatus status,
        IReadOnlyList<BuildingUnitViewModel> units,
        BuildingBranchViewModel? branch,
        bool isBranchRoot,
        double x,
        double y,
        TextSpans? lockedText,
        IReadOnlyList<WorldPlayerBuildingDefinitionId> lockedIds)
    {
        BuildingId = buildingId;
        IconKey = new AssetSpriteKey.BuildingIcon(buildingId);
        Name = name;
        Description = description;
        LevelText = levelText;
        Cost = cost;
        Status = status;
        StatusText = status.Accept(this);
        Offer = new OfferStatusViewModel(new OfferPriceViewModel(cost, resources), IsBuilt ? null : StatusText);
        Units = units;
        Branch = branch;
        IsBranchRoot = isBranchRoot;
        X = x;
        Y = y;
        LockedText = lockedText;
        LockedIds = lockedIds;
        PortraitKey = ResolvePortraitKey(units, IconKey);
    }

    private static string BuiltText => Tr("Built");
    private static string BranchLockedStatusText => Tr("Branch closed");
    private static string ScenarioLockedText => Tr("Not available in this quest");

    public WorldPlayerBuildingDefinitionId BuildingId { get; }
    public AssetSpriteKey.BuildingIcon IconKey { get; }
    public AssetSpriteKey PortraitKey { get; }
    public string Name { get; }
    public string Description { get; }
    public string LevelText { get; }
    public WorldResourceSet Cost { get; }
    public BuildingStatus Status { get; }
    public string? StatusText { get; }
    public OfferStatusViewModel Offer { get; }
    public IReadOnlyList<BuildingUnitViewModel> Units { get; }
    public BuildingBranchViewModel? Branch { get; }
    public bool IsBranchRoot { get; }
    public double X { get; }
    public double Y { get; }
    public TextSpans? LockedText { get; }
    public IReadOnlyList<WorldPlayerBuildingDefinitionId> LockedIds { get; }

    public Color BranchColor => Branch?.Color ?? Theme.BuildingGraph.CommonColor;
    public bool IsCommon => Branch is null;
    public bool IsConstructible => Status is BuildingStatus.Constructible;
    public bool IsBuilt => Status is BuildingStatus.Built;
    public bool IsUnavailable => Status is BuildingStatus.Unavailable;
    public bool IsBranchLocked => Status is BuildingStatus.BranchLocked;
    public bool IsMissingPrerequisite => Status is BuildingStatus.MissingPrerequisite;
    public bool IsScenarioLocked => Status is BuildingStatus.ScenarioLocked;
    public bool IsLocked => Status is BuildingStatus.BranchLocked or BuildingStatus.ScenarioLocked;
    public bool IsBlocked => Status is BuildingStatus.MissingPrerequisite or BuildingStatus.BranchLocked or BuildingStatus.ScenarioLocked;

    object? IDetailsSource.DetailsContent
    {
        get
        {
            if (Units.Count > 0)
            {
                return Units[0].Details;
            }
            else
            {
                return new BuildingDetailsViewModel(IconKey, Name, Description);
            }
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PaneContent))]
    public partial bool IsSelected { get; set; }

    public BuildingNodeViewModel? PaneContent => IsSelected ? this : null;

    [ObservableProperty]
    public partial bool IsSelectionLocked { get; set; }

    private static AssetSpriteKey ResolvePortraitKey(IReadOnlyList<BuildingUnitViewModel> units, AssetSpriteKey.BuildingIcon iconKey)
    {
        if (units.Count > 0)
        {
            return units[0].FaceKey;
        }
        else
        {
            return iconKey;
        }
    }

    string? BuildingStatus.IVisitor<string?>.Visit(BuildingStatus.Constructible status)
    {
        return null;
    }

    string? BuildingStatus.IVisitor<string?>.Visit(BuildingStatus.Built status)
    {
        return BuiltText;
    }

    string? BuildingStatus.IVisitor<string?>.Visit(BuildingStatus.BranchLocked status)
    {
        return BranchLockedStatusText;
    }

    string? BuildingStatus.IVisitor<string?>.Visit(BuildingStatus.MissingPrerequisite status)
    {
        return Tr("Requires {0}", status.PrerequisiteName);
    }

    string? BuildingStatus.IVisitor<string?>.Visit(BuildingStatus.ScenarioLocked status)
    {
        return ScenarioLockedText;
    }

    string? BuildingStatus.IVisitor<string?>.Visit(BuildingStatus.Unavailable status)
    {
        return status.Reason;
    }
}
