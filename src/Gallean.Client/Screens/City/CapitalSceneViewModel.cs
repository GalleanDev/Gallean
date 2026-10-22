using Gallean.Common;

namespace Gallean.Client;

internal sealed record CapitalSceneViewModel(
    AssetSpriteKey.CapitalBackground BackdropKey,
    IReadOnlyList<CapitalSceneLayerViewModel> Layers)
{
    public static CapitalSceneViewModel? Create(
        WorldRace race,
        IReadOnlySet<WorldPlayerBuildingDefinitionId> acquiredBuildings,
        WorldPlayerBuildingDefinitionId? constructedBuildingId)
    {
        CapitalSceneDefinition? definition = CapitalSceneDefinition.GetOptional(race);

        if (definition is not null)
        {
            List<CapitalSceneLayerViewModel> layers = [];

            foreach (CapitalSceneLayerDefinition layer in definition.Layers)
            {
                switch (layer)
                {
                    case CapitalSceneLayerDefinition.Background background:
                        layers.Add(new CapitalSceneLayerViewModel(
                            new AssetSpriteKey.CapitalBackground(race, background.Index),
                            AnimationKey: null,
                            IsConstructed: false));
                        break;
                    case CapitalSceneLayerDefinition.Animation animation:
                        layers.Add(new CapitalSceneLayerViewModel(
                            SpriteKey: null,
                            AnimationKey: new AssetAnimationKey.CapitalBackgroundOverlay(race, animation.Index),
                            IsConstructed: false));
                        break;
                    case CapitalSceneLayerDefinition.Building building when acquiredBuildings.Contains(building.BuildingId):
                        layers.Add(new CapitalSceneLayerViewModel(
                            SpriteKey: null,
                            AnimationKey: new AssetAnimationKey.CapitalBuilding(building.BuildingId),
                            IsConstructed: building.BuildingId == constructedBuildingId));
                        break;
                    case CapitalSceneLayerDefinition.Building:
                        break;
                }
            }

            return new CapitalSceneViewModel(new AssetSpriteKey.CapitalBackground(race, 0), layers);
        }
        else
        {
            return null;
        }
    }

    public bool Equals(CapitalSceneViewModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Layers.SequenceEqual(other.Layers);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.AddSequence(Layers);

        return hash.ToHashCode();
    }
}
