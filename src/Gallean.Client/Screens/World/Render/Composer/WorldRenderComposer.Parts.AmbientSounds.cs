using System.Collections.Frozen;
using System.Collections.Immutable;
using Gallean.Common;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal sealed partial class WorldRenderComposer
{
    private static class AmbientSounds
    {
        public readonly record struct Input(
            ImmutableSortedDictionary<WorldFortificationId, WorldFortification> Fortifications,
            ImmutableSortedDictionary<WorldLandmarkId, WorldLandmark> Landmarks,
            ImmutableSortedDictionary<WorldSiteId, WorldSite> Sites,
            ImmutableSortedDictionary<WorldResourceNodeId, WorldResourceNode> ResourceNodes,
            PlayerRaces PlayerRaces,
            WorldVisibility Visibility);

        private static readonly FrozenDictionary<WorldLandmarkDefinitionId, AssetSoundKey.AmbientSoundType> LandmarkSounds =
            new Dictionary<WorldLandmarkDefinitionId, AssetSoundKey.AmbientSoundType>
            {
                [new WorldLandmarkDefinitionId("G000MG0004")] = AssetSoundKey.AmbientSoundType.Torch,
                [new WorldLandmarkDefinitionId("G000MG0005")] = AssetSoundKey.AmbientSoundType.Torch,
                [new WorldLandmarkDefinitionId("G000MG0006")] = AssetSoundKey.AmbientSoundType.Waterfall,
                [new WorldLandmarkDefinitionId("G000MG0007")] = AssetSoundKey.AmbientSoundType.Waterfall,
                [new WorldLandmarkDefinitionId("G000MG0023")] = AssetSoundKey.AmbientSoundType.Lava,
                [new WorldLandmarkDefinitionId("G000MG0024")] = AssetSoundKey.AmbientSoundType.Lava,
                [new WorldLandmarkDefinitionId("G000MG0025")] = AssetSoundKey.AmbientSoundType.Cemetery,
                [new WorldLandmarkDefinitionId("G000MG0026")] = AssetSoundKey.AmbientSoundType.Cemetery,
                [new WorldLandmarkDefinitionId("G000MG0027")] = AssetSoundKey.AmbientSoundType.Cemetery,
                [new WorldLandmarkDefinitionId("G000MG0055")] = AssetSoundKey.AmbientSoundType.CityInFlames,
                [new WorldLandmarkDefinitionId("G000MG0073")] = AssetSoundKey.AmbientSoundType.Whirlpool,
                [new WorldLandmarkDefinitionId("G000MG0077")] = AssetSoundKey.AmbientSoundType.GateClosed,
                [new WorldLandmarkDefinitionId("G000MG0078")] = AssetSoundKey.AmbientSoundType.GateOpen,
                [new WorldLandmarkDefinitionId("G000MG0079")] = AssetSoundKey.AmbientSoundType.Waterfall,
                [new WorldLandmarkDefinitionId("G000MG0080")] = AssetSoundKey.AmbientSoundType.Waterfall,
                [new WorldLandmarkDefinitionId("G000MG0081")] = AssetSoundKey.AmbientSoundType.Waterfall,
                [new WorldLandmarkDefinitionId("G000MG0082")] = AssetSoundKey.AmbientSoundType.Waterfall
            }.ToFrozenDictionary();

        public static IReadOnlyList<AmbientSound> Build(Input input)
        {
            List<AmbientSound> sounds = [];

            foreach (WorldFortification fortification in input.Fortifications.Values)
            {
                if (IsObjectVisible(input.Visibility, fortification.Footprint))
                {
                    sounds.Add(new AmbientSound(
                        GetFortificationSound(input.PlayerRaces, fortification),
                        fortification.Footprint));
                }
            }

            foreach (WorldLandmark landmark in input.Landmarks.Values)
            {
                if (IsObjectVisible(input.Visibility, landmark.Footprint))
                {
                    if (LandmarkSounds.TryGetValue(landmark.DefinitionId, out AssetSoundKey.AmbientSoundType type))
                    {
                        sounds.Add(new AmbientSound(new AssetSoundKey.Ambient(type), landmark.Footprint));
                    }
                }
            }

            foreach (WorldSite site in input.Sites.Values)
            {
                if (IsObjectVisible(input.Visibility, site.Footprint))
                {
                    AssetSoundKey? siteSound = GetSiteSound(site);

                    if (siteSound is not null)
                    {
                        sounds.Add(new AmbientSound(siteSound, site.Footprint));
                    }
                }
            }

            foreach (WorldResourceNode node in input.ResourceNodes.Values)
            {
                if (IsObjectVisible(input.Visibility, node.Position))
                {
                    if (node.ResourceType == WorldResourceType.Gold)
                    {
                        sounds.Add(new AmbientSound(
                            new AssetSoundKey.Ambient(AssetSoundKey.AmbientSoundType.GoldMine),
                            new WorldFootprint(node.Position, 1, 1)));
                    }
                }
            }

            return sounds;
        }

        private static AssetSoundKey GetFortificationSound(PlayerRaces playerRaces, WorldFortification fortification)
        {
            return fortification switch
            {
                WorldCity => new AssetSoundKey.Ambient(AssetSoundKey.AmbientSoundType.City),
                WorldCapital capital => new AssetSoundKey.AmbientCapital(playerRaces[capital.OwnerId]),
                WorldRuin ruin => new AssetSoundKey.AmbientRuin(GetRuinVariant(ruin)),
                _ => throw new ArgumentOutOfRangeException(nameof(fortification), fortification, null)
            };
        }

        private static int GetRuinVariant(WorldRuin ruin)
        {
            WorldPosition topLeft = ruin.Footprint.TopLeft;

            return StableHash.Combine(topLeft.X, topLeft.Y).Mod(AssetSoundKey.AmbientRuin.VariantCount);
        }

        private static AssetSoundKey? GetSiteSound(WorldSite site)
        {
            return site switch
            {
                WorldMageSite => new AssetSoundKey.Ambient(AssetSoundKey.AmbientSoundType.MagicTower),
                WorldMerchantSite when site.Variant.Value == 0 => new AssetSoundKey.Ambient(AssetSoundKey.AmbientSoundType.Windmill),
                WorldMerchantSite => null,
                WorldMercenarySite => null,
                WorldTrainerSite => null,
                _ => throw new ArgumentOutOfRangeException(nameof(site), site, null)
            };
        }
    }
}
