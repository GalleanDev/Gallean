using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    private readonly record struct ScenarioEventEffectContext(
        WorldScenarioEventTriggeringEvent TriggeredEvent,
        WorldState State);

    private readonly record struct ScenarioEventEffectResult(
        WorldState State,
        IReadOnlyList<WorldEvent> Events);

    private sealed record ScenarioStackDestination(WorldPosition Position, WorldPath? Path);

    private sealed class ScenarioEventTriggeringEffectRule(IEngineContext context) :
        WorldEventRule(context),
        IScenarioEventEffectVisitor<ScenarioEventEffectContext, ScenarioEventEffectResult>
    {
        private static ScenarioEventEffectResult Empty(WorldState state)
        {
            return new ScenarioEventEffectResult(state, []);
        }

        public ScenarioEventEffectResult Visit(ScenarioEventPopupEffect effect, ScenarioEventEffectContext context)
        {
            ScenarioEventDefinition definition = Context.Scenario.GetEvent(context.TriggeredEvent.EventId);
            WorldStack? stack = ScenarioEventTriggererCalculator.FindStack(context.TriggeredEvent.Trigger, context.State);

            ScenarioEventPortraitId? portraitId;
            string? portraitName;

            if (effect.IsPortraitTriggerer && stack is not null)
            {
                (WorldUnit leader, GridPosition _, UnitDefinition _) = stack.GetLeader(Context.Definitions);
                portraitId = ScenarioEventPortraitId.Unit(leader.DefinitionId);
                portraitName = leader.Name;
            }
            else
            {
                portraitId = effect.PortraitId;
                portraitName = null;
            }

            ImmutableArray<WorldPlayerId> audience = ScenarioEventPopupCalculator.ResolveAudience(
                definition,
                effect,
                context.TriggeredEvent.TriggererId,
                context.State);

            WorldScenarioPopupEvent popupEvent = new(
                context.TriggeredEvent.EventId,
                effect.TextId,
                portraitId,
                portraitName,
                effect.IsPortraitLeft,
                context.TriggeredEvent.TriggererId,
                stack?.Position,
                audience);

            return new ScenarioEventEffectResult(context.State, [popupEvent]);
        }

        public ScenarioEventEffectResult Visit(ScenarioEventWinEffect effect, ScenarioEventEffectContext context)
        {
            if (context.State.ScenarioState.IsEnded)
            {
                return Empty(context.State);
            }

            WorldState state = context.State with
            {
                ScenarioState = context.State.ScenarioState with
                {
                    IsEnded = true,
                    Winner = effect.WinnerId
                }
            };

            return new ScenarioEventEffectResult(state, [new WorldScenarioEndedEvent(effect.WinnerId)]);
        }

        public ScenarioEventEffectResult Visit(ScenarioEventMoveStackEffect effect, ScenarioEventEffectContext context)
        {
            ImmutableArray<WorldStackId> instances = ScenarioStackReferenceCalculator.GetLiveInstances(effect.Stack, context.State);
            if (instances.IsEmpty)
            {
                return Empty(context.State);
            }

            WorldStack stack = context.State.Stacks[instances[0]];
            ScenarioLocationDefinition location = Context.Scenario.GetLocation(effect.Location);

            if (ScenarioLocationCalculator.Contains(location, stack.Position))
            {
                return Empty(context.State);
            }

            WorldPlan plan = WorldPlan.For(context.State);
            WorldMovementCalculator.MovementProfile profile = WorldMovementCalculator.GetMovementProfile(stack, Context.Definitions);
            WorldPosition[] candidates =
            [
                location.Position,
                .. ScenarioLocationCalculator.EnumerateTiles(location, context.State.Map)
            ];

            foreach (WorldPosition candidate in candidates)
            {
                if (plan.CanPlaceStackAt(candidate, profile))
                {
                    WorldPath? path = plan.GetPath(stack.Position, candidate, fogOfWar: null, profile, WorldMovementLimit.Unlimited);

                    return Relocate(stack, path, candidate, finalDirection: null, context.State);
                }
            }

            return Empty(context.State);
        }

        public ScenarioEventEffectResult Visit(ScenarioEventMoveBesideEffect effect, ScenarioEventEffectContext context)
        {
            WorldStack? triggerer = ScenarioEventTriggererCalculator.FindStack(context.TriggeredEvent.Trigger, context.State);
            if (triggerer is null)
            {
                return Empty(context.State);
            }

            WorldStack? stack = GetSubjectStack(effect.Stack, triggerer, context.State);

            if (stack is null)
            {
                return Empty(context.State);
            }

            if (stack.Position.GetDistance(triggerer.Position) <= 1)
            {
                return Empty(context.State);
            }

            if (TryMoveBeside(stack, triggerer, context.State, out ScenarioEventEffectResult result))
            {
                return result;
            }
            else
            {
                return Empty(context.State);
            }
        }

        public ScenarioEventEffectResult Visit(ScenarioEventBattleEffect effect, ScenarioEventEffectContext context)
        {
            WorldStack? triggerer = ScenarioEventTriggererCalculator.FindStack(context.TriggeredEvent.Trigger, context.State);
            if (triggerer is null)
            {
                return Empty(context.State);
            }

            WorldStack? attacker = GetSubjectStack(effect.Stack, triggerer, context.State);
            if (attacker is null)
            {
                return Empty(context.State);
            }

            WorldState state = context.State;
            List<WorldEvent> events = [];

            if (attacker.Position.GetDistance(triggerer.Position) > 1)
            {
                if (!TryMoveBeside(attacker, triggerer, state, out ScenarioEventEffectResult approach))
                {
                    return Empty(context.State);
                }

                state = approach.State;
                events.AddRange(approach.Events);
                attacker = state.Stacks[attacker.Id];
            }

            (_, _, UnitDefinition triggererLeaderDefinition) = triggerer.GetLeader(Context.Definitions);
            (_, _, UnitDefinition attackerLeaderDefinition) = attacker.GetLeader(Context.Definitions);

            if (triggererLeaderDefinition.Category == UnitCategory.Illusion)
            {
                WorldUnitGroup destroyer = new WorldUnitGroup.Stack(attacker.Id);

                events.Add(new WorldStackDestroyedEvent(triggerer.Id, destroyer, attacker.OwnerId));
                state = WorldCommandHelpers.RemoveStack(state, triggerer.Id, destroyer, attacker.OwnerId, Context.Globals);
            }
            else if (attackerLeaderDefinition.Category != UnitCategory.Illusion)
            {
                BattleModifiers modifiers = WorldCommandHelpers.CreateModifiers(state, attacker.OwnerId, triggerer.OwnerId, Context);
                BattleState battleState = WorldCommandHelpers.CreateStackBattleState(attacker, triggerer, modifiers, Context);

                events.Add(new WorldStackBattleStartedEvent(attacker.Id, triggerer.Id, triggerer.Position, battleState, IsDuel: false));
            }

            return new ScenarioEventEffectResult(state, events);
        }

        public ScenarioEventEffectResult Visit(ScenarioEventCastSpellEffect effect, ScenarioEventEffectContext context)
        {
            WorldStack? triggerer = ScenarioEventTriggererCalculator.FindStack(context.TriggeredEvent.Trigger, context.State);
            if (triggerer is null)
            {
                return Empty(context.State);
            }

            return Cast(effect.SpellId, effect.CasterId, new WorldSpellTarget(triggerer.Position, Radius: 0), context);
        }

        public ScenarioEventEffectResult Visit(ScenarioEventCastSpellMapEffect effect, ScenarioEventEffectContext context)
        {
            ScenarioLocationDefinition location = Context.Scenario.GetLocation(effect.Location);

            return Cast(
                effect.SpellId,
                effect.CasterId,
                new WorldSpellTarget(location.Position, location.Radius),
                context);
        }

        public ScenarioEventEffectResult Visit(ScenarioEventCreateStackEffect effect, ScenarioEventEffectContext context)
        {
            ScenarioStackTemplateDefinition template = Context.Scenario.GetStackTemplate(effect.Template);
            ScenarioLocationDefinition location = Context.Scenario.GetLocation(effect.Location);
            WorldStackId stackId = WorldCommandHelpers.GenerateStackId(context.State);
            WorldStack? stack = TryCreateStack(template, location, stackId, context.State, Context);

            if (stack is null)
            {
                return Empty(context.State);
            }

            WorldState state = context.State with { Stacks = context.State.Stacks.SetItem(stackId, stack) };

            return new ScenarioEventEffectResult(state, [new WorldStackCreatedEvent(stack, WorldResourceSet.Empty)]);
        }

        public ScenarioEventEffectResult Visit(ScenarioEventRemoveStackEffect effect, ScenarioEventEffectContext context)
        {
            ImmutableArray<WorldStackId> instances = ScenarioStackReferenceCalculator.GetLiveInstances(effect.Stack, context.State);
            if (instances.IsEmpty)
            {
                return Empty(context.State);
            }

            if (effect.FirstOnly)
            {
                instances = [instances[0]];
            }

            List<WorldEvent> events = [];
            WorldState state = context.State;

            foreach (WorldStackId stackId in instances)
            {
                events.Add(new WorldStackRemovedEvent(stackId));
                state = WorldCommandHelpers.RemoveStack(state, stackId, destroyer: null, destroyerPlayerId: null, Context.Globals);
            }

            return new ScenarioEventEffectResult(state, events);
        }

        public ScenarioEventEffectResult Visit(ScenarioEventChangeOwnerEffect effect, ScenarioEventEffectContext context)
        {
            ImmutableArray<WorldStackId> instances = ScenarioStackReferenceCalculator.GetLiveInstances(effect.Stack, context.State);
            if (instances.IsEmpty)
            {
                return Empty(context.State);
            }

            if (effect.FirstOnly)
            {
                instances = [instances[0]];
            }

            WorldSubrace subrace = context.State.Subraces[effect.Subrace];
            WorldState state = context.State;
            List<WorldEvent> events = [];

            foreach (WorldStackId stackId in instances)
            {
                WorldStack stack = state.Stacks[stackId];
                WorldPosition? exit = FindFortificationExit(stack, state);

                if (exit is not null)
                {
                    ScenarioEventEffectResult exitResult = Relocate(stack, path: null, exit.Value, finalDirection: null, state);

                    state = exitResult.State;
                    events.AddRange(exitResult.Events);
                    stack = state.Stacks[stackId];
                }

                events.Add(new WorldStackOwnerChangedEvent(stackId, subrace.PlayerId, subrace.Id, effect.PlayAnimation));

                state = state with
                {
                    Stacks = state.Stacks.SetItem(stackId, stack with
                    {
                        OwnerId = subrace.PlayerId,
                        SubraceId = subrace.Id
                    })
                };
            }

            return new ScenarioEventEffectResult(state, events);
        }

        public ScenarioEventEffectResult Visit(ScenarioEventChangeOrderEffect effect, ScenarioEventEffectContext context)
        {
            ImmutableArray<WorldStackId> instances = ScenarioStackReferenceCalculator.GetLiveInstances(effect.Stack, context.State);
            if (instances.IsEmpty)
            {
                return Empty(context.State);
            }

            if (effect.FirstOnly)
            {
                instances = [instances[0]];
            }

            WorldState state = context.State;
            List<WorldEvent> events = [];

            foreach (WorldStackId stackId in instances)
            {
                WorldStack stack = state.Stacks[stackId];

                WorldCanonicalStackOrder? order = StackOrderCalculator.CalculateChangedOrder(effect.Order, stack, state);

                if (order is not null)
                {
                    events.Add(new WorldStackOrderChangedEvent(stackId, order));

                    state = state with
                    {
                        Stacks = state.Stacks.SetItem(stackId, stack with { Order = order })
                    };
                }
            }

            return new ScenarioEventEffectResult(state, events);
        }

        public ScenarioEventEffectResult Visit(ScenarioEventEnableEventEffect effect, ScenarioEventEffectContext context)
        {
            return Empty(context.State);
        }

        public ScenarioEventEffectResult Visit(ScenarioEventGiveItemEffect effect, ScenarioEventEffectContext context)
        {
            WorldUnitGroup? target = effect.Target switch
            {
                ScenarioEventGiveItemTarget.Triggerer => GetTriggererStack(),
                ScenarioEventGiveItemTarget.TriggererCapital => GetTriggererCapital()
            };

            if (target is null)
            {
                return Empty(context.State);
            }

            WorldItem item = WorldItemFactory.Create(
                WorldCommandHelpers.GenerateItemId(context.State),
                Context.Definitions.GetItem(effect.Item),
                Context.Globals);
            WorldItemsReceivedEvent receivedEvent = new(context.TriggeredEvent.TriggererId, target, [item]);
            WorldInventory inventory = WorldUnitGroupCalculator.GetInventory(context.State, target);
            WorldState state = WorldUnitGroupCalculator.WithInventory(context.State, target, inventory.Add(item));

            return new ScenarioEventEffectResult(state, [receivedEvent]);

            WorldUnitGroup? GetTriggererStack()
            {
                WorldStack? stack = ScenarioEventTriggererCalculator.FindStack(context.TriggeredEvent.Trigger, context.State);

                if (stack is null)
                {
                    return null;
                }

                return new WorldUnitGroup.Stack(stack.Id);
            }

            WorldUnitGroup? GetTriggererCapital()
            {
                WorldPlan plan = WorldPlan.For(context.State);
                WorldCapital? capital = plan.GetCapital(context.TriggeredEvent.TriggererId);

                if (capital is null)
                {
                    return null;
                }

                return new WorldUnitGroup.Fortification(capital.Id);
            }
        }

        public ScenarioEventEffectResult Visit(ScenarioEventGiveSpellEffect effect, ScenarioEventEffectContext context)
        {
            WorldSpellsReceivedEvent receivedEvent = new(context.TriggeredEvent.TriggererId, [effect.SpellId]);

            return new ScenarioEventEffectResult(context.State, [receivedEvent]);
        }

        public ScenarioEventEffectResult Visit(ScenarioEventDestroyItemEffect effect, ScenarioEventEffectContext context)
        {
            return Empty(context.State);
        }

        public ScenarioEventEffectResult Visit(ScenarioEventAllyEffect effect, ScenarioEventEffectContext context)
        {
            return new ScenarioEventEffectResult(
                context.State,
                [new WorldAllianceStartedEvent(effect.Player1, effect.Player2, effect.Permanent)]);
        }

        public ScenarioEventEffectResult Visit(ScenarioEventDiplomacyEffect effect, ScenarioEventEffectContext context)
        {
            return Empty(context.State);
        }

        public ScenarioEventEffectResult Visit(ScenarioEventUnfogEffect effect, ScenarioEventEffectContext context)
        {
            if (!effect.Reveal)
            {
                return Empty(context.State);
            }

            ScenarioLocationDefinition location = Context.Scenario.GetLocation(effect.Location);
            int radius = effect.Radius ?? location.Radius;
            List<WorldEvent> events = [];
            WorldState state = context.State;

            foreach (WorldPlayerId playerId in effect.Players)
            {
                if (state.Players.TryGetValue(playerId, out WorldPlayer? player))
                {
                    ImmutableBitmap.Builder fog = player.FogOfWar.ToBuilder();
                    ImmutableArray<WorldPosition>.Builder revealed = ImmutableArray.CreateBuilder<WorldPosition>();

                    foreach (WorldPosition tile in ScenarioLocationCalculator.EnumerateTiles(location.Position, radius, state.Map))
                    {
                        if (!player.FogOfWar.Get(tile.X, tile.Y))
                        {
                            fog.Set(tile.X, tile.Y);
                            revealed.Add(tile);
                        }
                    }

                    if (revealed.Count > 0)
                    {
                        events.Add(new WorldFogRevealedEvent(playerId, revealed.ToImmutable()));

                        state = state with
                        {
                            Players = state.Players.SetItem(playerId, player with { FogOfWar = fog.ToImmutable() })
                        };
                    }
                }
            }

            return new ScenarioEventEffectResult(state, events);
        }

        public ScenarioEventEffectResult Visit(ScenarioEventRemoveMountainEffect effect, ScenarioEventEffectContext context)
        {
            ScenarioLocationDefinition location = Context.Scenario.GetLocation(effect.Location);
            ImmutableArray<WorldMountainId> mountainIds = ScenarioMountainRemovalCalculator.GetMountains(context.State, location);

            if (mountainIds.IsEmpty)
            {
                return Empty(context.State);
            }

            WorldState state = ScenarioMountainRemovalCalculator.Remove(context.State, location, mountainIds);

            return new ScenarioEventEffectResult(state, [new WorldMountainsRemovedEvent(effect.Location, mountainIds)]);
        }

        public ScenarioEventEffectResult Visit(ScenarioEventRemoveLandmarkEffect effect, ScenarioEventEffectContext context)
        {
            return Empty(context.State);
        }

        public ScenarioEventEffectResult Visit(ScenarioEventChangeObjectiveEffect effect, ScenarioEventEffectContext context)
        {
            return Empty(context.State);
        }

        public ScenarioEventEffectResult Visit(ScenarioEventChangeLandmarkEffect effect, ScenarioEventEffectContext context)
        {
            return Empty(context.State);
        }

        public ScenarioEventEffectResult Visit(ScenarioEventChangeTerrainEffect effect, ScenarioEventEffectContext context)
        {
            return Empty(context.State);
        }

        public ScenarioEventEffectResult Visit(ScenarioEventModifyVariableEffect effect, ScenarioEventEffectContext context)
        {
            return Empty(context.State);
        }

        public override EventRuleResult<WorldEvent> Visit(WorldScenarioEventTriggeringEvent ev, WorldState state)
        {
            ScenarioEventDefinition definition = Context.Scenario.GetEvent(ev.EventId);
            List<WorldEvent> events = [];

            foreach (ScenarioEventEffect effect in definition.Effects)
            {
                ScenarioEventEffectContext effectContext = new(ev, state);
                ScenarioEventEffectResult result = effect.Accept(this, effectContext);

                state = result.State;
                events.AddRange(result.Events);
            }

            List<WorldEvent> checks = [];

            foreach (WorldEvent effectEvent in events)
            {
                switch (effectEvent)
                {
                    case WorldStackCreatedEvent data:
                        checks.Add(new WorldScenarioCheckEvent(new ScenarioEventStackCreatedTrigger(data.Stack.Id), []));
                        break;
                    case WorldStackRemovedEvent:
                        checks.Add(new WorldScenarioCheckEvent(new ScenarioEventStackDestroyedTrigger(null), []));
                        break;
                }
            }

            return new EventRuleResult<WorldEvent>(false, [.. events, .. checks]);
        }

        private static ScenarioEventEffectResult Cast(
            WorldSpellDefinitionId spellId,
            WorldPlayerId? casterId,
            WorldSpellTarget target,
            ScenarioEventEffectContext context)
        {
            WorldPlayerId resolvedCasterId = casterId ?? context.TriggeredEvent.TriggererId;

            if (!context.State.Players.ContainsKey(resolvedCasterId))
            {
                return Empty(context.State);
            }

            return new ScenarioEventEffectResult(
                context.State,
                [new WorldSpellCastEvent(resolvedCasterId, spellId, target, WorldResourceSet.Empty, WorldSpellCastSource.Scenario)]);
        }

        private static ScenarioEventEffectResult Relocate(
            WorldStack stack,
            WorldPath? path,
            WorldPosition target,
            WorldStackDirection? finalDirection,
            WorldState state)
        {
            ImmutableArray<WorldPosition> positions;

            if (path is not null && !path.Positions.IsEmpty)
            {
                positions = [stack.Position, .. path.Positions];
            }
            else
            {
                positions = [stack.Position, target];
            }

            List<WorldStackRelocatedEvent> steps = [];

            for (int index = 1; index < positions.Length; index++)
            {
                WorldStackDirection direction = StackStepCalculator.GetDirection(positions[index - 1], positions[index]);

                steps.Add(new WorldStackRelocatedEvent(stack.Id, positions[index - 1], positions[index], direction));
            }

            if (finalDirection is not null)
            {
                steps[^1] = steps[^1] with { NewDirection = finalDirection.Value };
            }

            WorldState relocatedState = state with
            {
                Stacks = state.Stacks.SetItem(
                    stack.Id,
                    stack with { Position = target, Direction = steps[^1].NewDirection })
            };

            return new ScenarioEventEffectResult(relocatedState, steps);
        }

        private static WorldStack? GetSubjectStack(ScenarioStackReference reference, WorldStack triggerer, WorldState state)
        {
            ImmutableArray<WorldStackId> instances = ScenarioStackReferenceCalculator.GetLiveInstances(reference, state);
            if (instances.IsEmpty)
            {
                return null;
            }

            WorldStack stack = state.Stacks[instances[0]];

            if (stack.Id == triggerer.Id)
            {
                return null;
            }

            return stack;
        }

        private static UnitGrid<WorldUnit> CreateUnits(UnitGrid<ScenarioStackTemplateUnit> templateUnits, WorldState state, IEngineContext context)
        {
            WorldStackBuilder builder = new(context.Definitions);
            IEnumerable<WorldUnitId> ids = WorldCommandHelpers.GenerateUnitIds(state);

            foreach (((ScenarioStackTemplateUnit unit, GridPosition position), WorldUnitId id) in templateUnits.GetUnits().Zip(ids))
            {
                UnitDefinition definition = context.Definitions.GetUnit(unit.DefinitionId);
                string? name = LeaderNameCalculator.TryGetName(definition, context.Definitions, context.Random);

                builder.PlaceUnit(new WorldUnit(id, unit.DefinitionId, unit.Hitpoints, Experience: 0, unit.Level, unit.Modifiers, name, IsTypeLocked: false), position);
            }

            return builder.Build();
        }

        private static WorldStack? TryCreateStack(
            ScenarioStackTemplateDefinition template,
            ScenarioLocationDefinition location,
            WorldStackId stackId,
            WorldState state,
            IEngineContext context)
        {
            WorldPlan plan = WorldPlan.For(state);

            WorldStack stack = new(
                stackId,
                template.OwnerId,
                location.Position,
                template.Direction ?? WorldStackDirection.South,
                CreateUnits(template.Units, state, context),
                MovementPoints: 0,
                Inventory: WorldInventory.Empty,
                LeaderEquipment.Empty,
                template.SubraceId,
                template.AiPriority,
                AiIgnore: false,
                Concealed: false,
                template.Order,
                ExpirationTurn: null,
                PendingLeaderUpgrades: 0,
                BattlesWon: 0,
                template.Id);

            StackStats stats = context.UnitStats.GetWorldStackStats(stack);
            WorldCanonicalStackOrder order = StackOrderCalculator.CalculateCreatedOrder(stack, state);
            stack = stack with
            {
                MovementPoints = stats.MaxMovementPoints,
                Order = order
            };

            WorldMovementCalculator.MovementProfile profile = WorldMovementCalculator.GetMovementProfile(stack, context.Definitions);

            WorldPosition[] positions =
            [
                location.Position,
                .. ScenarioLocationCalculator.EnumerateTiles(location, state.Map)
            ];

            foreach (WorldPosition position in positions)
            {
                if (plan.CanPlaceStackAt(position, profile))
                {
                    return stack with { Position = position };
                }
            }

            return null;
        }

        private bool TryMoveBeside(WorldStack stack, WorldStack triggerer, WorldState state, out ScenarioEventEffectResult result)
        {
            WorldPlan plan = WorldPlan.For(state);
            WorldMovementCalculator.MovementProfile profile = WorldMovementCalculator.GetMovementProfile(stack, Context.Definitions);

            ScenarioStackDestination? destination = FindNearestDestination(
                triggerer.Position.GetAdjacentPositions(),
                plan,
                profile,
                stack.Position);

            if (destination is null)
            {
                result = Empty(state);

                return false;
            }

            WorldStackDirection direction = StackStepCalculator.GetDirection(destination.Position, triggerer.Position);

            result = Relocate(stack, destination.Path, destination.Position, direction, state);

            return true;
        }

        private static ScenarioStackDestination? FindNearestDestination(
            IEnumerable<WorldPosition> candidates,
            WorldPlan plan,
            WorldMovementCalculator.MovementProfile profile,
            WorldPosition from)
        {
            WorldPathMap? pathMap = plan.GetPathMap(from, fogOfWar: null, profile);
            WorldPosition? position = null;
            ScenarioStackDestination? nearest = null;
            int nearestCost = int.MaxValue;

            foreach (WorldPosition candidate in candidates)
            {
                if (plan.CanPlaceStackAt(candidate, profile))
                {
                    position ??= candidate;

                    if (pathMap is not null &&
                        pathMap.TryGetPath(candidate, WorldMovementLimit.Unlimited, out WorldPath? path) &&
                        path.Cost < nearestCost)
                    {
                        nearest = new ScenarioStackDestination(candidate, path);
                        nearestCost = path.Cost;
                    }
                }
            }

            if (nearest is not null)
            {
                return nearest;
            }
            else if (position is not null)
            {
                return new ScenarioStackDestination(position.Value, null);
            }
            else
            {
                return null;
            }
        }

        private WorldPosition? FindFortificationExit(WorldStack stack, WorldState state)
        {
            WorldPlan plan = WorldPlan.For(state);
            WorldFortification? fortification = plan.GetFortificationAt(stack.Position);

            if (fortification is null)
            {
                return null;
            }

            return FortificationCalculator.FindExit(fortification, stack, state, Context.Definitions);
        }
    }
}
