namespace Gallean.Engine;

internal static partial class PathTraversalCalculator
{
    public abstract record Encounter(WorldPosition Position, bool CanBegin)
    {
        public sealed record Bag(WorldBag Target, bool CanBegin) : Encounter(Target.Position, CanBegin)
        {
            public override TResult Accept<TContext, TResult>(IEncounterVisitor<TContext, TResult> visitor, TContext context)
            {
                return visitor.Visit(this, context);
            }
        }

        public abstract record Battle(WorldPosition Position, bool CanBegin) : Encounter(Position, CanBegin);

        public sealed record StackBattle(WorldStack Stack, bool CanBegin) : Battle(Stack.Position, CanBegin)
        {
            public override TResult Accept<TContext, TResult>(IEncounterVisitor<TContext, TResult> visitor, TContext context)
            {
                return visitor.Visit(this, context);
            }
        }

        public sealed record FortificationBattle(WorldFortification Fortification, bool CanBegin) : Battle(Fortification.Entrance, CanBegin)
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
