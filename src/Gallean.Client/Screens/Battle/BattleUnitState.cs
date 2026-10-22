using Gallean.Common;

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
        private sealed class Visitor : IBattleStatusEffectVisitor<Empty, StatusEffect>, IStaticInstance<Visitor>
        {
            public StatusEffect Visit(BattleDefendStatusEffect effect, Empty context) => new(StatusEffectType.Defend);
            public StatusEffect Visit(BattleParalyzeStatusEffect effect, Empty context) => new(StatusEffectType.Paralyze);
            public StatusEffect Visit(BattlePetrifyStatusEffect effect, Empty context) => new(StatusEffectType.Petrify);
            public StatusEffect Visit(BattlePoisonStatusEffect effect, Empty context) => new(StatusEffectType.Poison);
            public StatusEffect Visit(BattleFrostbiteStatusEffect effect, Empty context) => new(StatusEffectType.Frostbite);
            public StatusEffect Visit(BattleBlisterStatusEffect effect, Empty context) => new(StatusEffectType.Blister);
            public StatusEffect Visit(BattleRetreatStatusEffect effect, Empty context) => new(StatusEffectType.Retreat);
            public StatusEffect Visit(BattleRetreatedStatusEffect effect, Empty context) => new(StatusEffectType.Retreated);
            public StatusEffect Visit(BattleFortificationStatusEffect effect, Empty context) => new(StatusEffectType.Fortification);
            public StatusEffect Visit(BattleIncreaseDamageStatusEffect effect, Empty context) => new(StatusEffectType.IncreaseDamage);
            public StatusEffect Visit(BattleReduceDamageStatusEffect effect, Empty context) => new(StatusEffectType.ReduceDamage);
            public StatusEffect Visit(BattleReduceInitiativeStatusEffect effect, Empty context) => new(StatusEffectType.ReduceInitiative);
            public StatusEffect Visit(BattleShatterArmorStatusEffect effect, Empty context) => new(StatusEffectType.ShatterArmor);
            public StatusEffect Visit(BattleCureStatusEffect effect, Empty context) => new(StatusEffectType.Cure);

            public StatusEffect Visit(BattleTransformStatusEffect effect, Empty context)
            {
                StatusEffectType type = effect.Type switch
                {
                    BattleTransformType.Transform => StatusEffectType.Transform,
                    BattleTransformType.TransformSelf => StatusEffectType.TransformSelf,
                    BattleTransformType.Doppelganger => StatusEffectType.Doppelganger,
                    BattleTransformType.LevelDrain => StatusEffectType.LevelDrain
                };

                return new StatusEffect(type);
            }
        }

        public static StatusEffect Create(BattleAbstractStatusEffect effect)
        {
            return effect.Accept(Visitor.Instance, Empty.Value);
        }

        public static IEnumerable<StatusEffect> CreateAll(IEnumerable<BattleAbstractStatusEffect> effects)
        {
            foreach (BattleAbstractStatusEffect effect in effects)
            {
                yield return Create(effect);
            }
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
