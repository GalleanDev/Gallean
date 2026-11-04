namespace Gallean.Engine.Tests;

public sealed class WorldEngineAiAutoExperienceTests
{
    [Test]
    public void StackSimpleTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 1);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 4, leadership: 6);
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 50, initiative: 40, damage: 5);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar(["......??"])
        };
        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            IsAi = true
        };

        WorldStack aiStack = TestHelpers.SimpleStack(
            new WorldStackId("Ai"),
            ai.Id,
            new WorldPosition(7, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Experience = 100 }, GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier), GridPosition.FrontTop, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("dead"), soldier) with { Hitpoints = 0 }, GridPosition.BackBottom, false)
                .Build(),
            movementPoints: 4);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(soldier)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithStack(aiStack)
            .Build();

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(human.Id),
            new WorldTurnStartingEvent(ai.Id),
            new WorldStackMovementRestoredEvent(aiStack.Id, 4),
            new WorldTurnStartedEvent(ai.Id),
            new WorldTurnResourcesGeneratedEvent(ai.Id, new WorldResourceSet(100, 0, 0, 0, 25, 0)),
            new WorldUnitExperienceGainedEvent(new WorldUnitGroup.Stack(aiStack.Id), new WorldUnitId("leader"), 20),
            new WorldUnitExperienceGainedEvent(new WorldUnitGroup.Stack(aiStack.Id), new WorldUnitId("soldier"), 20)
        ]);

        UnitGrid<WorldUnit> expectedUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Experience = 120 }, GridPosition.FrontBottom, false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier) with { Experience = 20 }, GridPosition.FrontTop, false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("dead"), soldier) with { Hitpoints = 0 }, GridPosition.BackBottom, false)
            .Build();

        Assert.That(engine.State.Stacks[aiStack.Id], Is.EqualTo(aiStack with { Units = expectedUnits }));
    }

    [Test]
    public void StackExploredTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 1);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 4, leadership: 6);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar([".......?"])
        };
        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            IsAi = true
        };

        WorldStack aiStack = TestHelpers.SimpleStack(
            new WorldStackId("Ai"),
            ai.Id,
            new WorldPosition(7, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 4);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithStack(aiStack)
            .Build();

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(human.Id),
            new WorldTurnStartingEvent(ai.Id),
            new WorldStackMovementRestoredEvent(aiStack.Id, 4),
            new WorldTurnStartedEvent(ai.Id),
            new WorldTurnResourcesGeneratedEvent(ai.Id, new WorldResourceSet(100, 0, 0, 0, 25, 0))
        ]);

        Assert.That(engine.State.Stacks[aiStack.Id], Is.EqualTo(aiStack));
    }

    [Test]
    public void GarrisonTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 5);

        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 50, initiative: 40, damage: 5);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar(["..??????", "..??????", "..??????", "..??????", "..??????"])
        };
        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            IsAi = true
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            ai.Id,
            new WorldPosition(7, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier), GridPosition.FrontBottom, false)
                .Build());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(soldier)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithCapital(capital)
            .Build();

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(human.Id),
            new WorldTurnStartingEvent(ai.Id),
            new WorldTurnStartedEvent(ai.Id),
            new WorldTurnResourcesGeneratedEvent(ai.Id, new WorldResourceSet(100, 0, 0, 0, 25, 0)),
            new WorldUnitExperienceGainedEvent(new WorldUnitGroup.Fortification(capital.Id), new WorldUnitId("soldier"), 40)
        ]);

        UnitGrid<WorldUnit> expectedGarrison = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier) with { Experience = 40 }, GridPosition.FrontBottom, false)
            .Build();

        Assert.That(engine.State.Fortifications[capital.Id], Is.EqualTo(capital with { Garrison = expectedGarrison }));
    }

    [Test]
    public void DifficultyTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 1);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 4, leadership: 6);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar(["......??"])
        };
        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            IsAi = true
        };

        WorldStack aiStack = TestHelpers.SimpleStack(
            new WorldStackId("Ai"),
            ai.Id,
            new WorldPosition(7, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 4);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDifficulty(WorldDifficulty.VeryHard)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithStack(aiStack)
            .Build();

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(human.Id),
            new WorldTurnStartingEvent(ai.Id),
            new WorldStackMovementRestoredEvent(aiStack.Id, 4),
            new WorldTurnStartedEvent(ai.Id),
            new WorldTurnResourcesGeneratedEvent(ai.Id, new WorldResourceSet(145, 0, 0, 0, 36, 0)),
            new WorldUnitExperienceGainedEvent(new WorldUnitGroup.Stack(aiStack.Id), new WorldUnitId("leader"), 60)
        ]);

        UnitGrid<WorldUnit> expectedUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Experience = 60 }, GridPosition.FrontBottom, false)
            .Build();

        Assert.That(engine.State.Stacks[aiStack.Id], Is.EqualTo(aiStack with { Units = expectedUnits }));
    }

    [Test]
    public void UpgradeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 1);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 4, leadership: 6);
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 50, initiative: 40, damage: 5);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar(["......??"])
        };
        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            IsAi = true
        };

        WorldStack aiStack = TestHelpers.SimpleStack(
            new WorldStackId("Ai"),
            ai.Id,
            new WorldPosition(7, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier) with { Hitpoints = 20, Experience = 990 }, GridPosition.FrontTop, false)
                .Build(),
            movementPoints: 4);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(soldier)
            .WithUnitDynamicUpgrade(new UnitDynamicUpgradeDefinition(
                soldier.Id,
                UpgradeChangeLevel: 3,
                UnitDynamicUpgrade.Empty with { MaxHitpoints = 10, ExperienceNextLevel = 100 },
                UnitDynamicUpgrade.Empty with { MaxHitpoints = 5, ExperienceNextLevel = 50 }))
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithStack(aiStack)
            .Build();

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(human.Id),
            new WorldTurnStartingEvent(ai.Id),
            new WorldStackMovementRestoredEvent(aiStack.Id, 4),
            new WorldTurnStartedEvent(ai.Id),
            new WorldTurnResourcesGeneratedEvent(ai.Id, new WorldResourceSet(100, 0, 0, 0, 25, 0)),
            new WorldUnitExperienceGainedEvent(new WorldUnitGroup.Stack(aiStack.Id), new WorldUnitId("leader"), 20),
            new WorldUnitUpgradedEvent(new WorldUnitGroup.Stack(aiStack.Id), new WorldUnitId("soldier"), soldier.Id, 2, 60)
        ]);

        UnitGrid<WorldUnit> expectedUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Experience = 20 }, GridPosition.FrontBottom, false)
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier) with { Hitpoints = 60, Level = 2 }, GridPosition.FrontTop, false)
            .Build();

        Assert.That(engine.State.Stacks[aiStack.Id], Is.EqualTo(aiStack with { Units = expectedUnits }));
    }

    [Test]
    public void NeutralTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 1);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 4, leadership: 6);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar(["......??"])
        };
        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Neutral, map) with
        {
            IsAi = true
        };

        WorldStack neutralStack = TestHelpers.SimpleStack(
            new WorldStackId("Neutral"),
            neutral.Id,
            new WorldPosition(7, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 4);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithSuggestedLevel(9)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithStack(neutralStack)
            .Build();

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(human.Id),
            new WorldTurnStartingEvent(neutral.Id),
            new WorldStackMovementRestoredEvent(neutralStack.Id, 4),
            new WorldTurnStartedEvent(neutral.Id),
            new WorldTurnResourcesGeneratedEvent(neutral.Id, WorldResourceSet.Empty),
            new WorldUnitExperienceGainedEvent(new WorldUnitGroup.Stack(neutralStack.Id), new WorldUnitId("leader"), 20)
        ]);

        UnitGrid<WorldUnit> expectedUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader) with { Experience = 20 }, GridPosition.FrontBottom, false)
            .Build();

        Assert.That(engine.State.Stacks[neutralStack.Id], Is.EqualTo(neutralStack with { Units = expectedUnits }));
    }
}
