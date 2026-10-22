using Gallean.Common;

namespace Gallean.Engine;

public static class BattleUnitExtensions
{
    extension(BattleUnit unit)
    {
        public bool HasStatusEffect<TEffect>() where TEffect : BattleAbstractStatusEffect
        {
            return unit.StatusEffects.Any(effect => effect is TEffect);
        }

        public bool HasAnyStatusEffect<T1, T2>() where T1 : BattleAbstractStatusEffect where T2 : BattleAbstractStatusEffect
        {
            return unit.StatusEffects.Any(effect => effect is T1 or T2);
        }

        internal bool IsRetreating()
        {
            if (!unit.IsAlive)
            {
                return false;
            }
            else
            {
                return unit.StatusEffects.Any(effect => effect.Accept(IsRetreatingVisitor.Instance, Empty.Value));
            }
        }

        internal bool IsDisabled()
        {
            if (!unit.IsAlive)
            {
                return false;
            }
            else
            {
                return unit.StatusEffects.Any(effect => effect.Accept(IsDisabledVisitor.Instance, Empty.Value));
            }
        }

        internal bool IsActive()
        {
            if (!unit.IsAlive)
            {
                return false;
            }
            else if (unit.StatusEffects.Any(effect => effect.Accept(IsRetreatedVisitor.Instance, Empty.Value)))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    private sealed class IsRetreatedVisitor : IBattleStatusEffectVisitor<Empty, bool>, IStaticInstance<IsRetreatedVisitor>
    {
        public bool Visit(BattleDefendStatusEffect effect, Empty context) => false;
        public bool Visit(BattleParalyzeStatusEffect effect, Empty context) => false;
        public bool Visit(BattlePetrifyStatusEffect effect, Empty context) => false;
        public bool Visit(BattlePoisonStatusEffect effect, Empty context) => false;
        public bool Visit(BattleFrostbiteStatusEffect effect, Empty context) => false;
        public bool Visit(BattleBlisterStatusEffect effect, Empty context) => false;
        public bool Visit(BattleRetreatStatusEffect effect, Empty context) => false;
        public bool Visit(BattleRetreatedStatusEffect effect, Empty context) => true;
        public bool Visit(BattleFortificationStatusEffect effect, Empty context) => false;
        public bool Visit(BattleIncreaseDamageStatusEffect effect, Empty context) => false;
        public bool Visit(BattleReduceDamageStatusEffect effect, Empty context) => false;
        public bool Visit(BattleReduceInitiativeStatusEffect effect, Empty context) => false;
        public bool Visit(BattleShatterArmorStatusEffect effect, Empty context) => false;
        public bool Visit(BattleCureStatusEffect effect, Empty context) => false;
        public bool Visit(BattleTransformStatusEffect effect, Empty context) => false;
    }

    private sealed class IsDisabledVisitor : IBattleStatusEffectVisitor<Empty, bool>, IStaticInstance<IsDisabledVisitor>
    {
        public bool Visit(BattleDefendStatusEffect effect, Empty context) => false;
        public bool Visit(BattleParalyzeStatusEffect effect, Empty context) => true;
        public bool Visit(BattlePetrifyStatusEffect effect, Empty context) => true;
        public bool Visit(BattlePoisonStatusEffect effect, Empty context) => false;
        public bool Visit(BattleFrostbiteStatusEffect effect, Empty context) => false;
        public bool Visit(BattleBlisterStatusEffect effect, Empty context) => false;
        public bool Visit(BattleRetreatStatusEffect effect, Empty context) => false;
        public bool Visit(BattleRetreatedStatusEffect effect, Empty context) => false;
        public bool Visit(BattleFortificationStatusEffect effect, Empty context) => false;
        public bool Visit(BattleIncreaseDamageStatusEffect effect, Empty context) => false;
        public bool Visit(BattleReduceDamageStatusEffect effect, Empty context) => false;
        public bool Visit(BattleReduceInitiativeStatusEffect effect, Empty context) => false;
        public bool Visit(BattleShatterArmorStatusEffect effect, Empty context) => false;
        public bool Visit(BattleCureStatusEffect effect, Empty context) => false;
        public bool Visit(BattleTransformStatusEffect effect, Empty context) => false;
    }

    private sealed class IsRetreatingVisitor : IBattleStatusEffectVisitor<Empty, bool>, IStaticInstance<IsRetreatingVisitor>
    {
        public bool Visit(BattleDefendStatusEffect effect, Empty context) => false;
        public bool Visit(BattleParalyzeStatusEffect effect, Empty context) => false;
        public bool Visit(BattlePetrifyStatusEffect effect, Empty context) => false;
        public bool Visit(BattlePoisonStatusEffect effect, Empty context) => false;
        public bool Visit(BattleFrostbiteStatusEffect effect, Empty context) => false;
        public bool Visit(BattleBlisterStatusEffect effect, Empty context) => false;
        public bool Visit(BattleRetreatStatusEffect effect, Empty context) => true;
        public bool Visit(BattleRetreatedStatusEffect effect, Empty context) => false;
        public bool Visit(BattleFortificationStatusEffect effect, Empty context) => false;
        public bool Visit(BattleIncreaseDamageStatusEffect effect, Empty context) => false;
        public bool Visit(BattleReduceDamageStatusEffect effect, Empty context) => false;
        public bool Visit(BattleReduceInitiativeStatusEffect effect, Empty context) => false;
        public bool Visit(BattleShatterArmorStatusEffect effect, Empty context) => false;
        public bool Visit(BattleCureStatusEffect effect, Empty context) => false;
        public bool Visit(BattleTransformStatusEffect effect, Empty context) => false;
    }
}
