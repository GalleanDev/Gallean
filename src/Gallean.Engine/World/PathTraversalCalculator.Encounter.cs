namespace Gallean.Engine;

internal static partial class PathTraversalCalculator
{
    public abstract record Encounter(WorldPosition Position)
    {
        public sealed record Bag(WorldBag Target) : Encounter(Target.Position)
        {
            public override TResult Accept<TContext, TResult>(IEncounterVisitor<TContext, TResult> visitor, TContext context)
            {
                return visitor.Visit(this, context);
            }
        }

        public abstract record Battle(WorldPosition Position, bool CanBegin, bool CanAvoid) : Encounter(Position);

        public sealed record StackBattle(WorldStack Stack, bool CanBegin, bool CanAvoid) : Battle(Stack.Position, CanBegin, CanAvoid)
        {
            public override TResult Accept<TContext, TResult>(IEncounterVisitor<TContext, TResult> visitor, TContext context)
            {
                return visitor.Visit(this, context);
            }
        }

        public sealed record FortificationBattle(WorldFortification Fortification, bool CanBegin) : Battle(Fortification.Entrance, CanBegin, CanAvoid: false)
        {
            public override TResult Accept<TContext, TResult>(IEncounterVisitor<TContext, TResult> visitor, TContext context)
            {
                return visitor.Visit(this, context);
            }
        }

        public abstract TResult Accept<TContext, TResult>(IEncounterVisitor<TContext, TResult> visitor, TContext context);
    }

    public interface IEncounterVisitor<in TContext, out TResult>
    {
        TResult Visit(Encounter.Bag encounter, TContext context);
        TResult Visit(Encounter.StackBattle encounter, TContext context);
        TResult Visit(Encounter.FortificationBattle encounter, TContext context);
    }
}
