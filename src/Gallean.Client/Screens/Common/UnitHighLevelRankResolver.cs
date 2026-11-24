using Gallean.Engine;

namespace Gallean.Client;

internal static class UnitHighLevelRankResolver
{
    public static int Resolve(UnitDefinition definition, int level)
    {
        int levels = level - definition.Level;

        if (definition.Level == 1)
        {
            levels++;
        }

        return levels switch
        {
            >= 15 => 3,
            >= 10 => 2,
            >= 5 => 1,
            _ => 0
        };
    }
}
