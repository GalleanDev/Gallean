using System.Collections.Immutable;
using Gallean.Engine;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal sealed partial class WorldRenderComposer
{
    private static class Stacks
    {
        public readonly record struct Input(
            ImmutableSortedDictionary<WorldStackId, WorldStack> Stacks,
            ImmutableSortedDictionary<WorldSubraceId, WorldSubrace> Subraces,
            ImmutableSortedDictionary<WorldFortificationId, WorldFortification> Fortifications,
            PlayerRaces PlayerRaces,
            WorldMap Map,
            WorldVisibility Visibility,
            WorldPlayerId ViewPlayerId,
            WorldStackId? AnimatedStackId);

        public static IReadOnlyList<MapObject> Build(
            Input input,
            IGameDefinitionProvider definitions,
            IAssetMetadataProvider assets)
        {
            List<MapObject> objects = [];

            foreach (WorldStack stack in input.Stacks.Values)
            {
                if (input.AnimatedStackId == stack.Id)
                {
                    continue;
                }

                if (!input.Visibility.IsStackVisible(stack, input.ViewPlayerId))
                {
                    continue;
                }

                WorldFortification? fortification = FindFortificationAt(input.Fortifications, stack.Position);

                if (fortification is WorldCity or WorldCapital { CapturedById: null })
                {
                    objects.Add(new CityShield(
                        stack.Position,
                        GetDepth(stack.Position, Layer.Shields),
                        fortification.Footprint,
                        GetShieldKey(input.PlayerRaces, fortification)));
                }
                else
                {
                    StackSprite sprite = StackSpriteBuilder.Build(
                        input.Map, stack, stack.Position, AssetAnimationKey.StackUnitPose.Stop, stack.Direction, definitions, assets);

                    objects.Add(new Stack(
                        stack.Position,
                        GetDepth(stack.Position, Layer.Stacks),
                        sprite.SpriteKey,
                        sprite.ShadowKey,
                        sprite.AuraKey,
                        new AssetSpriteKey.StackBanner(input.Subraces[stack.SubraceId].BannerVariant.Value),
                        StackSpriteBuilder.GetOpacity(stack)));
                }
            }

            return [.. objects.OrderBy(item => item.Depth)];
        }

        private static WorldFortification? FindFortificationAt(
            ImmutableSortedDictionary<WorldFortificationId, WorldFortification> fortifications,
            WorldPosition position)
        {
            foreach (WorldFortification fortification in fortifications.Values)
            {
                if (fortification.Entrance == position)
                {
                    return fortification;
                }
            }

            return null;
        }

        private static AssetSpriteKey.CityShield GetShieldKey(PlayerRaces playerRaces, WorldFortification fortification)
        {
            return fortification switch
            {
                WorldCity city => new AssetSpriteKey.CityShield(playerRaces[city.OwnerId], IsCapital: false),
                WorldCapital capital => new AssetSpriteKey.CityShield(playerRaces[capital.OwnerId], IsCapital: true),
                _ => throw new ArgumentOutOfRangeException(nameof(fortification), fortification, null)
            };
        }
    }
}
