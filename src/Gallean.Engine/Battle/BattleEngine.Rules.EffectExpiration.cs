namespace Gallean.Engine;

internal partial class BattleEngine
{
    private sealed class EffectExpirationRule(IEngineContext context) : BattleEventRule(context)
    {
        public override EventRuleResult<BattleEvent> Visit(BattleUnitEffectDurationDecreasedEvent ev, BattleState state)
        {
            BattleUnit unit = state.GetUnit(ev.UnitId);
            List<BattleEvent> events = [];

            foreach (BattleAbstractStatusEffect effect in unit.StatusEffects)
            {
                if (IsExpired(effect))
                {
                    events.Add(new BattleUnitEffectExpiredEvent(ev.UnitId, effect));
                }
            }

            if (events.Count > 0)
            {
                return EventRuleResult<BattleEvent>.Append([.. events]);
            }
            else
            {
                return EventRuleResult<BattleEvent>.Empty;
            }
        }

        private static bool IsExpired(BattleAbstractStatusEffect effect)
        {
            return effect switch
            {
                BattleDefendStatusEffect => false,
                BattleParalyzeStatusEffect paralyze => paralyze.RemainingTurns <= 0,
                BattlePetrifyStatusEffect petrify => petrify.RemainingTurns <= 0,
                BattlePoisonStatusEffect poison => poison.RemainingTurns <= 0,
                BattleFrostbiteStatusEffect frostbite => frostbite.RemainingTurns <= 0,
                BattleBlisterStatusEffect blister => blister.RemainingTurns <= 0,
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
    }
}
