namespace Gallean.Client;

internal sealed record BuildingGraphViewModel(
    IReadOnlyList<BuildingNodeViewModel> Nodes,
    IReadOnlyList<BuildingLink> Links,
    IReadOnlyList<BuildingRingViewModel> Rings,
    IReadOnlyList<BuildingBranchViewModel> Branches,
    double HubRadiusX,
    double HubRadiusY,
    double HubSize,
    double ProjectionY,
    double ExtentX,
    double ExtentY)
{
    public static BuildingGraphViewModel Empty { get; } = new([], [], [], [], 0.0, 0.0, 0.0, 1.0, 0.0, 0.0);

    public bool IsEmpty => Nodes.Count == 0;

    public IEnumerable<AssetPreloadItem> GetPreloadItems()
    {
        foreach (BuildingNodeViewModel node in Nodes)
        {
            yield return new AssetPreloadItem.Sprite(node.IconKey);

            foreach (BuildingUnitViewModel unit in node.Units)
            {
                yield return new AssetPreloadItem.Sprite(unit.FaceKey);
                yield return new AssetPreloadItem.Sprite(unit.PortraitKey);
                yield return new AssetPreloadItem.Sprite(unit.Details.PortraitKey);
            }
        }
    }
}
