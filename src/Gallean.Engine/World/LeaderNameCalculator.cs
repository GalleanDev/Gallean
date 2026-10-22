using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

internal static class LeaderNameCalculator
{
    public static string? TryGetName(UnitDefinition definition, IGameDefinitionProvider definitions, IRandomProvider random)
    {
        if (definition.Category is UnitCategory.Leader or UnitCategory.NeutralLeader &&
            definition.Race != WorldRace.Neutral)
        {
            WorldRaceDefinition race = definitions.GetRace(definition.Race);
            ImmutableArray<string> names;

            if (definition.IsMale)
            {
                names = race.MaleLeaderNames;
            }
            else
            {
                names = race.FemaleLeaderNames;
            }

            if (!names.IsEmpty)
            {
                return names[random.Next(names.Length)];
            }
            else
            {
                return null;
            }
        }
        else
        {
            return null;
        }
    }
}
