namespace Gallean.Engine;

internal static class BattleStatusEffectExtensions
{
    extension(BattleAbstractStatusEffect effect)
    {
        public bool IsCurable()
        {
            return effect switch
            {
                BattleDefendStatusEffect => false,
                BattleParalyzeStatusEffect => true,
                BattlePetrifyStatusEffect => true,
                BattlePoisonStatusEffect => true,
                BattleFrostbiteStatusEffect => true,
                BattleBlisterStatusEffect => true,
                BattleRetreatStatusEffect => false,
                BattleRetreatedStatusEffect => false,
                BattleFortificationStatusEffect => false,
                BattleIncreaseDamageStatusEffect => false,
                BattleReduceDamageStatusEffect => true,
                BattleReduceInitiativeStatusEffect => true,
                BattleShatterArmorStatusEffect => false,
                BattleCureStatusEffect => false,
                BattleTransformStatusEffect transform => IsCurableTransform(transform.Type)
            };
        }
    }

    private static bool IsCurableTransform(BattleTransformType type)
    {
        return type switch
        {
            BattleTransformType.Transform => true,
            BattleTransformType.TransformSelf => false,
            BattleTransformType.Doppelganger => false,
            BattleTransformType.LevelDrain => false
        };
    }
}
