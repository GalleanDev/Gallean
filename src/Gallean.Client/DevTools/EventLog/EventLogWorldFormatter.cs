using System.Collections.Immutable;
using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed class EventLogWorldFormatter(IGameDefinitionProvider definitions, IGameTextProvider texts) :
    IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>,
    IWorldDiplomacyProposalActionVisitor<Empty, ImmutableArray<EventLogSpan>>
{
    public IReadOnlyList<EventLogEntry> Format(WorldEvent ev, WorldState state)
    {
        return ev.Accept(this, state);
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldBuildingConstructedEvent ev, WorldState state)
    {
        return AddEvent($"{Player(ev.PlayerId)} constructed {Building(ev.BuildingId)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldCityBribedEvent ev, WorldState state)
    {
        return AddEvent($"Stack {Reference(ev.StackId.Value)} bribed city {Reference(ev.CityId.Value)} for {Gold(ev.Cost.Gold)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldCityRiotStartedEvent ev, WorldState state)
    {
        return AddEvent($"A riot has been provoked inside city {Reference(ev.CityId.Value)} for {Value(ev.Turns)} turns.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldCityRiotTurnsDecreasedEvent ev, WorldState state)
    {
        return AddEvent($"The riot in city {Reference(ev.CityId.Value)} has {Value(ev.Turns)} turns left.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldCityRiotEndedEvent ev, WorldState state)
    {
        return AddEvent($"The riot in city {Reference(ev.CityId.Value)} has ended.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldCityUpgradedEvent ev, WorldState state)
    {
        return AddEvent($"City {Reference(ev.CityId.Value)} was upgraded to level {Value(ev.DefinitionId.Level)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldBagPickedUpEvent ev, WorldState state)
    {
        return AddEvent($"Stack {Reference(ev.StackId.Value)} picked up bag {Reference(ev.BagId.Value)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldItemDroppedEvent ev, WorldState state)
    {
        return AddEvent($"Stack {Reference(ev.StackId.Value)} dropped {Item(ev.Item.DefinitionId)} into bag {Reference(ev.BagId.Value)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldFortificationBattleStartedEvent ev, WorldState state)
    {
        return AddEvent($"Stack {Reference(ev.AttackerStackId.Value)} attacked fortification {Reference(ev.DefenderFortificationId.Value)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldFortificationBattleEndedEvent ev, WorldState state)
    {
        return AddEvent($"Battle for fortification {Reference(ev.DefenderFortificationId.Value)} ended. {Side(ev.WinningSide)} won.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldFortificationCapturingEvent ev, WorldState state)
    {
        return AddEvent($"Fortification {Reference(ev.FortificationId.Value)} captured by {Player(ev.NewOwnerId)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldFortificationCapturedEvent ev, WorldState state)
    {
        return [];
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldFortificationPoisonedEvent ev, WorldState state)
    {
        return AddEvent($"A thief poisoned fortification {Reference(ev.FortificationId.Value)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldTurnResourcesGeneratedEvent ev, WorldState state)
    {
        if (ev.Resources == WorldResourceSet.Empty)
        {
            return [];
        }
        else
        {
            return AddEvent($"{Player(ev.PlayerId)} received {Resources(ev.Resources)}.");
        }
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldResourceNodeCapturedEvent ev, WorldState state)
    {
        return AddEvent($"Resource node {Reference(ev.NodeId.Value)} captured by {Player(ev.NewOwnerId)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldStackBattleStartedEvent ev, WorldState state)
    {
        return AddEvent($"Stack {Reference(ev.AttackerStackId.Value)} attacked stack {Reference(ev.DefenderStackId.Value)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldStackBattleEndedEvent ev, WorldState state)
    {
        return AddEvent($"Battle between stacks {Reference(ev.AttackerStackId.Value)} and {Reference(ev.DefenderStackId.Value)} ended. {Side(ev.WinningSide)} won.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldStackCreatedEvent ev, WorldState state)
    {
        ImmutableArray<EventLogSpan> units = Join(ev.Stack.Units.GetUnits().Select(entry => Unit(entry.Unit)));

        return AddEvent($"{Player(ev.Stack.OwnerId)} received {units} at {Position(ev.Stack.Position)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldStackDestroyedEvent ev, WorldState state)
    {
        return AddEvent($"Stack {Reference(ev.StackId.Value)} was destroyed.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldStackRemovedEvent ev, WorldState state)
    {
        return AddEvent($"Stack {Reference(ev.StackId.Value)} was removed from the map.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldStackMovingEvent ev, WorldState state)
    {
        return AddEvent($"Stack {Reference(ev.StackId.Value)} moved to {Position(ev.TargetPosition)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldStackMovedEvent ev, WorldState state)
    {
        return [];
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldStackRelocatedEvent ev, WorldState state)
    {
        return AddEvent($"Stack {Reference(ev.StackId.Value)} was relocated to {Position(ev.TargetPosition)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldStackConcealedEvent ev, WorldState state)
    {
        return AddEvent($"Stack {Reference(ev.StackId.Value)} became invisible.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldStackRevealedEvent ev, WorldState state)
    {
        return AddEvent($"Stack {Reference(ev.StackId.Value)} is no longer invisible.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldStackOrderChangedEvent ev, WorldState state)
    {
        return AddEvent($"Stack {Reference(ev.StackId.Value)} changed its order.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldStackOwnerChangedEvent ev, WorldState state)
    {
        return AddEvent($"Stack {Reference(ev.StackId.Value)} now belongs to {Player(ev.OwnerId)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldFogConcealedEvent ev, WorldState state)
    {
        return AddEvent($"{Player(ev.PlayerId)} lost sight of {Value(ev.Positions.Length)} tiles.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldFogRevealedEvent ev, WorldState state)
    {
        return [];
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldStackMovementConsumedEvent ev, WorldState state)
    {
        return AddEvent($"Stack {Reference(ev.StackId.Value)} has {Value(ev.NewMovementPoints)} movement left.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldStackMovementRestoredEvent ev, WorldState state)
    {
        return AddEvent($"Stack {Reference(ev.StackId.Value)} movement restored to {Value(ev.NewMovementPoints)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldTurnStartingEvent ev, WorldState state)
    {
        return [];
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldTurnStartedEvent ev, WorldState state)
    {
        return AddEvent($"{Player(ev.PlayerId)} turn started.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldTurnEndedEvent ev, WorldState state)
    {
        return AddEvent($"{Player(ev.PlayerId)} ended the turn.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldUnitPurchasedEvent ev, WorldState state)
    {
        return AddEvent($"{Unit(ev.Unit.Name, ev.Unit.DefinitionId)} purchased in fortification {Reference(ev.FortificationId.Value)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldUnitDamagedEvent ev, WorldState state)
    {
        return AddEvent($"{Unit(state, ev.Group, ev.UnitId)} in {UnitGroup(ev.Group)} damaged to {Damage(ev.Hitpoints)} HP.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldUnitHealedEvent ev, WorldState state)
    {
        return AddEvent($"{Unit(state, ev.Group, ev.UnitId)} in {UnitGroup(ev.Group)} healed to {Healing(ev.Hitpoints)} HP.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldUnitTypeLockedEvent ev, WorldState state)
    {
        return AddEvent($"{Unit(state, ev.Group, ev.UnitId)} in {UnitGroup(ev.Group)} type locked.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldStackUnitTrainedEvent ev, WorldState state)
    {
        return AddEvent($"{Unit(state, new WorldUnitGroup.Stack(ev.StackId), ev.UnitId)} in stack {Reference(ev.StackId.Value)} trained to {Value(ev.Experience)} XP.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldUnitResurrectedEvent ev, WorldState state)
    {
        return AddEvent($"{Unit(state, ev.Group, ev.UnitId)} in {UnitGroup(ev.Group)} resurrected with {Healing(ev.Hitpoints)} HP.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldLeaderUpgradeGrantedEvent ev, WorldState state)
    {
        return AddEvent($"The leader of stack {Reference(ev.StackId.Value)} can learn a new ability.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldLeaderUpgradeSelectedEvent ev, WorldState state)
    {
        return AddEvent($"The leader of stack {Reference(ev.StackId.Value)} learned a new ability.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldUnitDisbandedEvent ev, WorldState state)
    {
        return AddEvent($"{Unit(state, ev.Group, ev.UnitId)} in {UnitGroup(ev.Group)} disbanded.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldStackAutoFittedEvent ev, WorldState state)
    {
        return AddEvent($"Stack {Reference(ev.StackId.Value)} auto-fitted.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldUnitTransferredEvent ev, WorldState state)
    {
        return AddEvent($"A unit transferred from {UnitGroup(ev.Source)} to {UnitGroup(ev.Target)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldUnitRepositionedEvent ev, WorldState state)
    {
        return AddEvent($"Unit repositioned in {UnitGroup(ev.Group)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldItemEquippedEvent ev, WorldState state)
    {
        return AddEvent($"Stack {Reference(ev.StackId.Value)} equipped {Item(ev.Item.DefinitionId)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldItemsReceivedEvent ev, WorldState state)
    {
        ImmutableArray<EventLogSpan> items = Join(ev.Items.Select(item => Item(item.DefinitionId)));

        return AddEvent($"{Player(ev.PlayerId)} received {items} in {UnitGroup(ev.Target)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldItemUnequippedEvent ev, WorldState state)
    {
        return AddEvent($"Stack {Reference(ev.StackId.Value)} unequipped {Slot(ev.Slot)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldItemUsedEvent ev, WorldState state)
    {
        if (ev.Consumed)
        {
            return AddEvent($"Stack {Reference(ev.StackId.Value)} consumed {Item(ev.Item.DefinitionId)}.");
        }
        else if (ev.RemainingCharges is not null)
        {
            return AddEvent($"Stack {Reference(ev.StackId.Value)} used {Item(ev.Item.DefinitionId)}, {Value(ev.RemainingCharges.Value)} charges left.");
        }
        else
        {
            return AddEvent($"Stack {Reference(ev.StackId.Value)} used {Item(ev.Item.DefinitionId)}.");
        }
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldUnitModifierAppliedEvent ev, WorldState state)
    {
        return AddEvent($"{Unit(state, ev.Group, ev.UnitId)} in {UnitGroup(ev.Group)} received modifier {Modifier(ev.Modifier.DefinitionId)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldUnitModifierDurationDecreasedEvent ev, WorldState state)
    {
        return [];
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldUnitModifierExpiredEvent ev, WorldState state)
    {
        return AddEvent($"{Unit(state, ev.Group, ev.UnitId)} in {UnitGroup(ev.Group)} lost modifier {Modifier(ev.ModifierId)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldInventoryItemTransferredEvent ev, WorldState state)
    {
        return AddEvent($"{Item(ev.Item.DefinitionId)} transferred from {UnitGroup(ev.Source)} to {UnitGroup(ev.Target)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldMercenaryPurchasedEvent ev, WorldState state)
    {
        return AddEvent($"{Unit(ev.Unit.Name, ev.Unit.DefinitionId)} hired by stack {Reference(ev.StackId.Value)} at mercenary camp {Reference(ev.SiteId.Value)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldMountainsRemovedEvent ev, WorldState state)
    {
        return AddEvent($"Mountains removed around location {Reference(ev.Location.Value)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldMapFeatureChangedEvent ev, WorldState state)
    {
        return AddEvent($"{Value(ev.Positions.Length)} tiles changed.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldItemPurchasedEvent ev, WorldState state)
    {
        return AddEvent($"Stack {Reference(ev.StackId.Value)} bought {Item(ev.Item.DefinitionId)} at merchant {Reference(ev.SiteId.Value)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldMerchantItemStolenEvent ev, WorldState state)
    {
        return AddEvent($"Stack {Reference(ev.StackId.Value)} stole {Item(ev.Item.DefinitionId)} from merchant {Reference(ev.SiteId.Value)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldItemSoldEvent ev, WorldState state)
    {
        return AddEvent($"Stack {Reference(ev.StackId.Value)} sold {Item(ev.Item.DefinitionId)} at merchant {Reference(ev.SiteId.Value)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldSpellPurchasedEvent ev, WorldState state)
    {
        return AddEvent($"{Player(ev.PlayerId)} learned {Spell(ev.SpellId)} at mage tower {Reference(ev.SiteId.Value)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldScenarioEventTriggeringEvent ev, WorldState state)
    {
        return [];
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldScenarioEventTriggeredEvent ev, WorldState state)
    {
        return [new EventLogEntry(EventLogEntryType.World, Spans($"Scenario event {Reference(ev.EventId.Value)} triggered."), ev.EventId)];
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldScenarioCheckEvent ev, WorldState state)
    {
        return [];
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldScenarioPopupEvent ev, WorldState state)
    {
        string text = texts.GetScenarioText(ev.TextId);

        return [new EventLogEntry(EventLogEntryType.ScenarioPopup, [new EventLogSpan(text, EventLogSpanType.Text)], ev.EventId)];
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldPlayerDefeatedEvent ev, WorldState state)
    {
        return AddEvent($"{Player(ev.PlayerId)} was defeated.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldScenarioEndedEvent ev, WorldState state)
    {
        if (ev.WinnerId is not null)
        {
            return AddEvent($"Scenario ended. {Player(ev.WinnerId.Value)} won.");
        }
        else
        {
            return AddEvent($"Scenario ended.");
        }
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldSpellCastEvent ev, WorldState state)
    {
        if (ev.CasterId is not null)
        {
            return AddEvent($"{Player(ev.CasterId.Value)} cast {Spell(ev.SpellId)}.");
        }
        else
        {
            return AddEvent($"{Spell(ev.SpellId)} was cast.");
        }
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldSpellResearchedEvent ev, WorldState state)
    {
        return AddEvent($"{Player(ev.PlayerId)} researched {Spell(ev.SpellId)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldSpellsReceivedEvent ev, WorldState state)
    {
        ImmutableArray<EventLogSpan> spells = Join(ev.Spells.Select(Spell));

        return AddEvent($"{Player(ev.PlayerId)} received {spells}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldResourcesStolenEvent ev, WorldState state)
    {
        return AddEvent($"Stack {Reference(ev.StackId.Value)} stole {Resources(ev.Resources)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldGameResumedEvent ev, WorldState state)
    {
        return [];
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldGoldGivenEvent ev, WorldState state)
    {
        return AddEvent($"{Player(ev.PlayerId)} gave {Gold(ev.Gold)} to {Player(ev.TargetPlayerId)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldDiplomacyProposalSentEvent ev, WorldState state)
    {
        WorldDiplomacyProposal proposal = ev.Proposal;
        ImmutableArray<EventLogSpan> offer = proposal.Action.AcceptProposal(this, Empty.Value);

        return AddEvent($"{Player(proposal.PlayerId)} offered {offer} to {Player(proposal.TargetPlayerId)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldDiplomacyProposalAnsweredEvent ev, WorldState state)
    {
        WorldDiplomacyProposal proposal = ev.Proposal;
        ImmutableArray<EventLogSpan> offer = proposal.Action.AcceptProposal(this, Empty.Value);

        if (ev.Accepted)
        {
            return AddEvent($"{Player(proposal.TargetPlayerId)} accepted {offer} from {Player(proposal.PlayerId)}.");
        }
        else
        {
            return AddEvent($"{Player(proposal.TargetPlayerId)} declined {offer} from {Player(proposal.PlayerId)}.");
        }
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldDiplomacyItemTradedEvent ev, WorldState state)
    {
        return AddEvent($"{Player(ev.SellerId)} sold {Item(ev.Item.DefinitionId)} to {Player(ev.BuyerId)} for {Gold(ev.Price)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldDiplomacySpellTradedEvent ev, WorldState state)
    {
        return AddEvent($"{Player(ev.SellerId)} sold {Spell(ev.SpellId)} to {Player(ev.BuyerId)} for {Gold(ev.Price)}.");
    }

    ImmutableArray<EventLogSpan> IWorldDiplomacyProposalActionVisitor<Empty, ImmutableArray<EventLogSpan>>.Visit(WorldDiplomacyTradeItemAction action, Empty context)
    {
        return Spans($"{Item(action.ItemId)} for {Gold(action.Price)}");
    }

    ImmutableArray<EventLogSpan> IWorldDiplomacyProposalActionVisitor<Empty, ImmutableArray<EventLogSpan>>.Visit(WorldDiplomacyTradeSpellAction action, Empty context)
    {
        return Spans($"{Spell(action.SpellId)} for {Gold(action.Price)}");
    }

    ImmutableArray<EventLogSpan> IWorldDiplomacyProposalActionVisitor<Empty, ImmutableArray<EventLogSpan>>.Visit(WorldDiplomacyProposeAllianceAction action, Empty context)
    {
        return Spans($"an alliance");
    }

    ImmutableArray<EventLogSpan> IWorldDiplomacyProposalActionVisitor<Empty, ImmutableArray<EventLogSpan>>.Visit(WorldDiplomacyExchangeMapAction action, Empty context)
    {
        return Spans($"a fog vision exchange");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldAllianceStartedEvent ev, WorldState state)
    {
        return AddEvent($"{Player(ev.PlayerId)} allied with {Player(ev.TargetPlayerId)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldAllianceEndedEvent ev, WorldState state)
    {
        return AddEvent($"{Player(ev.PlayerId)} broke the alliance with {Player(ev.TargetPlayerId)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldRelationChangedEvent ev, WorldState state)
    {
        return AddEvent($"Relations between {Player(ev.PlayerId)} and {Player(ev.TargetPlayerId)} changed to {Value(ev.Relation)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldSpellStolenEvent ev, WorldState state)
    {
        return AddEvent($"Stack {Reference(ev.StackId.Value)} stole {Spell(ev.SpellId)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldSpyAddedEvent ev, WorldState state)
    {
        return AddEvent($"{Player(ev.PlayerId)} now has a spy in the army of {Player(ev.TargetPlayerId)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldSpyRemovedEvent ev, WorldState state)
    {
        return AddEvent($"{Player(ev.PlayerId)} no longer has a spy in the army of {Player(ev.TargetPlayerId)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldRuinSpiedEvent ev, WorldState state)
    {
        return AddEvent($"{Player(ev.PlayerId)} spied upon a ruin.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldSiteVisitedEvent ev, WorldState state)
    {
        return AddEvent($"Stack {Reference(ev.StackId.Value)} visited site {Reference(ev.SiteId.Value)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldFortificationEnteredEvent ev, WorldState state)
    {
        return AddEvent($"Stack {Reference(ev.StackId.Value)} entered {Reference(ev.FortificationId.Value)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldFortificationExitedEvent ev, WorldState state)
    {
        return AddEvent($"Stack {Reference(ev.StackId.Value)} exited {Reference(ev.FortificationId.Value)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldRuinLootedEvent ev, WorldState state)
    {
        if (ev.Item is not null && ev.Resources != WorldResourceSet.Empty)
        {
            return AddEvent($"Stack {Reference(ev.StackId.Value)} looted {Resources(ev.Resources)} and {Item(ev.Item.DefinitionId)} from ruin {Reference(ev.RuinId.Value)}.");
        }
        else if (ev.Item is not null)
        {
            return AddEvent($"Stack {Reference(ev.StackId.Value)} looted {Item(ev.Item.DefinitionId)} from ruin {Reference(ev.RuinId.Value)}.");
        }
        else
        {
            return AddEvent($"Stack {Reference(ev.StackId.Value)} looted {Resources(ev.Resources)} from ruin {Reference(ev.RuinId.Value)}.");
        }
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldStackPoisonedEvent ev, WorldState state)
    {
        return AddEvent($"A thief poisoned stack {Reference(ev.StackId.Value)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldUnitsShuffledEvent ev, WorldState state)
    {
        return AddEvent($"A thief shuffled the units of {UnitGroup(ev.Group)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldThiefActionSucceededEvent ev, WorldState state)
    {
        return AddEvent($"Thief {Reference(ev.ThiefStackId.Value)} performed {ThiefAction(ev.Action)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldThiefActionFailedEvent ev, WorldState state)
    {
        return AddEvent($"Thief {Reference(ev.ThiefStackId.Value)} was captured attempting {ThiefAction(ev.Action)}.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldRodCreatedEvent ev, WorldState state)
    {
        return AddEvent($"{Player(ev.PlayerId)} planted a rod.");
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldRodDestroyedEvent ev, WorldState state)
    {
        if (ev.PlayerId is not null)
        {
            return AddEvent($"{Player(ev.PlayerId.Value)} destroyed a rod of {Player(ev.TargetPlayerId)}.");
        }
        else
        {
            return AddEvent($"A rod of {Player(ev.TargetPlayerId)} was destroyed.");
        }
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldRoundStartedEvent ev, WorldState state)
    {
        return [];
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldRoundEndedEvent ev, WorldState state)
    {
        return [];
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldAiActionStartedEvent ev, WorldState state)
    {
        return [];
    }

    IReadOnlyList<EventLogEntry> IWorldEventVisitor<WorldState, IReadOnlyList<EventLogEntry>>.Visit(WorldAiActionEndedEvent ev, WorldState state)
    {
        return [];
    }

    private static ImmutableArray<EventLogSpan> UnitGroup(WorldUnitGroup target)
    {
        return target switch
        {
            WorldUnitGroup.Stack data => Spans($"stack {Reference(data.StackId.Value)}"),
            WorldUnitGroup.Fortification data => Spans($"garrison {Reference(data.FortificationId.Value)}"),
            _ => throw new ArgumentOutOfRangeException(nameof(target))
        };
    }

    private static IReadOnlyList<EventLogEntry> AddEvent(EventLogInterpolatedStringHandler text)
    {
        return [new EventLogEntry(EventLogEntryType.World, text.ToImmutable(), null)];
    }

    private static ImmutableArray<EventLogSpan> Spans(EventLogInterpolatedStringHandler text)
    {
        return text.ToImmutable();
    }

    private static ImmutableArray<EventLogSpan> Join(IEnumerable<EventLogSpan> spans)
    {
        ImmutableArray<EventLogSpan>.Builder joined = ImmutableArray.CreateBuilder<EventLogSpan>();

        foreach (EventLogSpan span in spans)
        {
            if (joined.Count > 0)
            {
                joined.Add(new EventLogSpan(", ", EventLogSpanType.Text));
            }

            joined.Add(span);
        }

        return joined.ToImmutable();
    }

    private EventLogSpan Unit(WorldUnit unit)
    {
        string name = UnitNameResolver.Resolve(unit, definitions, texts);

        return new EventLogSpan(name, EventLogSpanType.Actor);
    }

    private EventLogSpan Unit(string? name, UnitDefinitionId id)
    {
        if (name is not null)
        {
            return new EventLogSpan(name, EventLogSpanType.Actor);
        }
        else
        {
            UnitDefinition unit = definitions.GetUnit(id);

            return new EventLogSpan(Text(unit.NameTextId), EventLogSpanType.Actor);
        }
    }

    private EventLogSpan Building(WorldPlayerBuildingDefinitionId id)
    {
        WorldPlayerBuildingDefinition building = definitions.GetPlayerBuilding(id);

        return new EventLogSpan(Text(building.NameTextId), EventLogSpanType.Object);
    }

    private EventLogSpan Item(ItemDefinitionId id)
    {
        ItemDefinition item = definitions.GetItem(id);

        return new EventLogSpan(Text(item.NameTextId), EventLogSpanType.Object);
    }

    private EventLogSpan Spell(WorldSpellDefinitionId id)
    {
        WorldSpellDefinition spell = definitions.GetSpell(id);

        return new EventLogSpan(Text(spell.NameTextId), EventLogSpanType.Object);
    }

    private string Text(GlobalTextId id)
    {
        return texts.GetGlobalText(id).Text;
    }

    private static EventLogSpan Player(WorldPlayerId playerId)
    {
        return new EventLogSpan($"Player {playerId.Value}", EventLogSpanType.Actor);
    }

    private static EventLogSpan Side(BattleSide side)
    {
        return side switch
        {
            BattleSide.Attacker => new EventLogSpan("Attacker", EventLogSpanType.Actor),
            BattleSide.Defender => new EventLogSpan("Defender", EventLogSpanType.Actor)
        };
    }

    private static EventLogSpan Reference(string id)
    {
        return new EventLogSpan(id, EventLogSpanType.Reference);
    }

    private static EventLogSpan Position(WorldPosition position)
    {
        return new EventLogSpan($"{position.X},{position.Y}", EventLogSpanType.Reference);
    }

    private EventLogSpan Unit(WorldState state, WorldUnitGroup group, WorldUnitId unitId)
    {
        UnitGrid<WorldUnit> units = group switch
        {
            WorldUnitGroup.Stack data => state.Stacks[data.StackId].Units,
            WorldUnitGroup.Fortification data => state.Fortifications[data.FortificationId].Garrison,
            _ => throw new ArgumentOutOfRangeException(nameof(group), group, null)
        };

        return Unit(units.GetUnit(unitId));
    }

    private EventLogSpan Modifier(UnitModifierDefinitionId id)
    {
        UnitModifierDefinition modifier = definitions.GetUnitModifier(id);

        return new EventLogSpan(Text(modifier.DescriptionTextId), EventLogSpanType.Object);
    }

    private EventLogSpan ThiefAction(WorldThiefActionType type)
    {
        WorldThiefActionDefinition action = definitions.GetThiefAction(type);

        return new EventLogSpan(Text(action.NameTextId), EventLogSpanType.Object);
    }

    private static EventLogSpan Slot(LeaderEquipmentSlot slot)
    {
        return new EventLogSpan(LeaderEquipmentSlotFormatter.FormatName(slot), EventLogSpanType.Object);
    }

    private static EventLogSpan Value(int value)
    {
        return new EventLogSpan(value.ToString(), EventLogSpanType.Value);
    }

    private static EventLogSpan Damage(int hitpoints)
    {
        return new EventLogSpan(hitpoints.ToString(), EventLogSpanType.Damage);
    }

    private static EventLogSpan Healing(int hitpoints)
    {
        return new EventLogSpan(hitpoints.ToString(), EventLogSpanType.Healing);
    }

    private static EventLogSpan Gold(int amount)
    {
        return new EventLogSpan($"{amount} gold", EventLogSpanType.Value);
    }

    private static EventLogSpan Resources(WorldResourceSet resources)
    {
        if (resources != WorldResourceSet.Empty)
        {
            return new EventLogSpan(WorldResourceFormatter.Format(resources), EventLogSpanType.Value);
        }
        else
        {
            return new EventLogSpan("nothing", EventLogSpanType.Text);
        }
    }
}
