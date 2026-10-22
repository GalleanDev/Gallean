using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

public sealed record ScenarioEventUnfogEffect(
    ScenarioLocationId Location,
    bool Reveal,
    int? Radius,
    ImmutableArray<WorldPlayerId> Players) : ScenarioEventEffect
{
    public bool Equals(ScenarioEventUnfogEffect? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Location == other.Location &&
               Reveal == other.Reveal &&
               Radius == other.Radius &&
               Players.SequenceEqual(other.Players);
    }

    public override TResult Accept<TContext, TResult>(IScenarioEventEffectVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(Location);
        hash.Add(Reveal);
        hash.Add(Radius);
        hash.AddSequence(Players);

        return hash.ToHashCode();
    }
}
