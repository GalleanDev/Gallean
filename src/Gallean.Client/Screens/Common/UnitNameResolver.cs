using Gallean.Engine;

namespace Gallean.Client;

internal static class UnitNameResolver
{
    public static string Resolve(WorldUnit unit, IGameDefinitionProvider definitions, IGameTextProvider texts)
    {
        if (unit.Name is null)
        {
            UnitDefinition definition = definitions.GetUnit(unit.DefinitionId);

            return texts.GetGlobalText(definition.NameTextId).Text;
        }
        else
        {
            return unit.Name;
        }
    }

    public static string Resolve(BattleUnit unit, IGameDefinitionProvider definitions, IGameTextProvider texts)
    {
        if (unit.Name is null)
        {
            UnitDefinition definition = definitions.GetUnit(unit.DefinitionId);

            return texts.GetGlobalText(definition.NameTextId).Text;
        }
        else
        {
            return unit.Name;
        }
    }
}
