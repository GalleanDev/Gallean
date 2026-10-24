namespace Gallean.Engine;

internal partial class WorldEngine
{
    private abstract class WorldEventRule(IEngineContext context) : IWorldEventVisitor<WorldState, EventRuleResult<WorldEvent>>, IEventRule<WorldState, WorldEvent>
    {
        protected IEngineContext Context { get; } = context;

        public EventRuleResult<WorldEvent> Apply(WorldEvent ev, WorldState state)
        {
            return ev.Accept(this, state);
        }

        public virtual EventRuleResult<WorldEvent> Visit(WorldAiActionStartedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldAiActionEndedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldAllianceStartedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldAllianceEndedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldBagPickedUpEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldItemDroppedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldBuildingConstructedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldCityBribedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldCityRiotStartedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldCityRiotTurnsDecreasedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldCityRiotEndedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldCityUpgradedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldDiplomacyProposalSentEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldDiplomacyProposalAnsweredEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldDiplomacyItemTradedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldDiplomacySpellTradedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldFogConcealedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldFogRevealedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldFortificationBattleStartedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldFortificationCapturingEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldFortificationCapturedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldFortificationPoisonedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldFortificationBattleEndedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldInventoryItemTransferredEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldItemEquippedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldItemsReceivedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldItemPurchasedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldItemSoldEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldItemUnequippedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldItemUsedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldLeaderUpgradeGrantedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldLeaderUpgradeSelectedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldMapFeatureChangedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldMercenaryPurchasedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldMerchantItemStolenEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldMountainsRemovedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldPlayerDefeatedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldRodCreatedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldRodDestroyedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldRoundStartedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldRoundEndedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldScenarioEndedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldScenarioPopupEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldScenarioEventTriggeringEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldScenarioEventTriggeredEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldScenarioCheckEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldRuinLootedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldRuinSpiedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldSiteVisitedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldFortificationEnteredEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldFortificationExitedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldSpellCastEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldSpellPurchasedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldSpellResearchedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldSpellsReceivedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldSpellStolenEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldSpyAddedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldSpyRemovedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldGameResumedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldGoldGivenEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldRelationChangedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldResourcesStolenEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldStackAutoFittedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldStackBattleStartedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldStackBattleEndedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldStackCreatedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldStackDestroyedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldUnitsShuffledEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldThiefActionSucceededEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldThiefActionFailedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldStackConcealedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldStackRevealedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldStackMovingEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldStackMovedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldStackMovementConsumedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldStackMovementRestoredEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldStackOrderChangedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldStackOwnerChangedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldStackPoisonedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldStackRelocatedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldStackRemovedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldStackUnitTrainedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldTurnStartingEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldTurnResourcesGeneratedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldTurnStartedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldTurnEndedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldUnitDisbandedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldUnitDamagedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldUnitHealedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldUnitTypeLockedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldUnitModifierAppliedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldUnitModifierDurationDecreasedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldUnitModifierExpiredEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldUnitPurchasedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldUnitRepositionedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldUnitResurrectedEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
        public virtual EventRuleResult<WorldEvent> Visit(WorldUnitTransferredEvent ev, WorldState state) => EventRuleResult<WorldEvent>.Empty;
    }
}
