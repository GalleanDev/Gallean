using Gallean.Common;

namespace Gallean.Engine;

public sealed partial class GameSession
{
    private sealed class WorldEventHandler(GameSession session) : IWorldEventVisitor<Empty, Empty>
    {
        public Empty Visit(WorldStackBattleStartedEvent ev, Empty context)
        {
            WorldState state = session.World.State;
            WorldPlayerId? attackerOwnerId = StackOwner(state, ev.AttackerStackId);
            WorldPlayerId? defenderOwnerId = StackOwner(state, ev.DefenderStackId);
            bool isAttackerAi = IsAiControlled(state, attackerOwnerId);
            bool isDefenderAi = IsAiControlled(state, defenderOwnerId);
            bool canAutoResolve = !IsKeyStack(state, ev.AttackerStackId) && !IsKeyStack(state, ev.DefenderStackId);

            WorldBattleUnitUpgradeResolver upgradeResolver = CreateUpgradeResolver(state, attackerOwnerId, defenderOwnerId);

            session.StartBattle(ev.InitialBattleState, upgradeResolver, isAttackerAi, isDefenderAi, canAutoResolve, ev.Position, finalState =>
            {
                WorldResolveStackBattleCommand command = new(
                    ev.AttackerStackId,
                    ev.DefenderStackId,
                    finalState,
                    ev.IsDuel);

                WorldCommandResult commandResult = session.World.DispatchCommand(command);

                if (commandResult.Failed)
                {
                    throw new WorldCommandException(commandResult.Error.Value);
                }
            });

            return Empty.Value;
        }

        public Empty Visit(WorldFortificationBattleStartedEvent ev, Empty context)
        {
            WorldState state = session.World.State;
            WorldPlayerId? attackerOwnerId = StackOwner(state, ev.AttackerStackId);
            WorldPlayerId? defenderOwnerId = FortificationOwner(state, ev.DefenderFortificationId);
            bool isAttackerAi = IsAiControlled(state, attackerOwnerId);
            bool isDefenderAi = IsAiControlled(state, defenderOwnerId);
            bool canAutoResolve = !IsKeyStack(state, ev.AttackerStackId);

            WorldBattleUnitUpgradeResolver upgradeResolver = CreateUpgradeResolver(state, attackerOwnerId, defenderOwnerId);

            session.StartBattle(ev.InitialState, upgradeResolver, isAttackerAi, isDefenderAi, canAutoResolve, ev.Position, finalState =>
            {
                WorldResolveFortificationBattleCommand command = new(
                    ev.AttackerStackId,
                    ev.DefenderFortificationId,
                    finalState);

                WorldCommandResult commandResult = session.World.DispatchCommand(command);

                if (commandResult.Failed)
                {
                    throw new WorldCommandException(commandResult.Error.Value);
                }
            });

            return Empty.Value;
        }

        public Empty Visit(WorldBagPickedUpEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldItemDroppedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldBuildingConstructedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldCityBribedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldCityRiotStartedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldCityRiotTurnsDecreasedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldCityRiotEndedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldCityUpgradedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldFortificationBattleEndedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldFortificationCapturingEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldFortificationCapturedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldFortificationPoisonedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldTurnResourcesGeneratedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldResourceNodeCapturedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldStackBattleEndedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldStackCreatedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldStackDestroyedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldUnitsShuffledEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldThiefActionSucceededEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldThiefActionFailedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldStackConcealedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldStackRevealedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldStackRemovedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldStackMovingEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldStackMovedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldStackRelocatedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldStackOrderChangedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldStackOwnerChangedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldFogConcealedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldFogRevealedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldStackMovementConsumedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldStackMovementRestoredEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldTurnStartingEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldTurnStartedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldTurnEndedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldUnitPurchasedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldUnitDamagedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldUnitHealedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldUnitTypeLockedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldUnitResurrectedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldLeaderUpgradeGrantedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldLeaderUpgradeSelectedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldUnitModifierAppliedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldUnitModifierDurationDecreasedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldUnitModifierExpiredEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldStackUnitTrainedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldUnitDisbandedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldStackAutoFittedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldUnitTransferredEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldUnitRepositionedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldItemEquippedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldItemsReceivedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldItemUnequippedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldItemUsedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldInventoryItemTransferredEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldMercenaryPurchasedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldMountainsRemovedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldMapFeatureChangedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldItemPurchasedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldMerchantItemStolenEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldItemSoldEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldSpellPurchasedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldScenarioEventTriggeringEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldScenarioEventTriggeredEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldScenarioCheckEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldScenarioPopupEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldPlayerDefeatedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldScenarioEndedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldRuinLootedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldRuinSpiedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldSiteVisitedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldFortificationEnteredEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldFortificationExitedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldSpellCastEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldSpellResearchedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldSpellsReceivedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldSpellStolenEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldSpyAddedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldSpyRemovedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldResourcesStolenEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldGameResumedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldGoldGivenEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldDiplomacyProposalSentEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldDiplomacyProposalAnsweredEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldDiplomacyItemTradedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldDiplomacySpellTradedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldAllianceStartedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldAllianceEndedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldRelationChangedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldStackPoisonedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldRodCreatedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldRodDestroyedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldRoundStartedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldRoundEndedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldAiActionStartedEvent ev, Empty context) => Empty.Value;
        public Empty Visit(WorldAiActionEndedEvent ev, Empty context) => Empty.Value;

        private static WorldPlayerId? StackOwner(WorldState state, WorldStackId stackId)
        {
            return state.Stacks.TryGetValue(stackId, out WorldStack? stack) ? stack.OwnerId : null;
        }

        private static WorldPlayerId? FortificationOwner(WorldState state, WorldFortificationId fortificationId)
        {
            if (state.Fortifications.TryGetValue(fortificationId, out WorldFortification? fortification))
            {
                return FortificationCalculator.GetOwner(fortification);
            }

            return null;
        }

        private bool IsKeyStack(WorldState state, WorldStackId stackId)
        {
            foreach (ScenarioStackReference reference in session.EngineContext.Globals.ScenarioKeyStacks)
            {
                if (ScenarioStackReferenceCalculator.IsInstance(reference, stackId, state))
                {
                    return true;
                }
            }

            return false;
        }

        private WorldBattleUnitUpgradeResolver CreateUpgradeResolver(
            WorldState state,
            WorldPlayerId? attackerOwnerId,
            WorldPlayerId? defenderOwnerId)
        {
            return new WorldBattleUnitUpgradeResolver(
                PlayerBuildings(state, attackerOwnerId),
                PlayerBuildings(state, defenderOwnerId),
                session.EngineContext);
        }

        private static WorldPlayerBuildings PlayerBuildings(WorldState state, WorldPlayerId? ownerId)
        {
            if (ownerId is null)
            {
                return new WorldPlayerBuildings([], []);
            }

            return state.Players[ownerId.Value].Buildings;
        }

        private static bool IsAiControlled(WorldState state, WorldPlayerId? ownerId)
        {
            if (ownerId is null)
            {
                return true;
            }

            WorldPlayerId id = ownerId.Value;

            return !state.Players.TryGetValue(id, out WorldPlayer? player) || player.IsAi;
        }
    }
}
