using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

public closed partial record AbstractWorldMoveStackCommand
{
    private sealed class EncounterVisitor : PathTraversalCalculator.IEncounterVisitor<EncounterVisitor.Context, WorldCommandResult>, IStaticInstance<EncounterVisitor>
    {
        public sealed record Context(
            WorldState State,
            WorldStack Stack,
            WorldMovementCalculator.MovementProfile Profile,
            WorldPlan Plan,
            ImmutableArray<WorldPosition> Approach,
            IEngineContext Engine);

        public WorldCommandResult Visit(PathTraversalCalculator.Encounter.Bag encounter, Context context)
        {
            if (context.Approach.IsEmpty)
            {
                return WorldCommandResult.Success(
                    Empty.Value,
                    [new WorldBagPickedUpEvent(context.Stack.Id, encounter.Target.Id, encounter.Target.Inventory)]);
            }
            else
            {
                return Approach(context, encounter.Position);
            }
        }

        public WorldCommandResult Visit(PathTraversalCalculator.Encounter.StackBattle encounter, Context context)
        {
            if (context.Profile.IsIllusion)
            {
                return WorldCommandResult.Fail(WorldCommandError.IllusionAttackForbidden);
            }

            if (!encounter.CanBegin && context.Approach.IsEmpty)
            {
                return WorldCommandResult.Fail(WorldCommandError.InsufficientMovement);
            }

            if (!encounter.CanBegin)
            {
                return Approach(context, null);
            }

            if (context.Approach.IsEmpty)
            {
                (_, _, UnitDefinition leaderDefinition) = encounter.Stack.GetLeader(context.Engine.Definitions);

                List<WorldEvent> events;
                if (leaderDefinition.Category == UnitCategory.Illusion)
                {
                    events = WorldCommandHelpers.CreateDestroyIllusionEvents(context.Stack, encounter.Stack, context.Engine);
                }
                else
                {
                    events = WorldCommandHelpers.CreateBattleStartEvents(context.State, context.Stack, encounter.Stack, null, context.Engine);
                }

                return WorldCommandResult.Success(Empty.Value, events);
            }
            else
            {
                return Approach(context, encounter.Position);
            }
        }

        public WorldCommandResult Visit(PathTraversalCalculator.Encounter.FortificationBattle encounter, Context context)
        {
            if (context.Profile.IsIllusion)
            {
                return WorldCommandResult.Fail(WorldCommandError.IllusionAttackForbidden);
            }

            if (!encounter.CanBegin && context.Approach.IsEmpty)
            {
                return WorldCommandResult.Fail(WorldCommandError.InsufficientMovement);
            }

            if (!encounter.CanBegin)
            {
                return Approach(context, null);
            }

            if (context.Approach.IsEmpty)
            {
                List<WorldEvent> battleEvents = WorldCommandHelpers.CreateBattleStartEvents(
                    context.State, context.Stack, null, encounter.Fortification, context.Engine);

                return WorldCommandResult.Success(Empty.Value, battleEvents);
            }
            else
            {
                return Approach(context, encounter.Position);
            }
        }

        private static WorldCommandResult Approach(Context context, WorldPosition? encounterPosition)
        {
            WorldStackMovingEvent stepEvent = WorldCommandHelpers.CreateMoveStepEvent(
                context.Stack,
                context.Plan,
                context.Profile,
                context.Approach,
                encounterPosition);

            return WorldCommandResult.Success(Empty.Value, [stepEvent]);
        }
    }
}
