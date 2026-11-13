namespace Gallean.Client;

internal static class BattleUnitState
{
    public sealed record Auras(
        AssetAnimationKey.BattleUnitAura? Idle,
        AssetAnimationKey.BattleUnitAura? Attack,
        AssetAnimationKey.BattleUnitAura? Hit)
    {
        public static Auras None { get; } = new(null, null, null);

        public static Auras Resolve(IAssetMetadataProvider assets, UnitDefinitionId definitionId, BattleSide facing)
        {
            return new Auras(
                ResolvePose(assets, definitionId, AssetAnimationKey.BattleUnitPose.Idle, facing),
                ResolvePose(assets, definitionId, AssetAnimationKey.BattleUnitPose.Attack, facing),
                ResolvePose(assets, definitionId, AssetAnimationKey.BattleUnitPose.Hit, facing));
        }

        public AssetAnimationKey.BattleUnitAura? Get(AssetAnimationKey.BattleUnitPose pose)
        {
            return pose switch
            {
                AssetAnimationKey.BattleUnitPose.Idle => Idle,
                AssetAnimationKey.BattleUnitPose.Attack => Attack,
                AssetAnimationKey.BattleUnitPose.Hit => Hit
            };
        }

        private static AssetAnimationKey.BattleUnitAura? ResolvePose(
            IAssetMetadataProvider assets,
            UnitDefinitionId definitionId,
            AssetAnimationKey.BattleUnitPose pose,
            BattleSide facing)
        {
            AssetAnimationKey.BattleUnitAura key = new(definitionId, pose, facing);

            if (assets.GetOptionalAnimationMetadata(key) is not null)
            {
                return key;
            }
            else
            {
                return null;
            }
        }
    }

    public enum StatusEffectType
    {
        Defend,
        Paralyze,
        Petrify,
        Poison,
        Frostbite,
        Blister,
        Retreat,
        Retreated,
        Fortification,
        IncreaseDamage,
        ReduceDamage,
        ReduceInitiative,
        ShatterArmor,
        Cure,
        Transform,
        TransformSelf,
        Doppelganger,
        LevelDrain
    }

    public sealed record StatusEffect(StatusEffectType Type)
    {
        public static StatusEffect Create(BattleAbstractStatusEffect effect)
        {
            StatusEffectType type = effect switch
            {
                BattleDefendStatusEffect => StatusEffectType.Defend,
                BattleParalyzeStatusEffect => StatusEffectType.Paralyze,
                BattlePetrifyStatusEffect => StatusEffectType.Petrify,
                BattlePoisonStatusEffect => StatusEffectType.Poison,
                BattleFrostbiteStatusEffect => StatusEffectType.Frostbite,
                BattleBlisterStatusEffect => StatusEffectType.Blister,
                BattleRetreatStatusEffect => StatusEffectType.Retreat,
                BattleRetreatedStatusEffect => StatusEffectType.Retreated,
                BattleFortificationStatusEffect => StatusEffectType.Fortification,
                BattleIncreaseDamageStatusEffect => StatusEffectType.IncreaseDamage,
                BattleReduceDamageStatusEffect => StatusEffectType.ReduceDamage,
                BattleReduceInitiativeStatusEffect => StatusEffectType.ReduceInitiative,
                BattleShatterArmorStatusEffect => StatusEffectType.ShatterArmor,
                BattleCureStatusEffect => StatusEffectType.Cure,
                BattleTransformStatusEffect transform => GetTransformType(transform.Type)
            };

            return new StatusEffect(type);
        }

        public static IEnumerable<StatusEffect> CreateAll(IEnumerable<BattleAbstractStatusEffect> effects)
        {
            foreach (BattleAbstractStatusEffect effect in effects)
            {
                yield return Create(effect);
            }
        }

        private static StatusEffectType GetTransformType(BattleTransformType type)
        {
            return type switch
            {
                BattleTransformType.Transform => StatusEffectType.Transform,
                BattleTransformType.TransformSelf => StatusEffectType.TransformSelf,
                BattleTransformType.Doppelganger => StatusEffectType.Doppelganger,
                BattleTransformType.LevelDrain => StatusEffectType.LevelDrain
            };
        }
    }

    public sealed record Feedback(FeedbackType Type, string Text)
    {
        public static Feedback Experience(int experience)
        {
            return new Feedback(FeedbackType.Heal, Tr("+{0} XP", experience));
        }
    }

    public enum FeedbackType
    {
        Damage,
        Miss,
        Ward,
        Heal,
        Effect,
        Action
    }

    public enum Presence
    {
        Alive,
        Dead,
        Absent
    }
}
