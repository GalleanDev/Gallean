namespace Gallean.Engine;

public interface IBattleEventVisitor<in TContext, out TResult>
{
    TResult Visit(BattleAutoResolveStepEvent ev, TContext context);
    TResult Visit(BattleEndedEvent ev, TContext context);
    TResult Visit(BattleItemUsedEvent ev, TContext context);
    TResult Visit(BattleRoundStartedEvent ev, TContext context);
    TResult Visit(BattleRoundEndedEvent ev, TContext context);
    TResult Visit(BattleUnitAttackEvent ev, TContext context);
    TResult Visit(BattleUnitAttackBlockedEvent ev, TContext context);
    TResult Visit(BattleUnitAttackGrantedEvent ev, TContext context);
    TResult Visit(BattleUnitAttackHitEvent ev, TContext context);
    TResult Visit(BattleUnitAttackMissedEvent ev, TContext context);
    TResult Visit(BattleUnitAttackStepEvent ev, TContext context);
    TResult Visit(BattleUnitAttackedEvent ev, TContext context);
    TResult Visit(BattleUnitCuredEvent ev, TContext context);
    TResult Visit(BattleUnitDamagedByEffectEvent ev, TContext context);
    TResult Visit(BattleUnitDefendedEvent ev, TContext context);
    TResult Visit(BattleUnitDiedEvent ev, TContext context);
    TResult Visit(BattleUnitEffectAppliedEvent ev, TContext context);
    TResult Visit(BattleUnitEffectDurationDecreasedEvent ev, TContext context);
    TResult Visit(BattleUnitEffectExpiredEvent ev, TContext context);
    TResult Visit(BattleUnitExperienceGainedEvent ev, TContext context);
    TResult Visit(BattleUnitHealedEvent ev, TContext context);
    TResult Visit(BattleUnitModifierAppliedEvent ev, TContext context);
    TResult Visit(BattleUnitRetreatedEvent ev, TContext context);
    TResult Visit(BattleUnitRetreatingEvent ev, TContext context);
    TResult Visit(BattleUnitRevivedEvent ev, TContext context);
    TResult Visit(BattleUnitSummonedEvent ev, TContext context);
    TResult Visit(BattleUnitUnsummonedEvent ev, TContext context);
    TResult Visit(BattleUnitTransformAppliedEvent ev, TContext context);
    TResult Visit(BattleUnitTransformRestoredEvent ev, TContext context);
    TResult Visit(BattleUnitTurnStartedEvent ev, TContext context);
    TResult Visit(BattleUnitTurnSkippedEvent ev, TContext context);
    TResult Visit(BattleUnitTurnEndingEvent ev, TContext context);
    TResult Visit(BattleUnitTurnEndedEvent ev, TContext context);
    TResult Visit(BattleUnitUpgradedEvent ev, TContext context);
    TResult Visit(BattleUnitWaitedEvent ev, TContext context);
    TResult Visit(BattleUnitWardGrantedEvent ev, TContext context);
    TResult Visit(BattleUnitWardConsumedEvent ev, TContext context);
    TResult Visit(BattleUnitWardExpiredEvent ev, TContext context);
}
