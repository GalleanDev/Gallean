using Avalonia.Media;

namespace Gallean.Client;

internal sealed record BuildingBranchViewModel(BuildingBranchType Type, double ForkX, double ForkY)
{
    public string Title => Type switch
    {
        BuildingBranchType.Fighter => Tr("Fighters"),
        BuildingBranchType.Ranger => Tr("Archers"),
        BuildingBranchType.Mage => Tr("Mages"),
        BuildingBranchType.Special => Tr("Special")
    };

    public IconMoniker Icon => Type switch
    {
        BuildingBranchType.Fighter => IconMoniker.Damage,
        BuildingBranchType.Ranger => IconMoniker.Accuracy,
        BuildingBranchType.Mage => IconMoniker.Spellbook,
        BuildingBranchType.Special => IconMoniker.Experience
    };

    public Color Color => Type switch
    {
        BuildingBranchType.Fighter => Theme.BuildingGraph.FighterColor,
        BuildingBranchType.Ranger => Theme.BuildingGraph.RangerColor,
        BuildingBranchType.Mage => Theme.BuildingGraph.MageColor,
        BuildingBranchType.Special => Theme.BuildingGraph.SpecialColor
    };
}
