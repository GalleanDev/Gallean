using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldSpellsReceivedEvent(
    WorldPlayerId PlayerId,
    ImmutableArray<WorldSpellDefinitionId> Spells) : WorldEvent
{
    public bool Equals(WorldSpellsReceivedEvent? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return PlayerId == other.PlayerId &&
               Spells.SequenceEqual(other.Spells);
    }

    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(PlayerId);
        hash.AddSequence(Spells);

        return hash.ToHashCode();
    }
}
