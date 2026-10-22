using System.Collections.Immutable;
using Avalonia.Media;
using Gallean.Common;
using Gallean.Engine;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client;

internal sealed partial class WorldRenderComposer
{
    private static class AnimationBuilder
    {
        public static StackMove? BuildStackMove(
            ImmutableSortedDictionary<WorldStackId, WorldStack> stacks,
            ImmutableSortedDictionary<WorldSubraceId, WorldSubrace> subraces,
            WorldMap map,
            IGameDefinitionProvider definitions,
            IAssetMetadataProvider assets,
            WorldStackId stackId,
            IReadOnlyList<WorldAnimationState.StackMoveStep> moveSteps,
            bool isObjective,
            Color? highlight)
        {
            if (moveSteps.Count < 2)
            {
                return null;
            }

            if (!stacks.TryGetValue(stackId, out WorldStack? stack))
            {
                return null;
            }

            List<StackMoveStep> steps = [];
            (WorldUnit leader, GridPosition _, UnitDefinition _) = stack.GetLeader(definitions);

            foreach (WorldAnimationState.StackMoveStep moveStep in moveSteps)
            {
                StackSprite sprite = StackSpriteBuilder.Build(
                    map, stack, moveStep.Position, AssetAnimationKey.StackUnitPose.Move, moveStep.Direction, definitions, assets);

                steps.Add(new StackMoveStep(
                    moveStep.Position,
                    GetDepth(moveStep.Position, Layer.Stacks),
                    sprite.SpriteKey,
                    sprite.ShadowKey,
                    sprite.AuraKey,
                    moveStep.Start));
            }

            StackMoveStep destinationStep = steps[^1];

            return new StackMove(
                stackId,
                GetMoveSoundKey(destinationStep, leader.DefinitionId),
                new AssetSpriteKey.StackBanner(subraces[stack.SubraceId].BannerVariant.Value),
                steps,
                StackSpriteBuilder.GetOpacity(stack),
                isObjective,
                highlight);
        }

        public static WorldRenderState.Effect BuildStackDestroy(
            IGameDefinitionProvider definitions,
            IAssetMetadataProvider assets,
            WorldPosition position,
            UnitDefinitionId leaderDefinitionId)
        {
            AssetAnimationKey effectKey = GetDestroyEffectKey(definitions, leaderDefinitionId);
            AssetAnimationMetadata metadata = assets.GetRequiredAnimationMetadata(effectKey);

            return new WorldRenderState.Effect(
                GetDestroySoundKey(definitions, leaderDefinitionId),
                position,
                GetDepth(position, Layer.StackDeath),
                effectKey,
                metadata.FrameCount);
        }

        public static WorldRenderState.Effect BuildStackRemove(
            IGameDefinitionProvider definitions,
            IAssetMetadataProvider assets,
            WorldPosition position,
            UnitDefinitionId leaderDefinitionId)
        {
            AssetAnimationKey effectKey = GetDestroyEffectKey(definitions, leaderDefinitionId);
            AssetAnimationMetadata metadata = assets.GetRequiredAnimationMetadata(effectKey);

            return new WorldRenderState.Effect(
                SoundKey: null,
                position,
                GetDepth(position, Layer.StackDeath),
                effectKey,
                metadata.FrameCount);
        }

        public static WorldRenderState.Effect BuildStackCreate(
            IAssetMetadataProvider assets,
            WorldPosition position)
        {
            AssetAnimationKey.StackCreateEffect effectKey = new();
            AssetAnimationMetadata metadata = assets.GetRequiredAnimationMetadata(effectKey);

            return new WorldRenderState.Effect(
                new AssetSoundKey.StackCreate(),
                position,
                GetDepth(position, Layer.StackCreate),
                effectKey,
                metadata.FrameCount);
        }

        public static WorldRenderState.Effect BuildStackOwnerChange(
            IAssetMetadataProvider assets,
            WorldPosition position)
        {
            AssetAnimationKey.StackOwnerChangeEffect effectKey = new();
            AssetAnimationMetadata metadata = assets.GetRequiredAnimationMetadata(effectKey);

            return new WorldRenderState.Effect(
                SoundKey: null,
                position,
                GetDepth(position, Layer.StackOwner),
                effectKey,
                metadata.FrameCount);
        }

        public static WorldRenderState.Effect BuildMountainRemove(IAssetMetadataProvider assets, WorldPosition center, int radius)
        {
            int size = (2 * radius) + 1;
            AssetAnimationKey.MountainRemoveEffect effectKey = new(size);
            AssetAnimationMetadata metadata = assets.GetRequiredAnimationMetadata(effectKey);

            return new WorldRenderState.Effect(
                new AssetSoundKey.Occupy(),
                center,
                GetDepth(center, Layer.MountainRemove),
                effectKey,
                metadata.FrameCount);
        }

        public static StackBattle BuildStackBattle(
            IAssetMetadataProvider assets,
            WorldPosition position)
        {
            AssetAnimationKey.StackBattleEffect effectKey = new();
            AssetAnimationMetadata metadata = assets.GetRequiredAnimationMetadata(effectKey);

            return new StackBattle(
                position,
                GetDepth(position, Layer.SymbolStackBattle),
                effectKey,
                metadata.FrameCount);
        }

