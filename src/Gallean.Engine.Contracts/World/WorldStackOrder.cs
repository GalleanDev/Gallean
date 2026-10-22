using System.Text.Json.Serialization;
using Gallean.Common;

namespace Gallean.Engine.Contracts;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Normal), "normal")]
[JsonDerivedType(typeof(Stand), "stand")]
[JsonDerivedType(typeof(Guard), "guard")]
[JsonDerivedType(typeof(AttackStack), "attackStack")]
[JsonDerivedType(typeof(DefendStack), "defendStack")]
[JsonDerivedType(typeof(SecureCity), "secureCity")]
[JsonDerivedType(typeof(Roam), "roam")]
[JsonDerivedType(typeof(Explore), "explore")]
[JsonDerivedType(typeof(MoveToLocation), "moveToLocation")]
[JsonDerivedType(typeof(DefendLocation), "defendLocation")]
[JsonDerivedType(typeof(Berserk), "berserk")]
[JsonDerivedType(typeof(Assist), "assist")]
[JsonDerivedType(typeof(Steal), "steal")]
[JsonDerivedType(typeof(DefendCity), "defendCity")]
[JsonDerivedType(typeof(Heal), "heal")]
[JsonDerivedType(typeof(Reinforce), "reinforce")]
public abstract record WorldStackOrder
{
    public sealed record Normal : WorldCanonicalStackOrder, IStaticInstance<Normal>
    {
        public override TResult Accept<TContext, TResult>(IWorldStackOrderVisitor<TContext, TResult> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }
    }

    public sealed record Stand : WorldCanonicalStackOrder, IStaticInstance<Stand>
    {
        public override TResult Accept<TContext, TResult>(IWorldStackOrderVisitor<TContext, TResult> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }
    }

    public sealed record Guard : WorldCanonicalStackOrder, IStaticInstance<Guard>
    {
        public override TResult Accept<TContext, TResult>(IWorldStackOrderVisitor<TContext, TResult> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }
    }

    public sealed record AttackStack(WorldStackId TargetId) : WorldCanonicalStackOrder
    {
        public override TResult Accept<TContext, TResult>(IWorldStackOrderVisitor<TContext, TResult> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }
    }

    public sealed record DefendStack(WorldStackId TargetId) : WorldCanonicalStackOrder
    {
        public override TResult Accept<TContext, TResult>(IWorldStackOrderVisitor<TContext, TResult> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }
    }

    public sealed record SecureCity(WorldFortificationId TargetId) : WorldCanonicalStackOrder
    {
        public override TResult Accept<TContext, TResult>(IWorldStackOrderVisitor<TContext, TResult> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }
    }

    public sealed record Roam : WorldCanonicalStackOrder, IStaticInstance<Roam>
    {
        public override TResult Accept<TContext, TResult>(IWorldStackOrderVisitor<TContext, TResult> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }
    }

    public sealed record Explore : WorldCanonicalStackOrder, IStaticInstance<Explore>
    {
        public override TResult Accept<TContext, TResult>(IWorldStackOrderVisitor<TContext, TResult> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }
    }

    public sealed record MoveToLocation(
        ScenarioLocationId TargetId,
        WorldPosition TargetPosition,
        int Radius) : WorldCanonicalStackOrder
    {
        public override TResult Accept<TContext, TResult>(IWorldStackOrderVisitor<TContext, TResult> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }
    }

    public sealed record DefendLocation(
        ScenarioLocationId TargetId,
        WorldPosition TargetPosition,
        int Radius) : WorldCanonicalStackOrder
    {
        public override TResult Accept<TContext, TResult>(IWorldStackOrderVisitor<TContext, TResult> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }
    }

    public sealed record Berserk : WorldCanonicalStackOrder, IStaticInstance<Berserk>
    {
        public override TResult Accept<TContext, TResult>(IWorldStackOrderVisitor<TContext, TResult> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }
    }

    public sealed record Assist(WorldStackId TargetId) : WorldCanonicalStackOrder
    {
        public override TResult Accept<TContext, TResult>(IWorldStackOrderVisitor<TContext, TResult> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }
    }

    public sealed record Steal : WorldCanonicalStackOrder, IStaticInstance<Steal>
    {
        public override TResult Accept<TContext, TResult>(IWorldStackOrderVisitor<TContext, TResult> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }
    }

    public sealed record DefendCity(WorldFortificationId TargetId) : WorldCanonicalStackOrder
    {
        public override TResult Accept<TContext, TResult>(IWorldStackOrderVisitor<TContext, TResult> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }
    }

    public sealed record Heal : WorldStackOrder, IStaticInstance<Heal>
    {
        public override TResult Accept<TContext, TResult>(IWorldStackOrderVisitor<TContext, TResult> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }
    }

    public sealed record Reinforce(WorldFortificationId TargetId) : WorldStackOrder
    {
        public override TResult Accept<TContext, TResult>(IWorldStackOrderVisitor<TContext, TResult> visitor, TContext context)
        {
            return visitor.Visit(this, context);
        }
    }

    public abstract TResult Accept<TContext, TResult>(IWorldStackOrderVisitor<TContext, TResult> visitor, TContext context);
}
