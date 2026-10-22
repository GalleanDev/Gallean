using System.Text.Json.Serialization;

namespace Gallean.Engine.Contracts;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Stack), "stack")]
[JsonDerivedType(typeof(Fortification), "fortification")]
[JsonDerivedType(typeof(Location), "location")]
[JsonDerivedType(typeof(Bag), "bag")]
public abstract record WorldAiTarget
{
    public sealed record Stack(WorldStackId StackId) : WorldAiTarget
    {
        public override TResult Accept<TContext, TResult>(IWorldAiTargetVisitor<TContext, TResult> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }
    }

    public sealed record Fortification(WorldFortificationId FortificationId) : WorldAiTarget
    {
        public override TResult Accept<TContext, TResult>(IWorldAiTargetVisitor<TContext, TResult> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }
    }

    public sealed record Location(WorldPosition Position) : WorldAiTarget
    {
        public override TResult Accept<TContext, TResult>(IWorldAiTargetVisitor<TContext, TResult> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }
    }

    public sealed record Bag(WorldBagId BagId) : WorldAiTarget
    {
        public override TResult Accept<TContext, TResult>(IWorldAiTargetVisitor<TContext, TResult> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }
    }

    public abstract TResult Accept<TContext, TResult>(IWorldAiTargetVisitor<TContext, TResult> visitor, TContext context);
}
