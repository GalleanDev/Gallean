using Gallean.Common;
using Gallean.Engine;
using Gallean.Engine.Tests;

namespace Gallean.Client.Tests;

public sealed class WorldStackInfoViewModelTests
{
    private static IReadOnlyList<string> FormatLines(IReadOnlyList<TextSpans> lines)
    {
        List<string> rows = [];

        foreach (TextSpans line in lines)
        {
            rows.Add(line.Text);
        }

        return rows;
    }

    [Test]
    public void NamedLeaderTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 3) with
            {
                NameTextId = new GlobalTextId("Knight")
            };
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 35, initiative: 50, damage: 10) with
        {
            NameTextId = new GlobalTextId("Squire")
        };

        WorldUnit leader = new(new WorldUnitId("leader"), leaderDefinition.Id, 100, 0, leaderDefinition.Level, [], Name: "Ardobert", IsTypeLocked: false);
        WorldUnit soldier = new(new WorldUnitId("soldier"), soldierDefinition.Id, 30, 0, soldierDefinition.Level, [], null, false);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(soldierDefinition)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(4, 4),
                TestHelpers.StackUnitGrid(
                    stackDefinitions,
                    [
                        (leader, GridPosition.FrontBottom),
                        (soldier, GridPosition.FrontCenter)
                    ]),
                movementPoints: 20) with
            {
                BattlesWon = 2
            };

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        WorldStackInfoViewModel info = WorldStackInfoViewModel.Create(
            session.World.State,
            session.World.State.TurnInfo.CurrentPlayerId,
            stack,
            engine.Definitions,
            TestGameTextProvider.Instance,
            engine.UnitStats);

        List<WorldUnitGroupInfoUnitViewModel> expectedUnits =
        [
            new(
                new AssetSpriteKey.UnitFace(leaderDefinition.Id),
                "Ardobert",
                100,
                100,
                IsAlive: true,
                Row: 2,
                Column: 1,
                ColumnSpan: 1,
                Statuses: []),
            new(
                new AssetSpriteKey.UnitFace(soldierDefinition.Id),
                "Squire",
                30,
                35,
                IsAlive: true,
                Row: 1,
                Column: 1,
                ColumnSpan: 1,
                Statuses: [])
        ];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(info.LeaderPortraitKey, Is.EqualTo(new AssetSpriteKey.UnitEventPortrait(ScenarioEventPortraitId.Unit(leaderDefinition.Id))));
            Assert.That(info.LeaderName, Is.EqualTo("Ardobert"));
            Assert.That(info.RaceName, Is.EqualTo("Empire"));
            Assert.That(FormatLines(info.Lines), Is.EqualTo(["Battles won: 2"]));
            Assert.That(info.Note, Is.Null);
            Assert.That(info.Grid.Units, Is.EqualTo(expectedUnits));
        }
    }

    [Test]
    public void UnspiedEnemyStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 3) with
            {
                NameTextId = new GlobalTextId("Knight")
            };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            enemy.Id,
            new WorldPosition(4, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leaderDefinition),
            movementPoints: 20);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithDefinition(leaderDefinition)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        WorldStackInfoViewModel info = WorldStackInfoViewModel.Create(
            session.World.State,
            player.Id,
            stack,
            engine.Definitions,
            TestGameTextProvider.Instance,
            engine.UnitStats);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(info.LeaderName, Is.EqualTo("Knight"));
            Assert.That(info.RaceName, Is.EqualTo("Undead Hordes"));
            Assert.That(info.Lines, Is.Empty);
            Assert.That(info.Note, Is.EqualTo("A thief must spy on this race to gain information about it."));
            Assert.That(info.Grid.Units, Is.Empty);
        }
    }

    [Test]
    public void SpiedEnemyStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            SpiedPlayers = [new WorldPlayerId(2)]
        };
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 3) with
            {
                NameTextId = new GlobalTextId("Knight")
            };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            enemy.Id,
            new WorldPosition(4, 4),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leaderDefinition),
            movementPoints: 20);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithDefinition(leaderDefinition)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        WorldStackInfoViewModel info = WorldStackInfoViewModel.Create(
            session.World.State,
            player.Id,
            stack,
            engine.Definitions,
            TestGameTextProvider.Instance,
            engine.UnitStats);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(info.LeaderName, Is.EqualTo("Knight"));
            Assert.That(info.RaceName, Is.EqualTo("Undead Hordes"));
            Assert.That(FormatLines(info.Lines), Is.EqualTo(["Battles won: 0"]));
            Assert.That(info.Note, Is.Null);
            Assert.That(info.Grid.Units, Is.EqualTo(new List<WorldUnitGroupInfoUnitViewModel>
            {
                new(
                    new AssetSpriteKey.UnitFace(leaderDefinition.Id),
                    "Knight",
                    100,
                    100,
                    IsAlive: true,
                    Row: 2,
                    Column: 1,
                    ColumnSpan: 1,
                    Statuses: [])
            }));
        }
    }

    [Test]
    public void UnitModifierTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitModifierDefinition armor = TestHelpers.SimpleModifier("armor") with { Armor = new UnitStatModifier(10, 0) };
        UnitModifierDefinition swiftness = TestHelpers.SimpleModifier("swiftness") with { Initiative = new UnitStatModifier(0, 20) };
        UnitModifierDefinition curse = TestHelpers.SimpleModifier("curse") with { Damage = new UnitStatModifier(0, -20) };
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 20, leadership: 3) with
            {
                NameTextId = new GlobalTextId("Knight")
            };

        WorldUnit leader = new(
            new WorldUnitId("leader"),
            leaderDefinition.Id,
            100,
            0,
            leaderDefinition.Level,
            [
                UnitModifier.Permanent(armor.Id),
                new UnitModifier(swiftness.Id, RemainingTurns: 2),
                new UnitModifier(curse.Id, RemainingTurns: 1)
            ],
            null,
            false);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnitModifier(armor)
            .AddUnitModifier(swiftness)
            .AddUnitModifier(curse)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(4, 4),
            TestHelpers.StackUnitGrid(stackDefinitions, [(leader, GridPosition.FrontBottom)]),
            movementPoints: 20);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(leaderDefinition)
            .WithDefinition(armor)
            .WithDefinition(swiftness)
            .WithDefinition(curse)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        WorldStackInfoViewModel info = WorldStackInfoViewModel.Create(
            session.World.State,
            player.Id,
            stack,
            engine.Definitions,
            TestGameTextProvider.Instance,
            engine.UnitStats);

        Assert.That(info.Grid.Units, Is.EqualTo(new List<WorldUnitGroupInfoUnitViewModel>
        {
            new(
                new AssetSpriteKey.UnitFace(leaderDefinition.Id),
                "Knight",
                100,
                100,
                IsAlive: true,
                Row: 2,
                Column: 1,
                ColumnSpan: 1,
                Statuses:
                [
                    new UnitModifierStatusViewModel(UnitModifierType.Weaken,
                        [new UnitModifierViewModel("-20% damage", TrSpans("Turns left: {0:*}", 1), UnitModifierType.Weaken)]),
                    new UnitModifierStatusViewModel(UnitModifierType.Boost,
                        [new UnitModifierViewModel("+20% initiative", TrSpans("Turns left: {0:*}", 2), UnitModifierType.Boost)])
                ])
        }));
    }
}
