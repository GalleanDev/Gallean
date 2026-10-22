using Gallean.Engine;

namespace Gallean.Client;

internal sealed class BattleAnimationResolver(IAssetMetadataProvider assets)
{
    public IReadOnlyList<AssetAnimationKey.BattleEffect.Attack> ResolveAttackLayers(
        UnitDefinitionId attackerId,
        AssetAnimationKey.BattleEffect.AttackType type,
        BattleSide targetSide)
    {
        if (ResolveLayer(attackerId, type, AssetAnimationKey.BattleLayer.Body, targetSide) is null)
        {
            return [];
        }

        List<AssetAnimationKey.BattleEffect.Attack> layers = [];

        foreach (AssetAnimationKey.BattleLayer layer in Enum.GetValues<AssetAnimationKey.BattleLayer>())
        {
            if (ResolveLayer(attackerId, type, layer, targetSide) is { } layerKey)
            {
                layers.Add(layerKey);
            }
        }

        return layers;
    }

    public IReadOnlyList<int> ResolveAttackVariants(IReadOnlyList<AssetAnimationKey.BattleEffect.Attack> layers)
    {
        List<int> variants = [];

        for (int variant = 0; variant < AssetAnimationKey.BattleEffect.Attack.VariantCount; variant++)
        {
            if (HasVariant(layers, variant))
            {
                variants.Add(variant);
            }
        }

        return variants;
    }

    public AssetAnimationKey.BattleEffect.Death ResolveDeath(UnitDeathAnimation animation, bool isLarge)
    {
        AssetAnimationKey.BattleEffect.Death key = new(animation, isLarge);

        if (assets.GetOptionalAnimationMetadata(key) is not null)
        {
            return key;
        }
        else
        {
            return new AssetAnimationKey.BattleEffect.Death(animation, IsLarge: false);
        }
    }

    public AssetAnimationKey.BattleEffect.Item? ResolveItem(ItemCategory category)
    {
        if (GetItemType(category) is { } type)
        {
            return new AssetAnimationKey.BattleEffect.Item(type);
        }
        else
        {
            return null;
        }
    }

    public AssetAnimationKey.BattleEffect.Status? ResolveStatus(BattleAbstractStatusEffect effect, bool isLarge)
    {
        if (GetStatusType(effect) is { } type)
        {
            return new AssetAnimationKey.BattleEffect.Status(type, isLarge);
        }
        else
        {
            return null;
        }
    }

    public AssetSoundKey.BattleEffect.Status? ResolveStatusSound(BattleAbstractStatusEffect effect)
    {
        if (GetStatusSoundType(effect) is { } type)
        {
            return new AssetSoundKey.BattleEffect.Status(type);
        }
        else
        {
            return null;
        }
    }

    public AssetAnimationKey.BattleEffect.Status ResolveTransform(BattleTransformType type, bool isLarge)
    {
        AssetAnimationKey.BattleEffect.StatusType statusType = type switch
        {
            BattleTransformType.Transform => AssetAnimationKey.BattleEffect.StatusType.Transform,
            BattleTransformType.TransformSelf => AssetAnimationKey.BattleEffect.StatusType.Transform,
            BattleTransformType.Doppelganger => AssetAnimationKey.BattleEffect.StatusType.Doppelganger,
            BattleTransformType.LevelDrain => AssetAnimationKey.BattleEffect.StatusType.ReduceLevel
        };

        return new AssetAnimationKey.BattleEffect.Status(statusType, isLarge);
    }

    public AssetAnimationKey.BattleEffect.Status ResolveTransformRestore(BattleTransformType type, bool isLarge)
    {
        AssetAnimationKey.BattleEffect.StatusType statusType = type switch
        {
            BattleTransformType.Transform => AssetAnimationKey.BattleEffect.StatusType.Untransform,
            BattleTransformType.TransformSelf => AssetAnimationKey.BattleEffect.StatusType.Untransform,
            BattleTransformType.Doppelganger => AssetAnimationKey.BattleEffect.StatusType.Undoppelganger,
            BattleTransformType.LevelDrain => AssetAnimationKey.BattleEffect.StatusType.RestoreLevel
        };

        return new AssetAnimationKey.BattleEffect.Status(statusType, isLarge);
    }

