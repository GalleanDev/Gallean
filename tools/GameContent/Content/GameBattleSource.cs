using Gallean.Engine.Contracts;
using static GameContent.GameAssetDescriptor;
using static GameContent.GameResourceNames;

namespace GameContent;

internal sealed class GameBattleSource(
    GameImages battle,
    GameImages battleUnits,
    GameImages battleItems,
    GameGraphicsResolver graphicsResolver)
{
    public static IEnumerable<GameAssetDescriptor> ListDescriptors(IReadOnlyCollection<UnitDefinitionId> unitIds)
    {
        for (int variant = 0; variant < AssetSpriteKey.BattleCorpse.VariantCount; variant++)
        {
            AssetSpriteKey.BattleCorpse small = new(IsLarge: false, variant);
            AssetSpriteKey.BattleCorpse large = new(IsLarge: true, variant);

            yield return RequiredSprite(small, source => source.Battle.GetCorpse(small.IsLarge, small.Variant));
            yield return RequiredSprite(large, source => source.Battle.GetCorpse(large.IsLarge, large.Variant));
        }

        foreach (UnitDeathAnimation animation in Enum.GetValues<UnitDeathAnimation>())
        {
            yield return RequiredAnimation(
                new AssetAnimationKey.BattleEffect.Death(animation, IsLarge: false),
                source => source.Battle.GetDeathEffect(animation, isLarge: false));

            yield return OptionalAnimation(
                new AssetAnimationKey.BattleEffect.Death(animation, IsLarge: true),
                source => source.Battle.GetDeathEffect(animation, isLarge: true));
        }

        foreach (WorldRace race in Enum.GetValues<WorldRace>())
        {
            yield return RequiredAnimation(
                new AssetAnimationKey.BattleEffect.LevelUp(race, IsLarge: false),
                source => AnimationOperations.TryCreate(source.Battle.GetLevelUpEffectFrames(race, isLarge: false)));

            yield return RequiredAnimation(
                new AssetAnimationKey.BattleEffect.LevelUp(race, IsLarge: true),
                source => AnimationOperations.TryCreate(source.Battle.GetLevelUpEffectFrames(race, isLarge: true)));
        }

        foreach (AssetAnimationKey.BattleEffect.StatusType type in Enum.GetValues<AssetAnimationKey.BattleEffect.StatusType>())
        {
            AssetAnimationKey.BattleEffect.Status small = new(type, IsLarge: false);
            AssetAnimationKey.BattleEffect.Status large = new(type, IsLarge: true);

            yield return RequiredAnimation(
                small,
                source => AnimationOperations.TryCreate(source.Battle.GetStatusEffectFrames(small.Type, small.IsLarge)));

            yield return RequiredAnimation(
                large,
                source => AnimationOperations.TryCreate(source.Battle.GetStatusEffectFrames(large.Type, large.IsLarge)));
        }

        yield return RequiredAnimation(
            new AssetAnimationKey.BattleEffect.Heal(),
            source => AnimationOperations.TryCreate(source.Battle.GetHealEffectFrames()));

        yield return RequiredAnimation(
            new AssetAnimationKey.BattleEffect.Blister(),
            source => AnimationOperations.TryCreate(source.Battle.GetBlisterEffectFrames()));

        foreach (AssetAnimationKey.BattleEffect.ItemType type in Enum.GetValues<AssetAnimationKey.BattleEffect.ItemType>())
        {
            AssetAnimationKey.BattleEffect.Item item = new(type);

            yield return RequiredAnimation(
                item,
                source => AnimationOperations.TryCreate(source.Battle.GetItemEffectFrames(item.Type)));
        }

        foreach (UnitDefinitionId unitId in unitIds)
        {
            foreach (BattleSide side in Enum.GetValues<BattleSide>())
            {
                foreach (AssetAnimationKey.BattleUnitPose pose in Enum.GetValues<AssetAnimationKey.BattleUnitPose>())
                {
                    AssetAnimationKey.BattleUnit unit = new(unitId, pose, side);
                    AssetAnimationKey.BattleUnitShadow shadow = new(unitId, pose, side);
                    AssetAnimationKey.BattleUnitAura aura = new(unitId, pose, side);

                    yield return RequiredAnimation(
                        unit,
                        source => GetUnitAnimation(source, unit));

                    yield return RequiredAnimation(
                        shadow,
                        source => AnimationOperations.TryCreate(source.Battle.GetUnitShadowFrames(shadow.DefinitionId, shadow.Pose, shadow.Side)));

                    yield return OptionalAnimation(
                        aura,
                        source => AnimationOperations.TryCreate(source.Battle.GetUnitFrames(aura.DefinitionId, aura.Pose, aura.Side, AssetAnimationKey.BattleLayer.Aura)));
                }
            }

            foreach (AssetAnimationKey.BattleEffect.AttackType effectType in Enum.GetValues<AssetAnimationKey.BattleEffect.AttackType>())
            {
                foreach (AssetAnimationKey.BattleLayer effectLayer in Enum.GetValues<AssetAnimationKey.BattleLayer>())
                {
                    foreach (AssetAnimationKey.BattleEffect.AttackSide effectSide in Enum.GetValues<AssetAnimationKey.BattleEffect.AttackSide>())
                    {
                        for (int variant = 0; variant < AssetAnimationKey.BattleEffect.Attack.VariantCount; variant++)
                        {
                            AssetAnimationKey.BattleEffect.Attack key = new(unitId, effectType, effectLayer, effectSide, variant);

                            yield return OptionalAnimation(
                                key,
                                source => GetAttackEffectAnimation(source, key));
                        }
                    }
                }
            }
        }
    }

    private static AssetAnimation? GetUnitAnimation(GameAssetSource source, AssetAnimationKey.BattleUnit unit)
    {
        IReadOnlyList<AssetSprite>? frames = source.Battle.GetUnitFrames(unit.DefinitionId, unit.Pose, unit.Side, AssetAnimationKey.BattleLayer.Body);

        return unit.Pose switch
        {
            AssetAnimationKey.BattleUnitPose.Idle => AnimationOperations.TryCreate(frames),
            AssetAnimationKey.BattleUnitPose.Attack => CreateAttackPoseAnimation(source, unit.DefinitionId, frames),
            AssetAnimationKey.BattleUnitPose.Hit => AnimationOperations.TryCreate(frames)
        };
    }

    private static AssetAnimation? CreateAttackPoseAnimation(GameAssetSource source, UnitDefinitionId unitId, IReadOnlyList<AssetSprite>? frames)
    {
        if (frames is null)
        {
            return null;
        }

        GameUnitAttackTriggers triggers = source.Audio.GetUnitAttackTriggers(unitId);

        return AnimationOperations.TryCreate(frames, triggers.Pose, triggers.Second);
    }

    private static AssetAnimation? GetAttackEffectAnimation(GameAssetSource source, AssetAnimationKey.BattleEffect.Attack key)
    {
        IReadOnlyList<AssetSprite>? frames = source.Battle.GetAttackEffectFrames(key.AttackerUnitId, key.Type, key.Layer, key.Side, key.Variant);

        return key.Layer switch
        {
            AssetAnimationKey.BattleLayer.Shadow => AnimationOperations.TryCreate(frames),
            AssetAnimationKey.BattleLayer.Body => CreateAttackEffectBodyAnimation(source, key, frames),
            AssetAnimationKey.BattleLayer.Aura => AnimationOperations.TryCreate(frames)
        };
    }

    private static AssetAnimation? CreateAttackEffectBodyAnimation(
        GameAssetSource source,
        AssetAnimationKey.BattleEffect.Attack key,
        IReadOnlyList<AssetSprite>? frames)
    {
        if (frames is null)
        {
            return null;
        }

        GameUnitAttackTriggers triggers = source.Audio.GetUnitAttackTriggers(key.AttackerUnitId);

        int triggerFrame = key.Type switch
        {
            AssetAnimationKey.BattleEffect.AttackType.Hit => triggers.Hit,
            AssetAnimationKey.BattleEffect.AttackType.Touch => 1
        };

        return AnimationOperations.TryCreate(frames, triggerFrame, triggers.Second);
    }

    private IReadOnlyList<AssetSprite>? GetUnitFrames(UnitDefinitionId unitId, AssetAnimationKey.BattleUnitPose pose, BattleSide side, AssetAnimationKey.BattleLayer layer)
    {
        UnitDefinitionId graphicsId = graphicsResolver.ResolveUnitBattleGraphicsId(unitId);

        IReadOnlyList<AssetSprite>? frames = battleUnits.TryGetAnimationFrames(Battle.Unit(graphicsId.Value, pose, layer, side));

        if (frames is null && pose != AssetAnimationKey.BattleUnitPose.Idle)
        {
            frames = battleUnits.TryGetAnimationFrames(Battle.Unit(graphicsId.Value, AssetAnimationKey.BattleUnitPose.Idle, layer, side));
        }

        if (frames is { Count: > 0 })
        {
            return SpriteOperations.CropFramesToContent(frames, SpriteOperations.ContentOrigin.Canvas, margin: 1);
        }

        return null;
    }

    private IReadOnlyList<AssetSprite>? GetUnitShadowFrames(UnitDefinitionId unitId, AssetAnimationKey.BattleUnitPose pose, BattleSide side)
    {
        IReadOnlyList<AssetSprite>? shadows = GetUnitFrames(unitId, pose, side, AssetAnimationKey.BattleLayer.Shadow);
        IReadOnlyList<AssetSprite>? bodies = GetUnitFrames(unitId, pose, side, AssetAnimationKey.BattleLayer.Body);

        if (shadows is not null && bodies is not null)
        {
            return shadows;
        }
        else
        {
            return null;
        }
    }

    private IReadOnlyList<AssetSprite>? GetAttackEffectFrames(
        UnitDefinitionId attackerUnitId,
        AssetAnimationKey.BattleEffect.AttackType type,
        AssetAnimationKey.BattleLayer layer,
        AssetAnimationKey.BattleEffect.AttackSide side,
        int variant)
    {
        UnitDefinitionId graphicsId = graphicsResolver.ResolveUnitBattleGraphicsId(attackerUnitId);

        IReadOnlyList<AssetSprite>? frames = battleUnits.TryGetAnimationFrames(Battle.AttackEffect(graphicsId.Value, type, layer, side, variant));

        if (frames is { Count: > 0 })
        {
            return SpriteOperations.CropFramesToContent(frames, SpriteOperations.ContentOrigin.Canvas, margin: 1);
        }

        return null;
    }

    private AssetSprite? GetCorpse(bool isLarge, int variant)
    {
        AssetSprite? sprite = battle.TryGetSprite(Battle.Corpse(isLarge, variant));

        if (sprite is null)
        {
            return null;
        }

        return CropCorpse(sprite);
    }

    private AssetAnimation? GetDeathEffect(UnitDeathAnimation animation, bool isLarge)
    {
        string? name = Battle.DeathEffect(animation, isLarge);

        if (name is null)
        {
            return null;
        }

        IReadOnlyList<AssetSprite>? frames = battle.TryGetAnimationFrames(name);

        if (frames is { Count: > 0 })
        {
            IReadOnlyList<AssetSprite> cropped = SpriteOperations.CropFramesToContent(frames, SpriteOperations.ContentOrigin.Canvas, margin: 1);

            return AnimationOperations.Create(cropped, Battle.DeathEffectRevealFrame(name), secondTriggerFrame: null);
        }

        return null;
    }

    private IReadOnlyList<AssetSprite>? GetLevelUpEffectFrames(WorldRace race, bool isLarge)
    {
        IReadOnlyList<AssetSprite>? frames = battle.TryGetAnimationFrames(Battle.LevelUpEffect(race, isLarge));

        if (frames is { Count: > 0 })
        {
            return SpriteOperations.CropFramesToContent(frames, SpriteOperations.ContentOrigin.Canvas, margin: 1);
        }

        return null;
    }

    private IReadOnlyList<AssetSprite>? GetStatusEffectFrames(AssetAnimationKey.BattleEffect.StatusType type, bool isLarge)
    {
        IReadOnlyList<AssetSprite>? frames = battle.TryGetAnimationFrames(Battle.StatusEffect(type, isLarge));

        if (frames is { Count: > 0 })
        {
            return SpriteOperations.CropFramesToContent(frames, SpriteOperations.ContentOrigin.Canvas, margin: 1);
        }

        return null;
    }

    private IReadOnlyList<AssetSprite>? GetHealEffectFrames()
    {
        IReadOnlyList<AssetSprite>? frames = battleUnits.TryGetAnimationFrames(Battle.HealEffect());

        if (frames is { Count: > 0 })
        {
            return SpriteOperations.CropFramesToContent(frames, SpriteOperations.ContentOrigin.Canvas, margin: 1);
        }

        return null;
    }

    private IReadOnlyList<AssetSprite>? GetBlisterEffectFrames()
    {
        IReadOnlyList<AssetSprite>? frames = battleUnits.TryGetAnimationFrames(Battle.BlisterEffect());

        if (frames is { Count: > 0 })
        {
            return SpriteOperations.CropFramesToContent(frames, SpriteOperations.ContentOrigin.Canvas, margin: 1);
        }

        return null;
    }

    private IReadOnlyList<AssetSprite>? GetItemEffectFrames(AssetAnimationKey.BattleEffect.ItemType type)
    {
        IReadOnlyList<AssetSprite>? frames = battleItems.TryGetAnimationFrames(Battle.ItemEffect(type));

        if (frames is { Count: > 0 })
        {
            return SpriteOperations.CropFramesToContent(frames, SpriteOperations.ContentOrigin.Canvas, margin: 1);
        }

        return null;
    }

    private static AssetSprite? CropCorpse(AssetSprite sprite)
    {
        SpriteOperations.ContentBox box = SpriteOperations.GetContentBox([sprite]);

        if (box.IsEmpty)
        {
            return null;
        }

        int offsetX = box.MinX - (sprite.Metadata.PixelWidth / 2);
        int offsetY = box.MinY - (sprite.Metadata.PixelHeight / 2);

        AssetSprite content = SpriteOperations.Crop(sprite, box.MinX, box.MinY, box.Width, box.Height);

        return content.WithOffset(offsetX, offsetY);
    }
}
