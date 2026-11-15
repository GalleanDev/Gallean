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
                MapObject item = fortification switch
                {
                    WorldCity city => CreateCityShield(stack, city, input.PlayerRaces[city.OwnerId], false),
                    WorldCapital { CapturedById: null } capital => CreateCityShield(stack, capital, input.PlayerRaces[capital.OwnerId], true),
                    WorldCapital or WorldRuin or null => CreateStack(input, stack, definitions, assets)
                };

                objects.Add(item);
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

        private static CityShield CreateCityShield(WorldStack stack, WorldFortification fortification, WorldRace race, bool isCapital)
        {
            return new CityShield(
                stack.Position,
                GetDepth(stack.Position, Layer.Shields),
                fortification.Footprint,
                new AssetSpriteKey.CityShield(race, isCapital));
        }

        private static Stack CreateStack(
            Input input,
            WorldStack stack,
            IGameDefinitionProvider definitions,
            IAssetMetadataProvider assets)
        {
            StackSprite sprite = StackSpriteBuilder.Build(
                input.Map, stack, stack.Position, AssetAnimationKey.StackUnitPose.Stop, stack.Direction, definitions, assets);

            return new Stack(
                stack.Position,
                GetDepth(stack.Position, Layer.Stacks),
                sprite.SpriteKey,
                sprite.ShadowKey,
                sprite.AuraKey,
                new AssetSpriteKey.StackBanner(input.Subraces[stack.SubraceId].BannerVariant.Value),
                StackSpriteBuilder.GetOpacity(stack));
        }
    }
}
