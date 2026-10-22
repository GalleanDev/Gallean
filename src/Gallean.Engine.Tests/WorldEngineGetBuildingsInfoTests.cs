namespace Gallean.Engine.Tests;

public sealed class WorldEngineGetBuildingsInfoTests
{
    private static WorldPlayerBuildingDefinition BuildingDefinition(
        WorldPlayerBuildingDefinitionId id,
        WorldPlayerBuildingDefinitionId? requiredId,
        UnitBranch? unitBranch,
        int level)
    {
        return TestHelpers.SimpleBuildingDefinition(id, WorldPlayerBuildingCategory.Unit) with
        {
            RequiredId = requiredId,
            UnitBranch = unitBranch,
            Level = level,
            Cost = new WorldResourceSet(Gold: 100, DeathMana: 0, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0)
        };
    }

    [Test]
    public void TreeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayerBuildingDefinitionId stableId = new("Stable");
        WorldPlayerBuildingDefinitionId dungeonId = new("Dungeon");
        WorldPlayerBuildingDefinitionId imperialStableId = new("ImperialStable");
        WorldPlayerBuildingDefinitionId tortureChamberId = new("TortureChamber");
        WorldPlayerBuildingDefinitionId shrineId = new("Shrine");
        WorldPlayerBuildingDefinitionId fountainId = new("Fountain");
        WorldPlayerBuildingDefinitionId templeId = new("Temple");

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(Gold: 500, DeathMana: 0, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0),
            Buildings = new WorldPlayerBuildings(
                [stableId, dungeonId, imperialStableId, tortureChamberId, shrineId, fountainId, templeId],
                [stableId])
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithDefinition(BuildingDefinition(stableId, null, UnitBranch.Fighter, 2))
            .WithDefinition(BuildingDefinition(dungeonId, null, UnitBranch.Fighter, 2))
            .WithDefinition(BuildingDefinition(imperialStableId, stableId, UnitBranch.Fighter, 3))
            .WithDefinition(BuildingDefinition(tortureChamberId, dungeonId, UnitBranch.Fighter, 3))
            .WithDefinition(BuildingDefinition(shrineId, imperialStableId, UnitBranch.Fighter, 4))
            .WithDefinition(BuildingDefinition(fountainId, imperialStableId, UnitBranch.Fighter, 4))
            .WithDefinition(BuildingDefinition(templeId, null, null, 2))
            .WithPlayer(player)
            .Build();

        WorldGetBuildingsInfoCommand.Result result = engine.CheckCommand(
            new WorldGetBuildingsInfoCommand(player.Id),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetBuildingsInfoCommand.Result([
            new WorldGetBuildingsInfoCommand.Building(dungeonId, WorldCommandError.BuildingBranchLocked, []),
            new WorldGetBuildingsInfoCommand.Building(fountainId, WorldCommandError.BuildingMissingPrerequisite, [shrineId]),
            new WorldGetBuildingsInfoCommand.Building(imperialStableId, null, []),
            new WorldGetBuildingsInfoCommand.Building(shrineId, WorldCommandError.BuildingMissingPrerequisite, [fountainId]),
            new WorldGetBuildingsInfoCommand.Building(stableId, WorldCommandError.BuildingAlreadyExists, []),
            new WorldGetBuildingsInfoCommand.Building(templeId, null, []),
            new WorldGetBuildingsInfoCommand.Building(tortureChamberId, WorldCommandError.BuildingBranchLocked, [])
        ])));
    }

    [Test]
    public void NotPlayerTurnTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .Build();

        engine.CheckCommandError(new WorldGetBuildingsInfoCommand(new WorldPlayerId(2)), WorldCommandError.NotPlayerTurn);
    }
}
