namespace Gallean.Engine;

internal partial class BattleEngine
{
    private abstract class BattleEventRule(IEngineContext context) : IBattleEventVisitor<BattleState, EventRuleResult<BattleEvent>>, IEventRule<BattleState, BattleEvent>
    {
        protected IEngineContext Context { get; } = context;

        public virtual EventRuleResult<BattleEvent> Visit(BattleAutoResolveStepEvent ev, BattleState state) => EventRuleResult<BattleEvent>.Empty;
        public virtual EventRuleResult<BattleEvent> Visit(BattleEndedEvent ev, BattleState state) => EventRuleResult<BattleEvent>.Empty;
        public virtual EventRuleResult<BattleEvent> Visit(BattleItemUsedEvent ev, BattleState state) => EventRuleResult<BattleEvent>.Empty;
        public virtual EventRuleResult<BattleEvent> Visit(BattleRoundStartedEvent ev, BattleState state) => EventRuleResult<BattleEvent>.Empty;
        public virtual EventRuleResult<BattleEvent> Visit(BattleRoundEndedEvent ev, BattleState state) => EventRuleResult<BattleEvent>.Empty;
        public virtual EventRuleResult<BattleEvent> Visit(BattleUnitAttackEvent ev, BattleState state) => EventRuleResult<BattleEvent>.Empty;
        public virtual EventRuleResult<BattleEvent> Visit(BattleUnitAttackBlockedEvent ev, BattleState state) => EventRuleResult<BattleEvent>.Empty;
        public virtual EventRuleResult<BattleEvent> Visit(BattleUnitAttackGrantedEvent ev, BattleState state) => EventRuleResult<BattleEvent>.Empty;
        public virtual EventRuleResult<BattleEvent> Visit(BattleUnitAttackHitEvent ev, BattleState state) => EventRuleResult<BattleEvent>.Empty;
        public virtual EventRuleResult<BattleEvent> Visit(BattleUnitAttackMissedEvent ev, BattleState state) => EventRuleResult<BattleEvent>.Empty;
        public virtual EventRuleResult<BattleEvent> Visit(BattleUnitAttackStepEvent ev, BattleState state) => EventRuleResult<BattleEvent>.Empty;
        public virtual EventRuleResult<BattleEvent> Visit(BattleUnitAttackedEvent ev, BattleState state) => EventRuleResult<BattleEvent>.Empty;
        public virtual EventRuleResult<BattleEvent> Visit(BattleUnitCuredEvent ev, BattleState state) => EventRuleResult<BattleEvent>.Empty;
        public virtual EventRuleResult<BattleEvent> Visit(BattleUnitDamagedByEffectEvent ev, BattleState state) => EventRuleResult<BattleEvent>.Empty;
        public virtual EventRuleResult<BattleEvent> Visit(BattleUnitDefendedEvent ev, BattleState state) => EventRuleResult<BattleEvent>.Empty;
        public virtual EventRuleResult<BattleEvent> Visit(BattleUnitDiedEvent ev, BattleState state) => EventRuleResult<BattleEvent>.Empty;
        public virtual EventRuleResult<BattleEvent> Visit(BattleUnitEffectAppliedEvent ev, BattleState state) => EventRuleResult<BattleEvent>.Empty;
        public virtual EventRuleResult<BattleEvent> Visit(BattleUnitEffectDurationDecreasedEvent ev, BattleState state) => EventRuleResult<BattleEvent>.Empty;
        public virtual EventRuleResult<BattleEvent> Visit(BattleUnitEffectExpiredEvent ev, BattleState state) => EventRuleResult<BattleEvent>.Empty;
        public virtual EventRuleResult<BattleEvent> Visit(BattleUnitExperienceGainedEvent ev, BattleState state) => EventRuleResult<BattleEvent>.Empty;
        public virtual EventRuleResult<BattleEvent> Visit(BattleUnitHealedEvent ev, BattleState state) => EventRuleResult<BattleEvent>.Empty;
        public virtual EventRuleResult<BattleEvent> Visit(BattleUnitModifierAppliedEvent ev, BattleState state) => EventRuleResult<BattleEvent>.Empty;
        public virtual EventRuleResult<BattleEvent> Visit(BattleUnitRetreatedEvent ev, BattleState state) => EventRuleResult<BattleEvent>.Empty;
        public virtual EventRuleResult<BattleEvent> Visit(BattleUnitRetreatingEvent ev, BattleState state) => EventRuleResult<BattleEvent>.Empty;
        public virtual EventRuleResult<BattleEvent> Visit(BattleUnitRevivedEvent ev, BattleState state) => EventRuleResult<BattleEvent>.Empty;
        public virtual EventRuleResult<BattleEvent> Visit(BattleUnitSummonedEvent ev, BattleState state) => EventRuleResult<BattleEvent>.Empty;
        public virtual EventRuleResult<BattleEvent> Visit(BattleUnitUnsummonedEvent ev, BattleState state) => EventRuleResult<BattleEvent>.Empty;
        public virtual EventRuleResult<BattleEvent> Visit(BattleUnitTransformAppliedEvent ev, BattleState state) => EventRuleResult<BattleEvent>.Empty;
        public virtual EventRuleResult<BattleEvent> Visit(BattleUnitTransformRestoredEvent ev, BattleState state) => EventRuleResult<BattleEvent>.Empty;
        public virtual EventRuleResult<BattleEvent> Visit(BattleUnitTurnStartedEvent ev, BattleState state) => EventRuleResult<BattleEvent>.Empty;
        public virtual EventRuleResult<BattleEvent> Visit(BattleUnitTurnSkippedEvent ev, BattleState state) => EventRuleResult<BattleEvent>.Empty;
        public virtual EventRuleResult<BattleEvent> Visit(BattleUnitTurnEndingEvent ev, BattleState state) => EventRuleResult<BattleEvent>.Empty;
        public virtual EventRuleResult<BattleEvent> Visit(BattleUnitTurnEndedEvent ev, BattleState state) => EventRuleResult<BattleEvent>.Empty;
        public virtual EventRuleResult<BattleEvent> Visit(BattleUnitUpgradedEvent ev, BattleState state) => EventRuleResult<BattleEvent>.Empty;
        public virtual EventRuleResult<BattleEvent> Visit(BattleUnitWaitedEvent ev, BattleState state) => EventRuleResult<BattleEvent>.Empty;
        public virtual EventRuleResult<BattleEvent> Visit(BattleUnitWardGrantedEvent ev, BattleState state) => EventRuleResult<BattleEvent>.Empty;
        public virtual EventRuleResult<BattleEvent> Visit(BattleUnitWardConsumedEvent ev, BattleState state) => EventRuleResult<BattleEvent>.Empty;
        public virtual EventRuleResult<BattleEvent> Visit(BattleUnitWardExpiredEvent ev, BattleState state) => EventRuleResult<BattleEvent>.Empty;

        public EventRuleResult<BattleEvent> Apply(BattleEvent ev, BattleState state)
        {
            return ev.Accept(this, state);
        }
    }
}
