using Gallean.Common;

namespace Gallean.Engine;

internal static class LeaderNameCalculator
{
    public static UnitName? TryGetName(UnitDefinition definition, IGameDefinitionProvider definitions, IRandomProvider random)
    {
        if (definition.Category is UnitCategory.Leader or UnitCategory.NeutralLeader &&
            definition.Race != WorldRace.Neutral)
        {
            WorldRaceDefinition race = definitions.GetRace(definition.Race);
            int count = definition.IsMale ? race.MaleLeaderNameCount : race.FemaleLeaderNameCount;

            if (count > 0)
            {
                return new LeaderUnitName(new LeaderNameId(definition.Race, definition.IsMale, random.Next(count)));
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
