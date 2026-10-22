namespace Gallean.Engine;

public abstract record WorldPathEncounter
{
    public sealed record Battle(WorldPosition TargetPosition) : WorldPathEncounter
    {
        public override TResult Accept<TContext, TResult>(IWorldPathEncounterVisitor<TContext, TResult> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }
    }

    public sealed record Thief(WorldPosition TargetPosition) : WorldPathEncounter
    {
        public override TResult Accept<TContext, TResult>(IWorldPathEncounterVisitor<TContext, TResult> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }
    }

    public sealed record Bag(WorldBagId BagId) : WorldPathEncounter
    {
        public override TResult Accept<TContext, TResult>(IWorldPathEncounterVisitor<TContext, TResult> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }
    }

    public sealed record Site(WorldSiteId SiteId) : WorldPathEncounter
    {
        public override TResult Accept<TContext, TResult>(IWorldPathEncounterVisitor<TContext, TResult> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }
    }

    public abstract TResult Accept<TContext, TResult>(IWorldPathEncounterVisitor<TContext, TResult> visitor, TContext context);
}
