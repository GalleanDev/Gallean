using Gallean.Common;

namespace Gallean.Engine;

public abstract partial record AbstractWorldMoveStackCommand
{
    private sealed class EncounterVisitor : PathTraversalCalculator.IEncounterVisitor<EncounterVisitor.Context, WorldCommandResult>, IStaticInstance<EncounterVisitor>
    {
        public sealed record Context(WorldState State, WorldStack Stack, IEngineContext Engine);

        public WorldCommandResult Visit(PathTraversalCalculator.Encounter.Bag encounter, Context context)
        {
            return WorldCommandResult.Success(
                Empty.Value,
                [new WorldBagPickedUpEvent(context.Stack.Id, encounter.Target.Id, encounter.Target.Inventory)]);
        }

        public WorldCommandResult Visit(PathTraversalCalculator.Encounter.StackBattle encounter, Context context)
        {
            if (!encounter.CanBegin)
            {
                return WorldCommandResult.Fail(WorldCommandError.InsufficientMovement);
            }

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

        public WorldCommandResult Visit(PathTraversalCalculator.Encounter.FortificationBattle encounter, Context context)
        {
            if (!encounter.CanBegin)
            {
                return WorldCommandResult.Fail(WorldCommandError.InsufficientMovement);
            }

            List<WorldEvent> battleEvents = WorldCommandHelpers.CreateBattleStartEvents(
                context.State, context.Stack, null, encounter.Fortification, context.Engine);

            return WorldCommandResult.Success(Empty.Value, battleEvents);
        }
    }
}
