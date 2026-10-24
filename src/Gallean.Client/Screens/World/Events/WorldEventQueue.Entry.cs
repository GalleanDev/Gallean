using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class WorldEventQueue
{
    public abstract record Entry(WorldState State)
    {
        public abstract record Animated(WorldState State) : Entry(State)
        {
            public abstract WorldAnimationState CreateAnimation(IGameDefinitionProvider definitions, WorldVisibility visibility);
        }

        public sealed record StackMove(
            WorldState State,
            WorldStackId StackId,
            IReadOnlyList<StackMove.Step> Steps) : Animated(State)
        {
            public sealed record Step(WorldPosition Position, WorldStackDirection Direction);

            private static readonly TimeSpan DefaultStepDuration = TimeSpan.FromMilliseconds(200);
            private static readonly TimeSpan LargeLeaderStepDuration = 2 * DefaultStepDuration;

            public bool Equals(StackMove? other)
            {
                return base.Equals(other) &&
                       StackId == other.StackId &&
                       Steps.SequenceEqual(other.Steps);
            }

            public override int GetHashCode()
            {
                HashCode hash = new();

                hash.Add(base.GetHashCode());
                hash.Add(StackId);
                hash.AddSequence(Steps);

                return hash.ToHashCode();
            }

            public override Entry? TryExtend(EngineStep<WorldState, WorldEvent> step)
            {
                return step.Event switch
                {
                    WorldFogRevealedEvent data when IsSameStackOwner(data) =>
                        this with { State = step.NewState },
                    WorldStackMovingEvent data when IsSameMove(data) =>
                        this with { State = step.NewState, Steps = AppendStep(data.TargetPosition, data.NewDirection) },
                    WorldStackMovedEvent data when data.StackId == StackId && data.NewMovementPoints > 0 =>
                        this with { State = step.NewState },
                    WorldStackRelocatedEvent data when IsSameRelocate(data) =>
                        this with { State = step.NewState, Steps = AppendStep(data.TargetPosition, data.NewDirection) },
                    _ => null
                };

                bool IsSameStackOwner(WorldFogRevealedEvent data)
                {
                    return State.Stacks.TryGetValue(StackId, out WorldStack? stack) && stack.OwnerId == data.PlayerId;
                }

                bool IsSameMove(WorldStackMovingEvent data)
                {
                    return data.StackId == StackId &&
                           data.SourcePosition == Steps[^1].Position;
                }

                bool IsSameRelocate(WorldStackRelocatedEvent data)
                {
                    return data.StackId == StackId &&
                           data.SourcePosition == Steps[^1].Position &&
                           data.SourcePosition.IsAdjacentTo(data.TargetPosition);
                }

                IReadOnlyList<Step> AppendStep(WorldPosition position, WorldStackDirection direction)
                {
                    List<Step> steps = [.. Steps];

                    steps[^1] = new Step(steps[^1].Position, direction);
                    steps.Add(new Step(position, direction));

                    return steps;
                }
            }

            public override WorldAnimationState CreateAnimation(IGameDefinitionProvider definitions, WorldVisibility visibility)
            {
                TimeSpan stepDuration = DefaultStepDuration;

                if (State.Stacks.TryGetValue(StackId, out WorldStack? stack))
                {
                    (WorldUnit _, GridPosition _, UnitDefinition leaderDefinition) = stack.GetLeader(definitions);

                    if (leaderDefinition.IsLarge)
                    {
                        stepDuration = LargeLeaderStepDuration;
                    }
                }

                List<WorldAnimationState.StackMoveStep> steps = [];
                TimeSpan start = TimeSpan.Zero;

                for (int index = 0; index < Steps.Count; index++)
                {
                    Step step = Steps[index];

                    steps.Add(new WorldAnimationState.StackMoveStep(step.Position, step.Direction, start));

                    if (index + 1 < Steps.Count && IsStepVisible(visibility, step.Position, Steps[index + 1].Position))
                    {
                        start += GetStepDuration(step.Direction, stepDuration);
                    }
                }

                return new WorldAnimationState.StackMove(StackId, steps);
            }

            public override WorldEventFocus GetFocus()
            {
                return new WorldEventFocus(Steps[1].Position, PlayerId: null);
            }

            private static bool IsStepVisible(WorldVisibility visibility, WorldPosition from, WorldPosition to)
            {
                return visibility.IsRevealed(from) || visibility.IsRevealed(to);
            }

            private static TimeSpan GetStepDuration(WorldStackDirection direction, TimeSpan defaultDuration)
            {
                return direction switch
                {
                    WorldStackDirection.SouthWest => defaultDuration,
                    WorldStackDirection.West => 2 * defaultDuration,
                    WorldStackDirection.NorthWest => defaultDuration,
                    WorldStackDirection.North => defaultDuration,
                    WorldStackDirection.NorthEast => defaultDuration,
                    WorldStackDirection.East => 2 * defaultDuration,
                    WorldStackDirection.SouthEast => defaultDuration,
                    WorldStackDirection.South => defaultDuration
                };
            }
        }

        public sealed record Battle(
            WorldState State,
            WorldStackId AttackerStackId,
            WorldPosition Position) : Entry(State)
        {
            public override WorldEventFocus GetFocus()
            {
                WorldStack? attacker = State.Stacks.GetValueOrDefault(AttackerStackId);

                return new WorldEventFocus(Position, attacker?.OwnerId);
            }
        }

        public sealed record StackDestroy(WorldState State, WorldStack Stack) : Animated(State)
        {
            public override WorldAnimationState CreateAnimation(IGameDefinitionProvider definitions, WorldVisibility visibility)
            {
                (WorldUnit leader, GridPosition _, UnitDefinition _) = Stack.GetLeader(definitions);

                return new WorldAnimationState.StackDestroy(Stack.Position, leader.DefinitionId);
            }

            public override WorldEventFocus GetFocus()
            {
                return new WorldEventFocus(Stack.Position, PlayerId: null);
            }
        }

        public sealed record StackRemove(WorldState State, WorldStack Stack) : Animated(State)
        {
            public override WorldAnimationState CreateAnimation(IGameDefinitionProvider definitions, WorldVisibility visibility)
            {
                (WorldUnit leader, GridPosition _, UnitDefinition _) = Stack.GetLeader(definitions);

                return new WorldAnimationState.StackRemove(Stack.Position, leader.DefinitionId);
            }

            public override WorldEventFocus GetFocus()
            {
                return new WorldEventFocus(Stack.Position, PlayerId: null);
            }
        }

        public sealed record StackCreate(WorldState State, WorldStack Stack) : Animated(State)
        {
            public override WorldAnimationState CreateAnimation(IGameDefinitionProvider definitions, WorldVisibility visibility)
            {
                return new WorldAnimationState.StackCreate(Stack.Position);
            }

            public override WorldEventFocus GetFocus()
            {
                return new WorldEventFocus(Stack.Position, PlayerId: null);
            }
        }

        public sealed record StackOwnerChange(WorldState State, WorldPosition Position) : Animated(State)
        {
            public override WorldAnimationState CreateAnimation(IGameDefinitionProvider definitions, WorldVisibility visibility)
            {
                return new WorldAnimationState.StackOwnerChange(Position);
            }

            public override WorldEventFocus GetFocus()
            {
                return new WorldEventFocus(Position, PlayerId: null);
            }
        }

        public sealed record MountainRemove(
            WorldState State,
            WorldPosition Center,
            int Radius) : Animated(State)
        {
            public override WorldAnimationState CreateAnimation(IGameDefinitionProvider definitions, WorldVisibility visibility)
            {
                return new WorldAnimationState.MountainRemove(Center, Radius);
            }

            public override WorldEventFocus GetFocus()
            {
                return new WorldEventFocus(Center, null);
            }
        }

        public sealed record Unfog(
            WorldState State,
            WorldPosition Position,
            WorldPlayerId PlayerId) : Animated(State)
        {
            public override WorldAnimationState CreateAnimation(IGameDefinitionProvider definitions, WorldVisibility visibility)
            {
                return new WorldAnimationState.Pause(PauseDurationDefault);
            }

            public override WorldEventFocus GetFocus()
            {
                return new WorldEventFocus(Position, PlayerId: null);
            }
        }

        public sealed record SpellCast(
            WorldState State,
            WorldPlayerId? CasterId,
            WorldSpellDefinitionId SpellId,
            WorldPosition Position,
            WorldStackId? SummonedStackId) : Animated(State)
        {
            public override Entry? TryExtend(EngineStep<WorldState, WorldEvent> step)
            {
                if (SummonedStackId is not null)
                {
                    return null;
                }

                return step.Event switch
                {
                    WorldStackCreatedEvent data when data.Stack.Position == Position => this with
                    {
                        State = step.NewState,
                        SummonedStackId = data.Stack.Id
                    },
                    _ => null
                };
            }

            public override WorldAnimationState CreateAnimation(IGameDefinitionProvider definitions, WorldVisibility visibility)
            {
                return new WorldAnimationState.SpellCast(SpellId, Position, SummonedStackId);
            }

            public override WorldEventFocus GetFocus()
            {
                return new WorldEventFocus(Position, CasterId);
            }
        }

        public sealed record GlobalSpellCast(
            WorldState State,
            WorldSpellDefinitionId SpellId,
            WorldPosition Position) : Animated(State)
        {
            public override WorldAnimationState CreateAnimation(IGameDefinitionProvider definitions, WorldVisibility visibility)
            {
                return new WorldAnimationState.SpellCast(SpellId, Position, SummonedStackId: null);
            }

            public override WorldEventFocus GetFocus()
            {
                return new WorldEventFocus(Position, PlayerId: null);
            }
        }

        public sealed record ScenarioPopup(
            WorldState State,
            ImmutableArray<WorldScenarioMessage> Messages) : Entry(State)
        {
            public bool Equals(ScenarioPopup? other)
            {
                return base.Equals(other) &&
                       Messages.SequenceEqual(other.Messages);
            }

            public override int GetHashCode()
            {
                HashCode hash = new();

                hash.Add(base.GetHashCode());
                hash.AddSequence(Messages);

                return hash.ToHashCode();
            }

            public override Entry? TryExtend(EngineStep<WorldState, WorldEvent> step)
            {
                if (step.Event is WorldScenarioPopupEvent)
                {
                    WorldScenarioMessage message = step.NewState.ScenarioState.Messages[^1];

                    if (IsSameDialog(message))
                    {
                        return this with
                        {
                            State = step.NewState,
                            Messages = Messages.Add(message)
                        };
                    }
                }

                return null;
            }

            private bool IsSameDialog(WorldScenarioMessage message)
            {
                return Messages[^1].EventId == message.EventId &&
                       Messages[^1].TriggererId == message.TriggererId &&
                       Messages[^1].Audience.SequenceEqual(message.Audience);
            }

            public override WorldEventFocus? GetFocus()
            {
                return null;
            }
        }

        public sealed record ObjectiveChange(WorldState State, ScenarioTextId TextId) : Entry(State)
        {
            public override WorldEventFocus? GetFocus()
            {
                return null;
            }
        }

        public sealed record Pause(
            WorldState State,
            WorldPosition Position,
            WorldPlayerId? PlayerId,
            TimeSpan Duration) : Animated(State)
        {
            public override WorldAnimationState CreateAnimation(IGameDefinitionProvider definitions, WorldVisibility visibility)
            {
                return new WorldAnimationState.Pause(Duration);
            }

            public override WorldEventFocus GetFocus()
            {
                return new WorldEventFocus(Position, PlayerId);
            }
        }

        public sealed record ThiefActionResult(
            WorldState State,
            WorldPosition Position,
            WorldPlayerId PlayerId,
            WorldThiefActionType Type,
            bool IsSuccess) : Animated(State)
        {
            public override WorldAnimationState CreateAnimation(IGameDefinitionProvider definitions, WorldVisibility visibility)
            {
                return new WorldAnimationState.Pause(PauseDurationDefault);
            }

            public override WorldEventFocus GetFocus()
            {
                return new WorldEventFocus(Position, PlayerId);
            }
        }

        public sealed record StackIdle(WorldState State, WorldStackId StackId) : Entry(State)
        {
            public override WorldEventFocus? GetFocus()
            {
                return null;
            }
        }

        public sealed record StackExhaust(WorldState State, WorldStackId StackId) : Entry(State)
        {
            public override Entry? TryExtend(EngineStep<WorldState, WorldEvent> step)
            {
                return step.Event switch
                {
                    WorldStackBattleStartedEvent data when data.AttackerStackId == StackId =>
                        new Battle(step.NewState, data.AttackerStackId, data.Position),
                    WorldFortificationBattleStartedEvent data when data.AttackerStackId == StackId =>
                        new Battle(step.NewState, data.AttackerStackId, data.Position),
                    _ => null
                };
            }

            public override WorldEventFocus? GetFocus()
            {
                return null;
            }
        }

        public sealed record TurnStart(WorldState State, WorldPlayerId PlayerId) : Entry(State)
        {
            public override WorldEventFocus? GetFocus()
            {
                return null;
            }
        }

        public sealed record HotseatTurn(WorldState State, WorldPlayerId PlayerId) : Entry(State)
        {
            public override WorldEventFocus? GetFocus()
            {
                return null;
            }
        }

        public sealed record ScenarioEnd(WorldState State, WorldPlayerId? WinnerId) : Entry(State)
        {
            public override WorldEventFocus? GetFocus()
            {
                return null;
            }
        }

        public sealed record CityRiotEnd(WorldState State, WorldPosition Position) : Entry(State)
        {
            public override WorldEventFocus? GetFocus()
            {
                return null;
            }
        }

        public sealed record FortificationEnter(WorldState State, WorldPosition Position) : Entry(State)
        {
            public override WorldEventFocus? GetFocus()
            {
                return null;
            }
        }

        public sealed record FortificationExit(WorldState State, WorldPosition Position, WorldStackId StackId) : Entry(State)
        {
            public override WorldEventFocus? GetFocus()
            {
                return null;
            }
        }

        public sealed record RuinLoot(
            WorldState State,
            WorldPosition Position,
            WorldStackId StackId,
            WorldResourceSet Resources,
            WorldItem? Item) : Entry(State)
        {
            public override WorldEventFocus? GetFocus()
            {
                return null;
            }
        }

        public sealed record BagPickUp(
            WorldState State,
            WorldStackId StackId,
            WorldInventory Items) : Entry(State)
        {
            public override WorldEventFocus? GetFocus()
            {
                return null;
            }
        }

        public sealed record ItemsReceive(
            WorldState State,
            WorldPlayerId PlayerId,
            ImmutableArray<WorldItem> Items) : Entry(State)
        {
            public bool Equals(ItemsReceive? other)
            {
                return base.Equals(other) &&
                       PlayerId == other.PlayerId &&
                       Items.SequenceEqual(other.Items);
            }

            public override int GetHashCode()
            {
                HashCode hash = new();

                hash.Add(base.GetHashCode());
                hash.Add(PlayerId);
                hash.AddSequence(Items);

                return hash.ToHashCode();
            }

            public override WorldEventFocus? GetFocus()
            {
                return null;
            }
        }

        public sealed record SpellsReceive(
            WorldState State,
            WorldPlayerId PlayerId,
            ImmutableArray<WorldSpellDefinitionId> Spells) : Entry(State)
        {
            public bool Equals(SpellsReceive? other)
            {
                return base.Equals(other) &&
                       PlayerId == other.PlayerId &&
                       Spells.SequenceEqual(other.Spells);
            }

            public override int GetHashCode()
            {
                HashCode hash = new();

                hash.Add(base.GetHashCode());
                hash.Add(PlayerId);
                hash.AddSequence(Spells);

                return hash.ToHashCode();
            }

            public override WorldEventFocus? GetFocus()
            {
                return null;
            }
        }

        public sealed record LeaderUpgrade(WorldState State, WorldStackId StackId) : Entry(State)
        {
            public override WorldEventFocus? GetFocus()
            {
                if (State.Stacks.TryGetValue(StackId, out WorldStack? stack))
                {
                    return new WorldEventFocus(stack.Position, null);
                }
                else
                {
                    return null;
                }
            }
        }

        public sealed record ThiefAction(
            WorldState State,
            WorldStackId StackId,
            WorldPosition TargetPosition) : Entry(State)
        {
            public override WorldEventFocus? GetFocus()
            {
                return new WorldEventFocus(TargetPosition, null);
            }
        }

        public sealed record SiteEnter(
            WorldState State,
            WorldStackId StackId,
            WorldSiteId SiteId) : Entry(State)
        {
            public override WorldEventFocus? GetFocus()
            {
                return null;
            }
        }

        public sealed record DiplomacyProposal(WorldState State, WorldDiplomacyProposal Proposal) : Entry(State)
        {
            public override WorldEventFocus? GetFocus()
            {
                return null;
            }
        }

        public sealed record GameStart(WorldState State) : Entry(State)
        {
            public override WorldEventFocus? GetFocus()
            {
                return null;
            }
        }

        public virtual bool Equals(Entry? other)
        {
            if (other is null)
            {
                return false;
            }

            return ReferenceEquals(State, other.State);
        }

        public override int GetHashCode()
        {
            return RuntimeHelpers.GetHashCode(State);
        }

        public virtual Entry? TryExtend(EngineStep<WorldState, WorldEvent> step)
        {
            return null;
        }

        public abstract WorldEventFocus? GetFocus();

        public WorldPosition? GetFocusPosition(WorldPlayerId viewPlayerId)
        {
            WorldEventFocus? focus = GetFocus();

            if (focus is null)
            {
                return null;
            }

            if (focus.PlayerId == viewPlayerId)
            {
                return null;
            }
            else
            {
                return focus.Position;
            }
        }
    }
}
