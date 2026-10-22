using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Gallean.Common;

namespace Gallean.Engine.Contracts;

public sealed record WorldPlayerTurnActions
{
    [JsonRequired]
    public bool BuildingConstructed { get; init; }

    [JsonRequired]
    public bool SpellResearched { get; init; }

    [JsonRequired]
    public ImmutableSortedDictionary<WorldSpellDefinitionId, int> SpellCasts { get; init; } =
        ImmutableSortedDictionary<WorldSpellDefinitionId, int>.Empty;

    [JsonRequired]
    public ImmutableSortedDictionary<WorldItemId, int> ItemUses { get; init; } =
        ImmutableSortedDictionary<WorldItemId, int>.Empty;

    [JsonRequired]
    public ImmutableSortedSet<WorldPlayerId> MapExchangeProposals { get; init; } = [];

    public bool Equals(WorldPlayerTurnActions? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return BuildingConstructed == other.BuildingConstructed &&
               SpellResearched == other.SpellResearched &&
               SpellCasts.SequenceEqual(other.SpellCasts) &&
               ItemUses.SequenceEqual(other.ItemUses) &&
               MapExchangeProposals.SequenceEqual(other.MapExchangeProposals);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(BuildingConstructed);
        hash.Add(SpellResearched);
        hash.AddSequence(SpellCasts);
        hash.AddSequence(ItemUses);
        hash.AddSequence(MapExchangeProposals);

        return hash.ToHashCode();
    }
}
