using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed record CapitalSceneViewModel(
    AssetSpriteKey BackdropKey,
    IReadOnlyList<CapitalSceneLayerViewModel> Layers)
{
    public static CapitalSceneViewModel? Create(
        IAssetMetadataProvider assets,
        WorldRace race,
        IReadOnlySet<WorldPlayerBuildingDefinitionId> acquiredBuildings,
        WorldPlayerBuildingDefinitionId? constructedBuildingId,
        IGameDefinitionProvider definitions,
        IGameTextProvider texts)
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
                            GetBackgroundKey(assets, race, background.Index),
                            AnimationKey: null,
                            Building: null,
                            IsConstructed: false));
                        break;
                    case CapitalSceneLayerDefinition.Animation animation:
                        layers.Add(new CapitalSceneLayerViewModel(
                            SpriteKey: null,
                            AnimationKey: new AssetAnimationKey.CapitalBackgroundOverlay(race, animation.Index),
                            Building: null,
                            IsConstructed: false));
                        break;
                    case CapitalSceneLayerDefinition.Building building when acquiredBuildings.Contains(building.BuildingId):
                        layers.Add(new CapitalSceneLayerViewModel(
                            SpriteKey: null,
                            AnimationKey: new AssetAnimationKey.CapitalBuilding(building.BuildingId),
                            CapitalBuildingViewModel.Create(building.BuildingId, definitions, texts),
                            IsConstructed: building.BuildingId == constructedBuildingId));
                        break;
                    case CapitalSceneLayerDefinition.Building:
                        break;
                }
            }

            return new CapitalSceneViewModel(GetBackgroundKey(assets, race, 0), layers);
        }
        else
        {
            return null;
        }
    }

    private static AssetSpriteKey GetBackgroundKey(IAssetMetadataProvider assets, WorldRace race, int variant)
    {
        AssetSpriteKey.CapitalBackgroundV2 key = new(race, variant);

        if (assets.GetOptionalSpriteMetadata(key) is not null)
        {
            return key;
        }

        return new AssetSpriteKey.CapitalBackground(race, variant);
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
