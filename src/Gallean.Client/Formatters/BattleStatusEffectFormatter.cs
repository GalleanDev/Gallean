namespace Gallean.Client;

internal static class BattleStatusEffectFormatter
{
    public static string FormatName(BattleAbstractStatusEffect effect)
    {
        return effect switch
        {
            BattleDefendStatusEffect => Tr("Defending"),
            BattleParalyzeStatusEffect => Tr("Paralyzed"),
            BattlePetrifyStatusEffect => Tr("Petrified"),
            BattlePoisonStatusEffect => Tr("Poisoned"),
            BattleFrostbiteStatusEffect => Tr("Frostbitten"),
            BattleBlisterStatusEffect => Tr("Blistered"),
            BattleRetreatStatusEffect => Tr("Retreating"),
            BattleRetreatedStatusEffect => Tr("Retreated"),
            BattleFortificationStatusEffect => Tr("Fortified"),
            BattleIncreaseDamageStatusEffect => Tr("Damage boosted"),
            BattleReduceDamageStatusEffect => Tr("Damage reduced"),
            BattleReduceInitiativeStatusEffect => Tr("Initiative reduced"),
            BattleShatterArmorStatusEffect => Tr("Armor shattered"),
            BattleCureStatusEffect => Tr("Cured"),
            BattleTransformStatusEffect transform => FormatTransformName(transform.Type)
        };
    }

    private static string FormatTransformName(BattleTransformType type)
    {
        return type switch
        {
            BattleTransformType.Transform => Tr("Transformed"),
            BattleTransformType.TransformSelf => Tr("Transformed"),
            BattleTransformType.Doppelganger => Tr("Doppelganger"),
            BattleTransformType.LevelDrain => Tr("Level drained")
        };
    }
}
