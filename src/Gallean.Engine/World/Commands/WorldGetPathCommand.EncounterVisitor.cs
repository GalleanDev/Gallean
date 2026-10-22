using Gallean.Common;

namespace Gallean.Engine;

public sealed partial record WorldGetPathCommand
{
    private sealed class EncounterVisitor : PathTraversalCalculator.IEncounterVisitor<EncounterVisitor.Context, WorldPathEncounter?>, IStaticInstance<EncounterVisitor>
    {
        public sealed record Context(
            WorldState State,
            WorldStack Stack,
            WorldMovementCalculator.MovementProfile Profile,
            IEngineContext Engine);

        public WorldPathEncounter? Visit(PathTraversalCalculator.Encounter.Bag encounter, Context context)
        {
            if (context.Stack.Position.GetDistance(encounter.Position) <= context.Engine.Globals.BagRange)
            {
                return new WorldPathEncounter.Bag(encounter.Target.Id);
            }
            else
            {
                return null;
            }
        }

        public WorldPathEncounter? Visit(PathTraversalCalculator.Encounter.StackBattle encounter, Context context)
        {
            return GetBattleEncounter(encounter, context);
        }

        public WorldPathEncounter? Visit(PathTraversalCalculator.Encounter.FortificationBattle encounter, Context context)
        {
            return GetBattleEncounter(encounter, context);
        }

        private static WorldPathEncounter? GetBattleEncounter(PathTraversalCalculator.Encounter.Battle encounter, Context context)
        {
            if (!encounter.CanBegin)
            {
                return null;
            }

            if (context.Profile.IsThief)
            {
                ThiefTarget? target = ThiefCalculator.ResolveTarget(context.State, context.Engine, context.Stack.OwnerId, encounter.Position);

                if (target is not null)
                {
                    return new WorldPathEncounter.Thief(encounter.Position);
                }
            }

            return new WorldPathEncounter.Battle(encounter.Position);
        }
    }
}
