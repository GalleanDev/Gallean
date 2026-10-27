using Gallean.Common;

namespace Gallean.Engine;

internal partial class BattleEngine
{
    private sealed class DamageOverTimeVisitor : IBattleStatusEffectVisitor<Empty, int>, IStaticInstance<DamageOverTimeVisitor>
    {
        public int Visit(BattleDefendStatusEffect effect, Empty context) => 0;
        public int Visit(BattleParalyzeStatusEffect effect, Empty context) => 0;
        public int Visit(BattlePetrifyStatusEffect effect, Empty context) => 0;
        public int Visit(BattlePoisonStatusEffect effect, Empty context) => effect.Damage;
        public int Visit(BattleFrostbiteStatusEffect effect, Empty context) => effect.Damage;
        public int Visit(BattleBlisterStatusEffect effect, Empty context) => effect.Damage;
        public int Visit(BattleRetreatStatusEffect effect, Empty context) => 0;
        public int Visit(BattleRetreatedStatusEffect effect, Empty context) => 0;
        public int Visit(BattleFortificationStatusEffect effect, Empty context) => 0;
        public int Visit(BattleIncreaseDamageStatusEffect effect, Empty context) => 0;
        public int Visit(BattleReduceDamageStatusEffect effect, Empty context) => 0;
        public int Visit(BattleReduceInitiativeStatusEffect effect, Empty context) => 0;
        public int Visit(BattleShatterArmorStatusEffect effect, Empty context) => 0;
        public int Visit(BattleCureStatusEffect effect, Empty context) => 0;
        public int Visit(BattleTransformStatusEffect effect, Empty context) => 0;
    }

    private sealed class TurnDurationVisitor : IBattleStatusEffectVisitor<Empty, bool>, IStaticInstance<TurnDurationVisitor>
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
        public bool Visit(BattleReduceDamageStatusEffect effect, Empty context) => false;
        public bool Visit(BattleReduceInitiativeStatusEffect effect, Empty context) => false;
        public bool Visit(BattleShatterArmorStatusEffect effect, Empty context) => false;
        public bool Visit(BattleCureStatusEffect effect, Empty context) => false;
        public bool Visit(BattleTransformStatusEffect effect, Empty context) => effect.RemainingTurns is not null;
    }

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
                    int damage = effect.Accept(DamageOverTimeVisitor.Instance, Empty.Value);

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
                    events.Add(BattleTransformationCalculator.GetRestoreEvent(state, context, ev.UnitId, transformEffect));
                }

                if (unit.StatusEffects.Any(effect => effect.Accept(TurnDurationVisitor.Instance, Empty.Value)))
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
    }
}
