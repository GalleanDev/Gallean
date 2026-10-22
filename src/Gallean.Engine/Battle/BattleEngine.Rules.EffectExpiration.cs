using Gallean.Common;

namespace Gallean.Engine;

internal partial class BattleEngine
{
    private sealed class EffectExpiredVisitor : IBattleStatusEffectVisitor<Empty, bool>, IStaticInstance<EffectExpiredVisitor>
    {
        public bool Visit(BattleDefendStatusEffect effect, Empty context) => false;
        public bool Visit(BattleParalyzeStatusEffect effect, Empty context) => effect.RemainingTurns <= 0;
        public bool Visit(BattlePetrifyStatusEffect effect, Empty context) => effect.RemainingTurns <= 0;
        public bool Visit(BattlePoisonStatusEffect effect, Empty context) => effect.RemainingTurns <= 0;
        public bool Visit(BattleFrostbiteStatusEffect effect, Empty context) => effect.RemainingTurns <= 0;
        public bool Visit(BattleBlisterStatusEffect effect, Empty context) => effect.RemainingTurns <= 0;
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

    private sealed class EffectExpirationRule(IEngineContext context) : BattleEventRule(context)
    {
        public override EventRuleResult<BattleEvent> Visit(BattleUnitEffectDurationDecreasedEvent ev, BattleState state)
        {
            BattleUnit unit = state.GetUnit(ev.UnitId);
            List<BattleEvent> events = [];

            foreach (BattleAbstractStatusEffect effect in unit.StatusEffects)
            {
                if (effect.Accept(EffectExpiredVisitor.Instance, Empty.Value))
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
    }
}
