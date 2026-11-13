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
                return unit.StatusEffects.Any(IsRetreatingEffect);
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
                return unit.StatusEffects.Any(IsDisablingEffect);
            }
        }

        internal bool IsActive()
        {
            if (!unit.IsAlive)
            {
                return false;
            }
            else if (unit.StatusEffects.Any(IsRetreatedEffect))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    private static bool IsRetreatingEffect(BattleAbstractStatusEffect effect)
    {
        return effect switch
        {
            BattleDefendStatusEffect => false,
            BattleParalyzeStatusEffect => false,
            BattlePetrifyStatusEffect => false,
            BattlePoisonStatusEffect => false,
            BattleFrostbiteStatusEffect => false,
            BattleBlisterStatusEffect => false,
            BattleRetreatStatusEffect => true,
            BattleRetreatedStatusEffect => false,
            BattleFortificationStatusEffect => false,
            BattleIncreaseDamageStatusEffect => false,
            BattleReduceDamageStatusEffect => false,
            BattleReduceInitiativeStatusEffect => false,
            BattleShatterArmorStatusEffect => false,
            BattleCureStatusEffect => false,
            BattleTransformStatusEffect => false
        };
    }

    private static bool IsDisablingEffect(BattleAbstractStatusEffect effect)
    {
        return effect switch
        {
            BattleDefendStatusEffect => false,
            BattleParalyzeStatusEffect => true,
            BattlePetrifyStatusEffect => true,
            BattlePoisonStatusEffect => false,
            BattleFrostbiteStatusEffect => false,
            BattleBlisterStatusEffect => false,
            BattleRetreatStatusEffect => false,
            BattleRetreatedStatusEffect => false,
            BattleFortificationStatusEffect => false,
            BattleIncreaseDamageStatusEffect => false,
            BattleReduceDamageStatusEffect => false,
            BattleReduceInitiativeStatusEffect => false,
            BattleShatterArmorStatusEffect => false,
            BattleCureStatusEffect => false,
            BattleTransformStatusEffect => false
        };
    }

    private static bool IsRetreatedEffect(BattleAbstractStatusEffect effect)
    {
        return effect switch
        {
            BattleDefendStatusEffect => false,
            BattleParalyzeStatusEffect => false,
            BattlePetrifyStatusEffect => false,
            BattlePoisonStatusEffect => false,
            BattleFrostbiteStatusEffect => false,
            BattleBlisterStatusEffect => false,
            BattleRetreatStatusEffect => false,
            BattleRetreatedStatusEffect => true,
            BattleFortificationStatusEffect => false,
            BattleIncreaseDamageStatusEffect => false,
            BattleReduceDamageStatusEffect => false,
            BattleReduceInitiativeStatusEffect => false,
            BattleShatterArmorStatusEffect => false,
            BattleCureStatusEffect => false,
            BattleTransformStatusEffect => false
        };
    }
}