        public static SpellCast BuildSpellCast(
            ImmutableSortedDictionary<WorldStackId, WorldStack> stacks,
            ImmutableSortedDictionary<WorldSubraceId, WorldSubrace> subraces,
            WorldMap map,
            IGameDefinitionProvider definitions,
            IAssetMetadataProvider assets,
            WorldSpellDefinitionId spellId,
            WorldPosition position,
            WorldStackId? summonedStackId)
        {
            List<SpellCastLayer> layers = [];
            int frameCount = 0;
            int revealFrame = 0;

            foreach (AssetAnimationKey.WorldSpellEffectLayer effectLayer in Enum.GetValues<AssetAnimationKey.WorldSpellEffectLayer>())
            {
                for (int sublayer = 0; sublayer < AssetAnimationKey.WorldSpellEffect.SublayerCount; sublayer++)
                {
                    AssetAnimationKey.WorldSpellEffect key = new(spellId, effectLayer, sublayer);
                    AssetAnimationMetadata? metadata = assets.GetOptionalAnimationMetadata(key);

                    if (metadata is not null)
                    {
                        layers.Add(new SpellCastLayer.Effect(
                            key,
                            GetDepth(position, GetSpellLayer(effectLayer, sublayer)),
                            metadata.Value.FrameCount));

                        frameCount = Math.Max(frameCount, metadata.Value.FrameCount);

                        if (metadata.Value.TriggerFrame is not null)
                        {
                            revealFrame = Math.Max(revealFrame, metadata.Value.TriggerFrame.Value);
                        }
                    }
                }
            }

            WorldStack? summonedStack = null;

            if (summonedStackId is not null && stacks.TryGetValue(summonedStackId.Value, out WorldStack? stack))
            {
                summonedStack = stack;
                layers.Add(BuildSpellCastSummon(subraces, map, definitions, assets, stack, revealFrame));
            }

            return new SpellCast(
                new AssetSoundKey.WorldSpellCast(spellId),
                position,
                [.. layers.OrderBy(layer => layer.Depth)],
                summonedStack?.Id,
                frameCount);
        }

        private static AssetSoundKey GetMoveSoundKey(StackMoveStep destinationStep, UnitDefinitionId leaderDefinitionId)
        {
            if (destinationStep.AssetKey is AssetAnimationKey.StackBoat)
            {
                return new AssetSoundKey.StackBoatMove();
            }
            else
            {
                WorldPosition destination = destinationStep.Position;
                int variant = StableHash.Combine(destination.X, destination.Y).Mod(AssetSoundKey.StackUnitMove.VariantCount);

                return new AssetSoundKey.StackUnitMove(leaderDefinitionId, variant);
            }
        }

        private static AssetAnimationKey GetDestroyEffectKey(IGameDefinitionProvider definitions, UnitDefinitionId leaderDefinitionId)
        {
            if (definitions.GetUnit(leaderDefinitionId).Category == UnitCategory.Summon)
            {
                return new AssetAnimationKey.StackSummonDestroyEffect();
            }
            else
            {
                return new AssetAnimationKey.StackDestroyEffect();
            }
        }

        private static AssetSoundKey GetDestroySoundKey(IGameDefinitionProvider definitions, UnitDefinitionId leaderDefinitionId)
        {
            if (definitions.GetUnit(leaderDefinitionId).Category == UnitCategory.Summon)
            {
                return new AssetSoundKey.StackSummonDestroy();
            }
            else
            {
                return new AssetSoundKey.StackDestroy();
            }
        }

        private static Layer GetSpellLayer(AssetAnimationKey.WorldSpellEffectLayer effectLayer, int sublayer)
        {
            if (sublayer == 0)
            {
                return effectLayer switch
                {
                    AssetAnimationKey.WorldSpellEffectLayer.Ground => Layer.SpellsGround1,
                    AssetAnimationKey.WorldSpellEffectLayer.Under => Layer.SpellsUnder1,
                    AssetAnimationKey.WorldSpellEffectLayer.Front => Layer.SpellsFront1,
                    AssetAnimationKey.WorldSpellEffectLayer.Over => Layer.SpellsOver1,
                    AssetAnimationKey.WorldSpellEffectLayer.Top => Layer.SpellsTop1
                };
            }
            else
            {
                return effectLayer switch
                {
                    AssetAnimationKey.WorldSpellEffectLayer.Ground => Layer.SpellsGround2,
                    AssetAnimationKey.WorldSpellEffectLayer.Under => Layer.SpellsUnder2,
                    AssetAnimationKey.WorldSpellEffectLayer.Front => Layer.SpellsFront2,
                    AssetAnimationKey.WorldSpellEffectLayer.Over => Layer.SpellsOver2,
                    AssetAnimationKey.WorldSpellEffectLayer.Top => Layer.SpellsTop2
                };
            }
        }

        private static SpellCastLayer.Summon BuildSpellCastSummon(
            ImmutableSortedDictionary<WorldSubraceId, WorldSubrace> subraces,
            WorldMap map,
            IGameDefinitionProvider definitions,
            IAssetMetadataProvider assets,
            WorldStack stack,
            int revealFrame)
        {
            StackSprite sprite = StackSpriteBuilder.Build(
                map, stack, stack.Position, AssetAnimationKey.StackUnitPose.Stop, stack.Direction, definitions, assets);

            Stack summoned = new(
                stack.Position,
                GetDepth(stack.Position, Layer.Stacks),
                sprite.SpriteKey,
                sprite.ShadowKey,
                sprite.AuraKey,
                new AssetSpriteKey.StackBanner(subraces[stack.SubraceId].BannerVariant.Value),
                StackSpriteBuilder.GetOpacity(stack));

            return new SpellCastLayer.Summon(summoned, revealFrame);
        }
    }
}
