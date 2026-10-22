using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class WorldRenderComposer
{
    private static class StackSpriteBuilder
    {
        public static StackSprite Build(
            WorldMap map,
            WorldStack stack,
            WorldPosition position,
            AssetAnimationKey.StackUnitPose pose,
            WorldStackDirection direction,
            IGameDefinitionProvider definitions,
            IAssetMetadataProvider assets)
        {
            AssetAnimationKey spriteKey = GetSpriteKey(map, stack, position, pose, direction, definitions);
            AssetAnimationKey? shadowKey = GetShadowKey(spriteKey, assets);
            AssetAnimationKey? auraKey = GetAuraKey(spriteKey, assets);

            return new StackSprite(spriteKey, shadowKey, auraKey);
        }

        public static double GetOpacity(WorldStack stack)
        {
            return stack.Concealed ? 0.5 : 1.0;
        }

        private static AssetAnimationKey GetSpriteKey(
            WorldMap map,
            WorldStack stack,
            WorldPosition position,
            AssetAnimationKey.StackUnitPose pose,
            WorldStackDirection direction,
            IGameDefinitionProvider definitions)
        {
            (WorldUnit leader, GridPosition _, UnitDefinition leaderDefinition) = stack.GetLeader(definitions);

            if (leaderDefinition.IsWaterOnly)
            {
                return new AssetAnimationKey.StackUnit(leader.DefinitionId, pose, (int)direction);
            }
            else
            {
                bool isFlying = leaderDefinition.MoveAbility.HasFlag(UnitMoveAbility.Flying);
                WorldMapCell cell = map.GetCellAt(position);

                if (cell.Terrain == WorldMapTerrain.Water && !isFlying)
                {
                    return new AssetAnimationKey.StackBoat(leaderDefinition.Race, pose, (int)direction);
                }
                else
                {
                    AssetAnimationKey.StackUnitPose resolvedPose;

                    if (isFlying)
                    {
                        resolvedPose = AssetAnimationKey.StackUnitPose.Move;
                    }
                    else
                    {
                        resolvedPose = pose;
                    }

                    return new AssetAnimationKey.StackUnit(leader.DefinitionId, resolvedPose, (int)direction);
                }
            }
        }

        private static AssetAnimationKey? GetShadowKey(AssetAnimationKey spriteKey, IAssetMetadataProvider assets)
        {
            AssetAnimationKey? shadowKey = spriteKey switch
            {
                AssetAnimationKey.StackUnit unit => new AssetAnimationKey.StackUnitShadow(unit.DefinitionId, unit.Pose, unit.Direction),
                AssetAnimationKey.StackBoat boat => new AssetAnimationKey.StackBoatShadow(boat.Race, boat.Pose, boat.Direction),
                _ => null
            };

            if (shadowKey is not null && assets.GetOptionalAnimationMetadata(shadowKey) is not null)
            {
                return shadowKey;
            }
            else
            {
                return null;
            }
        }

        private static AssetAnimationKey? GetAuraKey(AssetAnimationKey spriteKey, IAssetMetadataProvider assets)
        {
            AssetAnimationKey? auraKey = spriteKey switch
            {
                AssetAnimationKey.StackUnit unit => new AssetAnimationKey.StackUnitAura(unit.DefinitionId, unit.Pose, unit.Direction),
                _ => null
            };

            if (auraKey is not null && assets.GetOptionalAnimationMetadata(auraKey) is not null)
            {
                return auraKey;
            }
            else
            {
                return null;
            }
        }
    }
}
