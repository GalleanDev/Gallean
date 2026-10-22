using Gallean.Common;

namespace Gallean.Engine;

internal static class BattleStatusEffectExtensions
{
    extension(BattleAbstractStatusEffect effect)
    {
        public bool IsCurable()
        {
            return effect.Accept(IsCurableEffectVisitor.Instance, Empty.Value);
        }
    }

    private sealed class IsCurableEffectVisitor : IBattleStatusEffectVisitor<Empty, bool>, IStaticInstance<IsCurableEffectVisitor>
    {
        public bool Visit(BattleDefendStatusEffect effect, Empty context) => false;
        public bool Visit(BattleParalyzeStatusEffect effect, Empty context) => true;
        public bool Visit(BattlePetrifyStatusEffect effect, Empty context) => true;
        public bool Visit(BattlePoisonStatusEffect effect, Empty context) => true;
        public bool Visit(BattleFrostbiteStatusEffect effect, Empty context) => true;
        public bool Visit(BattleBlisterStatusEffect effect, Empty context) => true;
        public bool Visit(BattleRetreatStatusEffect effect, Empty context) => false;
        public bool Visit(BattleRetreatedStatusEffect effect, Empty context) => false;
        public bool Visit(BattleFortificationStatusEffect effect, Empty context) => false;
        public bool Visit(BattleIncreaseDamageStatusEffect effect, Empty context) => false;
        public bool Visit(BattleReduceDamageStatusEffect effect, Empty context) => true;
        public bool Visit(BattleReduceInitiativeStatusEffect effect, Empty context) => true;
        public bool Visit(BattleShatterArmorStatusEffect effect, Empty context) => false;
        public bool Visit(BattleCureStatusEffect effect, Empty context) => false;

        public bool Visit(BattleTransformStatusEffect effect, Empty context)
        {
            return effect.Type switch
            {
                BattleTransformType.Transform => true,
                BattleTransformType.TransformSelf => false,
                BattleTransformType.Doppelganger => false,
                BattleTransformType.LevelDrain => false
            };
        }
    }
}
