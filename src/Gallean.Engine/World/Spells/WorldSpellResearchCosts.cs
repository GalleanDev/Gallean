using System.Collections.Frozen;

namespace Gallean.Engine;

internal sealed class WorldSpellResearchCosts
{
    private readonly FrozenDictionary<WorldRace, FrozenDictionary<WorldSpellDefinitionId, WorldResourceSet>> costsByRace;

    public WorldSpellResearchCosts(IEnumerable<WorldSpellResearchCost> research)
    {
        Dictionary<WorldRace, Dictionary<WorldSpellDefinitionId, WorldResourceSet>> builder = [];

        foreach (WorldSpellResearchCost entry in research)
        {
            if (!builder.TryGetValue(entry.Race, out Dictionary<WorldSpellDefinitionId, WorldResourceSet>? costs))
            {
                costs = [];
                builder[entry.Race] = costs;
            }

            costs[entry.SpellId] = entry.Cost;
        }

        costsByRace = builder.ToFrozenDictionary(
            entry => entry.Key,
            entry => entry.Value.ToFrozenDictionary());
    }

    public IReadOnlyDictionary<WorldSpellDefinitionId, WorldResourceSet> GetSpellResearchCosts(WorldRace race)
    {
        if (costsByRace.TryGetValue(race, out FrozenDictionary<WorldSpellDefinitionId, WorldResourceSet>? costs))
        {
            return costs;
        }

        return FrozenDictionary<WorldSpellDefinitionId, WorldResourceSet>.Empty;
    }
}
