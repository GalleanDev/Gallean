namespace Gallean.Engine;

internal static class WorldHelpers
{
    public static bool IsRacePlayable(WorldRace race)
    {
        return race switch
        {
            WorldRace.Human => true,
            WorldRace.Undead => true,
            WorldRace.Heretic => true,
            WorldRace.Dwarf => true,
            WorldRace.Elf => true,
            WorldRace.Neutral => false
        };
    }

    public static WorldMapTerrain GetNativeTerrain(WorldRace race)
    {
        return race switch
        {
            WorldRace.Human => WorldMapTerrain.Human,
            WorldRace.Undead => WorldMapTerrain.Undead,
            WorldRace.Heretic => WorldMapTerrain.Heretic,
            WorldRace.Dwarf => WorldMapTerrain.Dwarf,
            WorldRace.Neutral => WorldMapTerrain.Neutral,
            WorldRace.Elf => WorldMapTerrain.Elf
        };
    }
}
