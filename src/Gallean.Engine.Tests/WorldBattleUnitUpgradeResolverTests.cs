namespace Gallean.Engine.Tests;

public sealed class WorldBattleUnitUpgradeResolverTests
{
    [Test]
    public void LevelUpWithoutUpgradePathTest()
    {
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 10, initiative: 50, damage: 5);
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, TestHelpers.SimpleMap(1, 1));

        IEngineContext context = new TestWorldEngineBuilder()
            .WithMap(TestHelpers.SimpleMap(1, 1))
            .WithDefinition(soldierDefinition)
            .WithUnitDynamicUpgrade(new UnitDynamicUpgradeDefinition(
                soldierDefinition.Id,
                UpgradeChangeLevel: 3,
                UnitDynamicUpgrade.Empty with { MaxHitpoints = 10, Damage = 3, ExperienceKilled = 2, ExperienceNextLevel = 5 },
                UnitDynamicUpgrade.Empty with { MaxHitpoints = 5, Damage = 1, ExperienceKilled = 1, ExperienceNextLevel = 5 }))
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .BuildContext();

        WorldBattleUnitUpgradeResolver resolver = new(new WorldPlayerBuildings([], []), new WorldPlayerBuildings([], []), context);

        BattleUnit unit = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, soldierDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        Assert.That(resolver.UpgradeUnit(unit), Is.EqualTo(new BattleUnitUpgrade(soldierDefinition.Id, 2, 20, 20)));
    }

    [Test]
    public void BranchUpgradeRequiresBuildingTest()
    {
        UnitDefinition squireDefinition = TestHelpers.SimpleUnitDefinition("squire", maxHitpoints: 10, initiative: 50, damage: 5);
        UnitDefinition knightDefinition = TestHelpers.SimpleUnitDefinition("knight", maxHitpoints: 20, initiative: 60, damage: 10);
        WorldPlayerBuildingDefinitionId barracksId = new("barracks");

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, TestHelpers.SimpleMap(1, 1));

        IEngineContext context = new TestWorldEngineBuilder()
            .WithMap(TestHelpers.SimpleMap(1, 1))
            .WithDefinition(squireDefinition)
            .WithDefinition(knightDefinition)
            .WithUnitUpgrade(new UnitUpgradeDefinition(squireDefinition.Id, knightDefinition.Id, barracksId))
            .WithDefinition(TestHelpers.SimpleBuildingDefinition(barracksId, WorldPlayerBuildingCategory.Unit))
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .BuildContext();

        WorldBattleUnitUpgradeResolver resolver = new(new WorldPlayerBuildings([barracksId], [barracksId]), new WorldPlayerBuildings([barracksId], []), context);

        BattleUnit attacker = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, squireDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        Assert.That(resolver.UpgradeUnit(attacker), Is.EqualTo(new BattleUnitUpgrade(knightDefinition.Id, 1, 20, 20)));

        BattleUnit defender = attacker with { Side = BattleSide.Defender };
        Assert.That(resolver.UpgradeUnit(defender), Is.Null);
    }

    [Test]
    public void LeaderLevelCapTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 5, leadership: 3);
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, TestHelpers.SimpleMap(1, 1));

        IEngineContext context = new TestWorldEngineBuilder()
            .WithMap(TestHelpers.SimpleMap(1, 1))
            .WithDefinition(leaderDefinition)
            .WithUnitDynamicUpgrade(new UnitDynamicUpgradeDefinition(
                leaderDefinition.Id,
                UpgradeChangeLevel: 3,
                UnitDynamicUpgrade.Empty with { MaxHitpoints = 10, Damage = 3, ExperienceKilled = 2, ExperienceNextLevel = 5 },
                UnitDynamicUpgrade.Empty with { MaxHitpoints = 5, Damage = 1, ExperienceKilled = 1, ExperienceNextLevel = 5 }))
            .WithMaxLeaderLevel(2)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .BuildContext();

        WorldBattleUnitUpgradeResolver resolver = new(new WorldPlayerBuildings([], []), new WorldPlayerBuildings([], []), context);

        BattleUnit belowCap = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, leaderDefinition.Id, BattleSide.Attacker, hitpoints: 100);
        Assert.That(resolver.UpgradeUnit(belowCap), Is.EqualTo(new BattleUnitUpgrade(leaderDefinition.Id, 2, 110, 110)));

        BattleUnit atCap = belowCap with { Level = 2 };
        Assert.That(resolver.UpgradeUnit(atCap), Is.Null);
    }

    [Test]
    public void SoldierLeaderLevelCapTest()
    {
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 10, initiative: 50, damage: 5);
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, TestHelpers.SimpleMap(1, 1));

        IEngineContext context = new TestWorldEngineBuilder()
            .WithMap(TestHelpers.SimpleMap(1, 1))
            .WithDefinition(soldierDefinition)
            .WithUnitDynamicUpgrade(new UnitDynamicUpgradeDefinition(
                soldierDefinition.Id,
                UpgradeChangeLevel: 3,
                UnitDynamicUpgrade.Empty with { MaxHitpoints = 10, Damage = 3, ExperienceKilled = 2, ExperienceNextLevel = 5 },
                UnitDynamicUpgrade.Empty with { MaxHitpoints = 5, Damage = 1, ExperienceKilled = 1, ExperienceNextLevel = 5 }))
            .WithMaxLeaderLevel(2)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .BuildContext();

        WorldBattleUnitUpgradeResolver resolver = new(new WorldPlayerBuildings([], []), new WorldPlayerBuildings([], []), context);

        BattleUnit unit = new(new BattleUnitId(0), null, soldierDefinition.Id, BattleSide.Attacker, 10, 0, 0, 2, [], [], null, [], null, false);
        Assert.That(resolver.UpgradeUnit(unit), Is.EqualTo(new BattleUnitUpgrade(soldierDefinition.Id, 3, 30, 30)));
    }

    [Test]
    public void UnavailableUpgradeBuildingTest()
    {
        UnitDefinition squireDefinition = TestHelpers.SimpleUnitDefinition("squire", maxHitpoints: 10, initiative: 50, damage: 5);
        UnitDefinition knightDefinition = TestHelpers.SimpleUnitDefinition("knight", maxHitpoints: 20, initiative: 60, damage: 10);
        WorldPlayerBuildingDefinitionId barracksId = new("barracks");

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, TestHelpers.SimpleMap(1, 1));

        IEngineContext context = new TestWorldEngineBuilder()
            .WithMap(TestHelpers.SimpleMap(1, 1))
            .WithDefinition(squireDefinition)
            .WithDefinition(knightDefinition)
            .WithUnitDynamicUpgrade(new UnitDynamicUpgradeDefinition(
                squireDefinition.Id,
                UpgradeChangeLevel: 3,
                UnitDynamicUpgrade.Empty with { MaxHitpoints = 10, Damage = 3, ExperienceKilled = 2, ExperienceNextLevel = 5 },
                UnitDynamicUpgrade.Empty with { MaxHitpoints = 5, Damage = 1, ExperienceKilled = 1, ExperienceNextLevel = 5 }))
            .WithUnitUpgrade(new UnitUpgradeDefinition(squireDefinition.Id, knightDefinition.Id, barracksId))
            .WithDefinition(TestHelpers.SimpleBuildingDefinition(barracksId, WorldPlayerBuildingCategory.Unit))
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .BuildContext();

        WorldBattleUnitUpgradeResolver resolver = new(new WorldPlayerBuildings([], []), new WorldPlayerBuildings([], []), context);

        BattleUnit unit = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, squireDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        Assert.That(resolver.UpgradeUnit(unit), Is.EqualTo(new BattleUnitUpgrade(squireDefinition.Id, 2, 20, 20)));
    }

    [Test]
    public void BranchLockedUpgradeBuildingTest()
    {
        UnitDefinition squireDefinition = TestHelpers.SimpleUnitDefinition("squire", maxHitpoints: 10, initiative: 50, damage: 5);
        UnitDefinition knightDefinition = TestHelpers.SimpleUnitDefinition("knight", maxHitpoints: 20, initiative: 60, damage: 10);
        WorldPlayerBuildingDefinitionId barracksId = new("barracks");
        WorldPlayerBuildingDefinitionId chapelId = new("chapel");

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, TestHelpers.SimpleMap(1, 1));

        IEngineContext context = new TestWorldEngineBuilder()
            .WithMap(TestHelpers.SimpleMap(1, 1))
            .WithDefinition(squireDefinition)
            .WithDefinition(knightDefinition)
            .WithUnitDynamicUpgrade(new UnitDynamicUpgradeDefinition(
                squireDefinition.Id,
                UpgradeChangeLevel: 3,
                UnitDynamicUpgrade.Empty with { MaxHitpoints = 10, Damage = 3, ExperienceKilled = 2, ExperienceNextLevel = 5 },
                UnitDynamicUpgrade.Empty with { MaxHitpoints = 5, Damage = 1, ExperienceKilled = 1, ExperienceNextLevel = 5 }))
            .WithUnitUpgrade(new UnitUpgradeDefinition(squireDefinition.Id, knightDefinition.Id, barracksId))
            .WithDefinition(TestHelpers.SimpleBuildingDefinition(barracksId, WorldPlayerBuildingCategory.Unit) with { UnitBranch = UnitBranch.Fighter, Level = 2 })
            .WithDefinition(TestHelpers.SimpleBuildingDefinition(chapelId, WorldPlayerBuildingCategory.Unit) with { UnitBranch = UnitBranch.Fighter, Level = 2 })
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .BuildContext();

        WorldBattleUnitUpgradeResolver resolver = new(new WorldPlayerBuildings([barracksId, chapelId], [chapelId]), new WorldPlayerBuildings([], []), context);

        BattleUnit unit = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, squireDefinition.Id, BattleSide.Attacker, hitpoints: 10);
        Assert.That(resolver.UpgradeUnit(unit), Is.EqualTo(new BattleUnitUpgrade(squireDefinition.Id, 2, 20, 20)));
    }

    [Test]
    public void TypeLockedTest()
    {
        UnitDefinition squireDefinition = TestHelpers.SimpleUnitDefinition("squire", maxHitpoints: 10, initiative: 50, damage: 5);
        UnitDefinition knightDefinition = TestHelpers.SimpleUnitDefinition("knight", maxHitpoints: 20, initiative: 60, damage: 10);
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, TestHelpers.SimpleMap(1, 1));

        IEngineContext context = new TestWorldEngineBuilder()
            .WithMap(TestHelpers.SimpleMap(1, 1))
            .WithDefinition(squireDefinition)
            .WithDefinition(knightDefinition)
            .WithUnitUpgrade(new UnitUpgradeDefinition(squireDefinition.Id, knightDefinition.Id, RequiredBuildingId: null))
            .WithUnitDynamicUpgrade(new UnitDynamicUpgradeDefinition(
                squireDefinition.Id,
                UpgradeChangeLevel: 3,
                UnitDynamicUpgrade.Empty with { MaxHitpoints = 10, Damage = 3, ExperienceKilled = 2, ExperienceNextLevel = 5 },
                UnitDynamicUpgrade.Empty with { MaxHitpoints = 5, Damage = 1, ExperienceKilled = 1, ExperienceNextLevel = 5 }))
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .BuildContext();

        WorldBattleUnitUpgradeResolver resolver = new(new WorldPlayerBuildings([], []), new WorldPlayerBuildings([], []), context);

        BattleUnit unit = TestHelpers.SimpleBattleUnit(new BattleUnitId(0), null, squireDefinition.Id, BattleSide.Attacker, hitpoints: 10) with { IsTypeLocked = true };
        Assert.That(resolver.UpgradeUnit(unit), Is.EqualTo(new BattleUnitUpgrade(squireDefinition.Id, 2, 20, 20)));
    }
}
