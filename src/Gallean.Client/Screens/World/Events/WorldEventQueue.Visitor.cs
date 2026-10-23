using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class WorldEventQueue
{
    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldStackMovingEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        queue.Add(new Entry.StackMove(
            step.NewState,
            ev.StackId,
            [
                new Entry.StackMove.Step(ev.SourcePosition, ev.NewDirection),
                new Entry.StackMove.Step(ev.TargetPosition, ev.NewDirection)
            ]));

        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldStackMovedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        if (ev.NewMovementPoints <= 0)
        {
            queue.Add(new Entry.StackExhaust(step.NewState, ev.StackId));
        }

        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldStackRelocatedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        if (ev.SourcePosition.IsAdjacentTo(ev.TargetPosition))
        {
            queue.Add(new Entry.StackMove(
                step.NewState,
                ev.StackId,
                [
                    new Entry.StackMove.Step(ev.SourcePosition, ev.NewDirection),
                    new Entry.StackMove.Step(ev.TargetPosition, ev.NewDirection)
                ]));
        }
        else
        {
            AddStackCreate(ev.StackId, step);
        }

        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldStackConcealedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldStackRevealedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldStackOrderChangedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldStackOwnerChangedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        WorldStack stack = step.NewState.Stacks[ev.StackId];

        if (ev.PlayAnimation)
        {
            queue.Add(new Entry.StackOwnerChange(step.NewState, stack.Position));
        }
        else
        {
            queue.Add(new Entry.Pause(step.NewState, stack.Position, null, PauseDurationDefault));
        }

        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldFogConcealedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldFogRevealedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldStackBattleStartedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        queue.Add(new Entry.Battle(step.NewState, ev.AttackerStackId, ev.Position));

        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldFortificationBattleStartedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        queue.Add(new Entry.Battle(step.NewState, ev.AttackerStackId, ev.Position));

        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldBuildingConstructedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        WorldPlan plan = WorldPlan.For(step.NewState);

        if (plan.GetCapital(ev.PlayerId) is { } capital)
        {
            queue.Add(new Entry.Pause(step.NewState, capital.Entrance, ev.PlayerId, PauseDurationDefault));
        }

        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldCityBribedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldCityRiotStartedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldCityRiotTurnsDecreasedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldCityRiotEndedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        if (step.NewState.Fortifications.TryGetValue(ev.CityId, out WorldFortification? fortification))
        {
            queue.Add(new Entry.CityRiotEnd(step.NewState, fortification.Entrance));
        }

        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldCityUpgradedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        if (step.NewState.Fortifications.GetValueOrDefault(ev.CityId) is WorldCity city)
        {
            queue.Add(new Entry.Pause(step.NewState, city.Entrance, city.OwnerId, PauseDurationDefault));
        }

        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldBagPickedUpEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        if (step.NewState.Stacks.TryGetValue(ev.StackId, out WorldStack? stack))
        {
            queue.Add(new Entry.Pause(step.NewState, stack.Position, stack.OwnerId, PauseDurationDefault));
            queue.Add(new Entry.BagPickUp(step.NewState, ev.StackId, ev.Items));
        }

        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldItemDroppedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        if (step.NewState.Stacks.TryGetValue(ev.StackId, out WorldStack? stack))
        {
            queue.Add(new Entry.Pause(step.NewState, stack.Position, stack.OwnerId, PauseDurationDefault));
        }

        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldFortificationBattleEndedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldFortificationCapturingEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        if (step.NewState.Fortifications.TryGetValue(ev.FortificationId, out WorldFortification? fortification))
        {
            queue.Add(new Entry.Pause(step.NewState, fortification.Entrance, ev.NewOwnerId, PauseDurationDefault));
        }

        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldFortificationCapturedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldFortificationPoisonedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        if (step.NewState.Fortifications.TryGetValue(ev.FortificationId, out WorldFortification? fortification))
        {
            queue.Add(new Entry.Pause(step.NewState, fortification.Entrance, null, PauseDurationDefault));
        }

        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldTurnResourcesGeneratedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldResourceNodeCapturedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        if (step.NewState.ResourceNodes.TryGetValue(ev.NodeId, out WorldResourceNode? node))
        {
            queue.Add(new Entry.Pause(step.NewState, node.Position, ev.NewOwnerId, PauseDurationDefault));
        }

        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldStackBattleEndedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldStackCreatedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        AddStackCreate(ev.Stack.Id, step);

        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldStackDestroyedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        AddStackDestroy(ev.StackId, step);

        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldStackRemovedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        if (step.OldState.Stacks.TryGetValue(ev.StackId, out WorldStack? stack))
        {
            queue.Add(new Entry.StackRemove(step.NewState, stack));
        }

        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldStackMovementConsumedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        WorldStack stack = step.OldState.Stacks[ev.StackId];

        if (stack.MovementPoints > 0 && ev.NewMovementPoints <= 0)
        {
            queue.Add(new Entry.StackExhaust(step.NewState, ev.StackId));
        }

        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldStackMovementRestoredEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldTurnStartingEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        if (step.NewState.Players[ev.PlayerId].IsAi)
        {
            return Empty.Value;
        }

        AddHotseatTurn(step.NewState, ev.PlayerId);

        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldRoundStartedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldRoundEndedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldTurnStartedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        if (step.NewState.Players[ev.PlayerId].IsAi)
        {
            return Empty.Value;
        }

        queue.Add(new Entry.TurnStart(step.NewState, ev.PlayerId));

        if (IsHotseat(step.NewState))
        {
            WorldPlan plan = WorldPlan.For(step.NewState);

            if (plan.GetCapital(ev.PlayerId) is { } capital)
            {
                queue.Add(new Entry.Pause(step.NewState, capital.Entrance, null, PauseDurationDefault));
            }
        }

        AddTurnPrompts(step.NewState, ev.PlayerId);

        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldGameResumedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        if (step.NewState.Players[ev.PlayerId].IsAi)
        {
            return Empty.Value;
        }

        AddHotseatTurn(step.NewState, ev.PlayerId);
        AddTurnPrompts(step.NewState, ev.PlayerId);

        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldTurnEndedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldUnitPurchasedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldUnitDamagedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldUnitHealedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldUnitTypeLockedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldUnitResurrectedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldLeaderUpgradeGrantedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        WorldState state = step.NewState;
        WorldPlayerId ownerId = state.Stacks[ev.StackId].OwnerId;

        if (ownerId == state.TurnInfo.CurrentPlayerId && !state.Players[ownerId].IsAi)
        {
            queue.Add(new Entry.LeaderUpgrade(state, ev.StackId));
        }

        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldLeaderUpgradeSelectedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldUnitModifierAppliedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldUnitModifierDurationDecreasedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldUnitModifierExpiredEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldStackUnitTrainedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldUnitDisbandedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldStackAutoFittedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldUnitTransferredEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldUnitRepositionedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldItemEquippedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldItemsReceivedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        queue.Add(new Entry.ItemsReceive(step.NewState, ev.PlayerId, ev.Items));

        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldItemUnequippedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldItemUsedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldInventoryItemTransferredEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldMercenaryPurchasedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldItemPurchasedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldMerchantItemStolenEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldItemSoldEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldSpellPurchasedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldScenarioEventTriggeringEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldScenarioEventTriggeredEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        ScenarioEventDefinition definition = scenario.GetEvent(ev.EventId);

        if (definition.Effects.Any(effect => effect is ScenarioEventChangeObjectiveEffect) &&
            step.NewState.ScenarioState.ObjectiveTextId is { } textId)
        {
            queue.Add(new Entry.ObjectiveChange(step.NewState, textId));
        }

        foreach (ScenarioEventEffect effect in definition.Effects)
        {
            if (effect is ScenarioEventUnfogEffect { Reveal: true } unfog)
            {
                ScenarioLocationDefinition location = scenario.GetLocation(unfog.Location);

                foreach (WorldPlayerId playerId in unfog.Players)
                {
                    queue.Add(new Entry.Unfog(step.NewState, location.Position, playerId));
                }
            }
        }

        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldScenarioCheckEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldScenarioPopupEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        WorldPosition? focusPosition = ResolveScenarioPopupFocusPosition(ev, step.NewState);

        if (focusPosition is not null)
        {
            queue.Add(new Entry.Pause(step.NewState, focusPosition.Value, null, PauseDurationScenarioPopup));
        }

        queue.Add(new Entry.ScenarioPopup(step.NewState, [ev]));

        return Empty.Value;
    }

    private static WorldPosition? ResolveScenarioPopupFocusPosition(WorldScenarioPopupEvent ev, WorldState state)
    {
        if (ev.TriggererPosition is not null)
        {
            return ev.TriggererPosition;
        }
        else if (ev.Audience.Contains(state.TurnInfo.CurrentPlayerId))
        {
            WorldPlan plan = WorldPlan.For(state);
            WorldCapital? capital = plan.GetCapital(state.TurnInfo.CurrentPlayerId);

            return capital?.Entrance;
        }
        else
        {
            return null;
        }
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldScenarioEndedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        queue.Add(new Entry.ScenarioEnd(step.NewState, ev.WinnerId));

        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldSpellCastEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        if (ev.Target is not null)
        {
            queue.Add(new Entry.SpellCast(step.NewState, ev.CasterId, ev.SpellId, ev.Target.Center, SummonedStackId: null));
        }
        else if (ev.CasterId is not null)
        {
            WorldPlan plan = WorldPlan.For(step.NewState);
            WorldCapital? capital = plan.GetCapital(ev.CasterId.Value);

            if (capital is not null)
            {
                queue.Add(new Entry.GlobalSpellCast(step.NewState, ev.SpellId, capital.Entrance));
            }
        }

        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldSpellResearchedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldSpellsReceivedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        queue.Add(new Entry.SpellsReceive(step.NewState, ev.PlayerId, ev.Spells));

        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldSpellStolenEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldSpyAddedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldSpyRemovedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldRuinSpiedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldSiteVisitedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldFortificationEnteredEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        if (step.NewState.Fortifications.TryGetValue(ev.FortificationId, out WorldFortification? fortification))
        {
            queue.Add(new Entry.FortificationEnter(step.NewState, fortification.Entrance));
        }

        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldFortificationExitedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        if (step.NewState.Fortifications.TryGetValue(ev.FortificationId, out WorldFortification? fortification))
        {
            queue.Add(new Entry.FortificationExit(step.NewState, fortification.Entrance, ev.StackId));
        }

        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldRuinLootedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        if (step.NewState.Fortifications.TryGetValue(ev.RuinId, out WorldFortification? fortification))
        {
            queue.Add(new Entry.RuinLoot(step.NewState, fortification.Entrance, ev.StackId, ev.Resources, ev.Item));
        }

        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldResourcesStolenEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldGoldGivenEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldDiplomacyProposalSentEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldDiplomacyProposalAnsweredEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldDiplomacyItemTradedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldDiplomacySpellTradedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldAllianceStartedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldAllianceEndedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldRelationChangedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldStackPoisonedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        if (step.NewState.Stacks.TryGetValue(ev.StackId, out WorldStack? stack))
        {
            queue.Add(new Entry.Pause(step.NewState, stack.Position, null, PauseDurationDefault));
        }

        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldUnitsShuffledEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldThiefActionSucceededEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        if (step.NewState.Stacks.TryGetValue(ev.ThiefStackId, out WorldStack? thief))
        {
            queue.Add(new Entry.ThiefActionResult(step.NewState, thief.Position, thief.OwnerId, ev.Action, IsSuccess: true));
        }

        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldThiefActionFailedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        if (step.NewState.Stacks.TryGetValue(ev.ThiefStackId, out WorldStack? thief))
        {
            queue.Add(new Entry.ThiefActionResult(step.NewState, thief.Position, thief.OwnerId, ev.Action, IsSuccess: false));
        }

        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldRodCreatedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        queue.Add(new Entry.Pause(step.NewState, ev.Position, ev.PlayerId, PauseDurationDefault));

        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldRodDestroyedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        queue.Add(new Entry.Pause(step.NewState, ev.Position, ev.PlayerId, PauseDurationDefault));

        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldMountainsRemovedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        ScenarioLocationDefinition location = scenario.GetLocation(ev.Location);

        queue.Add(new Entry.MountainRemove(step.NewState, location.Position, location.Radius));

        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldMapFeatureChangedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldPlayerDefeatedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldAiActionStartedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    Empty IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>.Visit(WorldAiActionEndedEvent ev, EngineStep<WorldState, WorldEvent> step)
    {
        return Empty.Value;
    }

    private static bool IsHotseat(WorldState state)
    {
        WorldPlan plan = WorldPlan.For(state);

        int humanPlayers = 0;

        foreach (WorldPlayer player in state.Players.Values)
        {
            if (!player.IsAi && !plan.IsPlayerDefeated(player.Id))
            {
                humanPlayers++;
            }
        }

        return humanPlayers > 1;
    }

    private void AddHotseatTurn(WorldState state, WorldPlayerId playerId)
    {
        if (IsHotseat(state))
        {
            queue.Add(new Entry.HotseatTurn(state, playerId));
        }
    }

    private void AddTurnPrompts(WorldState state, WorldPlayerId playerId)
    {
        foreach (WorldStack stack in state.Stacks.Values)
        {
            if (stack.OwnerId == playerId && stack.PendingLeaderUpgrades > 0)
            {
                queue.Add(new Entry.LeaderUpgrade(state, stack.Id));
            }
        }

        foreach (WorldDiplomacyProposal proposal in state.Players[playerId].IncomingDiplomacyProposals)
        {
            queue.Add(new Entry.DiplomacyProposal(state, proposal));
        }
    }

    private void AddStackCreate(WorldStackId stackId, EngineStep<WorldState, WorldEvent> step)
    {
        if (step.NewState.Stacks.TryGetValue(stackId, out WorldStack? stack))
        {
            WorldPlan plan = WorldPlan.For(step.NewState);

            if (plan.GetFortificationAt(stack.Position) is not WorldCapital)
            {
                queue.Add(new Entry.StackCreate(step.NewState, stack));
            }
        }
    }

    private void AddStackDestroy(WorldStackId stackId, EngineStep<WorldState, WorldEvent> step)
    {
        if (step.OldState.Stacks.TryGetValue(stackId, out WorldStack? stack))
        {
            queue.Add(new Entry.StackDestroy(step.NewState, stack));
        }
    }
}