    private static AssetAnimationKey.BattleEffect.ItemType? GetItemType(ItemCategory category)
    {
        return category switch
        {
            ItemCategory.Armor => null,
            ItemCategory.Jewel => null,
            ItemCategory.Weapon => null,
            ItemCategory.Banner => null,
            ItemCategory.PotionTemporaryBoost => AssetAnimationKey.BattleEffect.ItemType.Potion,
            ItemCategory.PotionHeal => AssetAnimationKey.BattleEffect.ItemType.Potion,
            ItemCategory.PotionRevive => AssetAnimationKey.BattleEffect.ItemType.Potion,
            ItemCategory.PotionPermanentBoost => AssetAnimationKey.BattleEffect.ItemType.Potion,
            ItemCategory.Scroll => null,
            ItemCategory.Wand => null,
            ItemCategory.Valuable => null,
            ItemCategory.Orb => AssetAnimationKey.BattleEffect.ItemType.Orb,
            ItemCategory.Talisman => AssetAnimationKey.BattleEffect.ItemType.Talisman,
            ItemCategory.TravelItem => null,
            ItemCategory.Special => null
        };
    }

    private static AssetAnimationKey.BattleEffect.StatusType? GetStatusType(BattleAbstractStatusEffect effect)
    {
        return effect switch
        {
            BattlePoisonStatusEffect => AssetAnimationKey.BattleEffect.StatusType.Poison,
            BattleFrostbiteStatusEffect => AssetAnimationKey.BattleEffect.StatusType.Frostbite,
            BattleBlisterStatusEffect => AssetAnimationKey.BattleEffect.StatusType.Blister,
            _ => null
        };
    }

    private static AssetSoundKey.BattleEffect.StatusType? GetStatusSoundType(BattleAbstractStatusEffect effect)
    {
        return effect switch
        {
            BattlePoisonStatusEffect => AssetSoundKey.BattleEffect.StatusType.Poison,
            BattleFrostbiteStatusEffect => AssetSoundKey.BattleEffect.StatusType.Frostbite,
            BattleBlisterStatusEffect => AssetSoundKey.BattleEffect.StatusType.Blister,
            BattleShatterArmorStatusEffect => AssetSoundKey.BattleEffect.StatusType.ShatterArmor,
            _ => null
        };
    }

    private AssetAnimationKey.BattleEffect.Attack? ResolveLayer(
        UnitDefinitionId attackerId,
        AssetAnimationKey.BattleEffect.AttackType type,
        AssetAnimationKey.BattleLayer layer,
        BattleSide targetSide)
    {
        foreach (AssetAnimationKey.BattleEffect.AttackSide side in SideOrder(targetSide))
        {
            AssetAnimationKey.BattleEffect.Attack key = new(attackerId, type, layer, side, Variant: 0);

            if (assets.GetOptionalAnimationMetadata(key) is not null)
            {
                return key;
            }
        }

        return null;

        static AssetAnimationKey.BattleEffect.AttackSide[] SideOrder(BattleSide side) =>
        [
            side switch
            {
                BattleSide.Attacker => AssetAnimationKey.BattleEffect.AttackSide.Attack,
                BattleSide.Defender => AssetAnimationKey.BattleEffect.AttackSide.Defend
            },
            AssetAnimationKey.BattleEffect.AttackSide.Both
        ];
    }

    private bool HasVariant(IReadOnlyList<AssetAnimationKey.BattleEffect.Attack> keys, int variant)
    {
        foreach (AssetAnimationKey.BattleEffect.Attack key in keys)
        {
            if (assets.GetOptionalAnimationMetadata(key.WithVariant(variant)) is null)
            {
                return false;
            }
        }

        return true;
    }
}
