using Gallean.Engine;

namespace Gallean.Client;

internal static class WorldPlayerFormatter
{
    public static string FormatLordName(IGameTextProvider texts, WorldPlayer player)
    {
        return texts.GetScenarioText(player.NameTextId);
    }

    public static string FormatRaceName(IGameDefinitionProvider definitions, IGameTextProvider texts, WorldPlayer player)
    {
        WorldRaceDefinition race = definitions.GetRace(player.Race);

        return texts.GetGlobalText(race.NameTextId).Text;
    }
}
