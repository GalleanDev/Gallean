using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed class WorldFinishedStacks : IWorldEventVisitor<WorldState, Empty>, IDisposable
{
    private readonly Lock locker = new();
    private readonly HashSet<WorldStackId> stacks = [];
    private readonly IDisposable subscription;

    public WorldFinishedStacks(IObservable<EngineStep<WorldState, WorldEvent>> events)
    {
        subscription = events.Subscribe(step => step.Event.Accept(this, step.NewState));
    }

    public void Add(WorldStackId stackId)
    {
        lock (locker)
        {
            stacks.Add(stackId);
        }
    }

    public bool Contains(WorldStackId stackId)
    {
        lock (locker)
        {
            return stacks.Contains(stackId);
        }
    }

    public void Dispose()
    {
        subscription.Dispose();
    }

    private Empty Resume(WorldStackId stackId)
    {
        lock (locker)
        {
            stacks.Remove(stackId);
        }

        return Empty.Value;
    }

    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldAiActionStartedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldAiActionEndedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldAllianceStartedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldAllianceEndedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldBagPickedUpEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldItemDroppedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldBuildingConstructedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldCityBribedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldCityRiotStartedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldCityRiotTurnsDecreasedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldCityRiotEndedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldCityUpgradedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldDiplomacyProposalSentEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldDiplomacyProposalAnsweredEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldDiplomacyItemTradedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldDiplomacySpellTradedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldFogConcealedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldFogRevealedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldFortificationBattleStartedEvent ev, WorldState state) => Resume(ev.AttackerStackId);
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldFortificationCapturingEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldFortificationCapturedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldFortificationPoisonedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldFortificationBattleEndedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldGameResumedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldGoldGivenEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldRelationChangedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldResourcesStolenEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldInventoryItemTransferredEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldItemEquippedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldItemsReceivedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldItemPurchasedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldItemSoldEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldItemUnequippedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldItemUsedEvent ev, WorldState state) => Resume(ev.StackId);
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldLeaderUpgradeGrantedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldLeaderUpgradeSelectedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldMapFeatureChangedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldMercenaryPurchasedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldMerchantItemStolenEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldMountainsRemovedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldPlayerDefeatedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldResourceNodeCapturedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldRodCreatedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldRodDestroyedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldRoundStartedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldRoundEndedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldScenarioEndedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldScenarioPopupEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldScenarioEventTriggeringEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldScenarioEventTriggeredEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldScenarioCheckEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldRuinLootedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldRuinSpiedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldSiteVisitedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldFortificationEnteredEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldFortificationExitedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldSpellCastEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldSpellPurchasedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldSpellResearchedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldSpellsReceivedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldSpellStolenEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldSpyAddedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldSpyRemovedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldStackAutoFittedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldStackBattleStartedEvent ev, WorldState state) => Resume(ev.AttackerStackId);
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldStackBattleEndedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldStackCreatedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldStackDestroyedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldUnitsShuffledEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldThiefActionSucceededEvent ev, WorldState state) => Resume(ev.ThiefStackId);
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldThiefActionFailedEvent ev, WorldState state) => Resume(ev.ThiefStackId);
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldStackConcealedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldStackRevealedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldStackMovingEvent ev, WorldState state) => Resume(ev.StackId);
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldStackMovedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldStackMovementConsumedEvent ev, WorldState state) => Resume(ev.StackId);
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldStackMovementRestoredEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldStackOrderChangedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldStackOwnerChangedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldStackPoisonedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldStackRelocatedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldStackRemovedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldStackUnitTrainedEvent ev, WorldState state) => Empty.Value;

    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldTurnStartingEvent ev, WorldState state)
    {
        lock (locker)
        {
            stacks.RemoveWhere(stackId => !state.Stacks.TryGetValue(stackId, out WorldStack? stack) || stack.OwnerId == ev.PlayerId);
        }

        return Empty.Value;
    }

    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldTurnResourcesGeneratedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldTurnStartedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldTurnEndedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldUnitDisbandedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldUnitDamagedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldUnitHealedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldUnitTypeLockedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldUnitModifierAppliedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldUnitModifierDurationDecreasedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldUnitModifierExpiredEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldUnitPurchasedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldUnitRepositionedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldUnitResurrectedEvent ev, WorldState state) => Empty.Value;
    Empty IWorldEventVisitor<WorldState, Empty>.Visit(WorldUnitTransferredEvent ev, WorldState state) => Empty.Value;
}
