using System.Collections.Immutable;
using Gallean.Engine;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal sealed partial class WorldRenderComposer
{
    private static class Fortifications
    {
        public readonly record struct Input(
            ImmutableSortedDictionary<WorldFortificationId, WorldFortification> Fortifications,
            PlayerRaces PlayerRaces,
            WorldVisibility Visibility);

        public static IReadOnlyList<MapObject> Build(Input input, IAssetMetadataProvider assets)
        {
            List<MapObject> objects = [];

            foreach (WorldFortification fortification in input.Fortifications.Values)
            {
                if (IsObjectVisible(input.Visibility, fortification.Footprint))
                {
                    int depth = GetFootprintDepth(fortification.Footprint, GetFortificationLayer(fortification));

                    objects.Add(new Fortification(
                        fortification.Entrance,
                        depth,
                        fortification.Footprint,
                        GetFortificationSprite(input.PlayerRaces, fortification)));

                    if (fortification is WorldCity city)
                    {
                        AssetAnimationKey.CityShadow shadowKey = new(city.DefinitionId.Level);

                        if (assets.GetOptionalAnimationMetadata(shadowKey) is not null)
                        {
                            objects.Add(new ObjectShadow(
                                city.Entrance,
                                GetFootprintDepth(city.Footprint, Layer.StacksShadow),
                                city.Footprint,
                                shadowKey));
                        }
                    }
                    else if (fortification is WorldRuin ruin)
                    {
                        objects.Add(new RuinBanner(
                            ruin.Entrance,
                            GetDepth(ruin.Entrance, Layer.Banners),
                            ruin.Footprint,
                            new AssetSpriteKey.RuinBanner(ruin.LooterId is not null)));
                    }
                }
            }

            return objects;
        }

        private static Layer GetFortificationLayer(WorldFortification fortification)
        {
            return fortification switch
            {
                WorldCity => Layer.Villages,
                WorldCapital => Layer.Capitals,
                WorldRuin => Layer.Ruins,
                _ => throw new ArgumentOutOfRangeException(nameof(fortification), fortification, null)
            };
        }

        private static AssetAnimationKey GetFortificationSprite(
            PlayerRaces playerRaces,
            WorldFortification fortification)
        {
            return fortification switch
            {
                WorldCity city => new AssetAnimationKey.City(city.DefinitionId.Level),
                WorldCapital capital => new AssetAnimationKey.Capital(playerRaces[capital.OwnerId], capital.CapturedById is not null),
                WorldRuin ruin => new AssetAnimationKey.Ruin(ruin.LooterId is not null, ruin.Variant.Value),
                _ => throw new ArgumentOutOfRangeException(nameof(fortification), fortification, null)
            };
        }
    }
}
