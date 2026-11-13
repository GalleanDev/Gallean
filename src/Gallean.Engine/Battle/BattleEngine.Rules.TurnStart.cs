namespace Gallean.Engine;

internal partial class BattleEngine
{
    private sealed class TurnStartRule(IEngineContext context) : BattleEventRule(context)
    {
        public override EventRuleResult<BattleEvent> Visit(BattleUnitTurnStartedEvent ev, BattleState state)
        {
            if (state.Outcome is not null)
            {
                return EventRuleResult<BattleEvent>.Empty;
            }

            List<BattleEvent> events = [];

            BattleUnit unit = state.GetUnit(ev.UnitId);

            bool expiresModifiers = ev.Turn.Type switch
            {
                BattleUnitTurnType.Normal => true,
                BattleUnitTurnType.SecondAttack => false,
                BattleUnitTurnType.GrantedAttack => false,
                BattleUnitTurnType.Resumed => false,
                BattleUnitTurnType.Doppelganger => false
            };

            if (expiresModifiers)
            {
                foreach (BattleUnitId unitId in state.GetAllUnitIds())
                {
                    BattleUnit target = state.GetUnit(unitId);

                    foreach (BattleUnitWardModifier wardModifier in target.WardModifiers)
                    {
                        if (wardModifier.CasterId == ev.UnitId)
                        {
                            events.Add(new BattleUnitWardExpiredEvent(unitId, wardModifier));
                        }
                    }
                }
            }

            bool expiresEffects = ev.Turn.Type switch
            {
                BattleUnitTurnType.Normal => true,
                BattleUnitTurnType.SecondAttack => false,
                BattleUnitTurnType.GrantedAttack => false,
                BattleUnitTurnType.Resumed => false,
                BattleUnitTurnType.Doppelganger => false
            };

            if (expiresEffects)
            {
                int hitpoints = unit.Hitpoints;

                foreach (BattleAbstractStatusEffect effect in unit.StatusEffects)
                {
                    int damage = GetDamageOverTime(effect);

                    if (damage > 0)
                    {
                        hitpoints = Math.Max(0, hitpoints - damage);
                        events.Add(new BattleUnitDamagedByEffectEvent(ev.UnitId, effect, damage, hitpoints));

                        if (hitpoints <= 0)
                        {
                            events.Add(new BattleUnitDiedEvent(ev.UnitId));

                            return EventRuleResult<BattleEvent>.Append([.. events]);
                        }
                    }
                }

                BattleTransformStatusEffect? transformEffect = BattleTransformationCalculator.GetTransformEffect(unit);
                if (transformEffect is { RemainingTurns: <= 1 })
                {
                    events.Add(BattleTransformationCalculator.GetRestoreEvent(state, Context, ev.UnitId, transformEffect));
                }

                if (unit.StatusEffects.Any(HasTurnDuration))
                {
                    events.Add(new BattleUnitEffectDurationDecreasedEvent(ev.UnitId));
                }
            }

            if (unit.IsDisabled())
            {
                events.Add(new BattleUnitTurnSkippedEvent(ev.UnitId));
                events.Add(new BattleUnitTurnEndingEvent(ev.UnitId));

                return EventRuleResult<BattleEvent>.Append([.. events]);
            }

            bool resolvesRetreat = ev.Turn.Type switch
            {
                BattleUnitTurnType.Normal => true,
                BattleUnitTurnType.SecondAttack => true,
                BattleUnitTurnType.GrantedAttack => false,
                BattleUnitTurnType.Resumed => true,
                BattleUnitTurnType.Doppelganger => true
            };

            if (resolvesRetreat && unit.IsRetreating())
            {
                if (unit.SummonerId is not null)
                {
                    events.Add(new BattleUnitUnsummonedEvent(ev.UnitId));
                }
                else
                {
                    events.Add(new BattleUnitRetreatedEvent(ev.UnitId));
                }

                events.Add(new BattleUnitTurnEndingEvent(ev.UnitId));

                return EventRuleResult<BattleEvent>.Append([.. events]);
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

        private static int GetDamageOverTime(BattleAbstractStatusEffect effect)
        {
            return effect switch
            {
                BattleDefendStatusEffect => 0,
                BattleParalyzeStatusEffect => 0,
                BattlePetrifyStatusEffect => 0,
                BattlePoisonStatusEffect poison => poison.Damage,
                BattleFrostbiteStatusEffect frostbite => frostbite.Damage,
                BattleBlisterStatusEffect blister => blister.Damage,
                BattleRetreatStatusEffect => 0,
                BattleRetreatedStatusEffect => 0,
                BattleFortificationStatusEffect => 0,
                BattleIncreaseDamageStatusEffect => 0,
                BattleReduceDamageStatusEffect => 0,
                BattleReduceInitiativeStatusEffect => 0,
                BattleShatterArmorStatusEffect => 0,
                BattleCureStatusEffect => 0,
                BattleTransformStatusEffect => 0
            };
        }

        private static bool HasTurnDuration(BattleAbstractStatusEffect effect)
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
                BattleReduceDamageStatusEffect => false,
                BattleReduceInitiativeStatusEffect => false,
                BattleShatterArmorStatusEffect => false,
                BattleCureStatusEffect => false,
                BattleTransformStatusEffect transform => transform.RemainingTurns is not null
            };
        }
    }
}
