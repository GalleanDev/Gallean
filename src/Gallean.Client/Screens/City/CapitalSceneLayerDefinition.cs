namespace Gallean.Client;

internal closed record CapitalSceneLayerDefinition
{
    public sealed record Background(int Index) : CapitalSceneLayerDefinition;

    public sealed record Animation(int Index) : CapitalSceneLayerDefinition;

    public sealed record Building(WorldPlayerBuildingDefinitionId BuildingId) : CapitalSceneLayerDefinition;
}
