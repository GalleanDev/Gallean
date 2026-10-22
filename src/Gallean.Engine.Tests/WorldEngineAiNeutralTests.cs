using Gallean.Common;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineAiNeutralTests
{
    [Test]
    public void StrategyTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 4);

        UnitDefinition main = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("main", maxHitpoints: 100, initiative: 50, damage: 50),
            movementPoints: 6, leadership: 6);
        UnitDefinition weak = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 10, initiative: 40, damage: 5),
                movementPoints: 6, leadership: 6) with
            {
                ExperienceKilled = 50
            };

        WorldPlayerBuildingDefinition barracks = new(
            Id: new WorldPlayerBuildingDefinitionId("barracks"),
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Guild,
            UnitBranch: null,
            Level: 1,
            Cost: WorldResourceSet.Empty,
            UnitIds: []);

        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Neutral, map) with
        {
            IsAi = true,
            Buildings = new WorldPlayerBuildings([barracks.Id], [])
        };
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            neutral.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("main"), main), GridPosition.BackBottom, false)
                .Build());
        WorldStack guardStack = TestHelpers.SimpleStack(
                new WorldStackId("Guard"),
                neutral.Id,
                city.Entrance,
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("main2"), main) with { Hitpoints = 50 }, GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 6) with
            {
                Order = WorldStackOrder.Guard.Instance
            };
        WorldStack weakStack = TestHelpers.SimpleStack(
            new WorldStackId("Weak"),
            player.Id,
            new WorldPosition(4, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("weak"), weak), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 0);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(weak)
            .WithDefinition(barracks)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(guardStack)
            .WithStack(weakStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 877] Move Guard -> (4, 3) (Attacker wins)"
        ]));
        Assert.That(session.World.State.Players[neutral.Id], Is.EqualTo(neutral));
        Assert.That(session.World.State.Fortifications[city.Id], Is.EqualTo(city));
    }

    [Test]
    public void OrdersTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 1);

        UnitDefinition main = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("main", maxHitpoints: 100, initiative: 50, damage: 50),
            movementPoints: 6, leadership: 6);
        UnitDefinition weak = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Neutral, map) with { IsAi = true };
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack standStack = TestHelpers.SimpleStack(
                new WorldStackId("Stand"),
                neutral.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("main"), main), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 6) with
            {
                Order = WorldStackOrder.Stand.Instance
            };
        WorldStack berserkStack = TestHelpers.SimpleStack(
                new WorldStackId("Berserk"),
                neutral.Id,
                new WorldPosition(4, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("main2"), main), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 6) with
            {
                Order = WorldStackOrder.Berserk.Instance
            };
        WorldStack weakStack = TestHelpers.SimpleStack(
            new WorldStackId("Weak"),
            player.Id,
            new WorldPosition(1, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("weak"), weak), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 0);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(weak)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(standStack)
            .WithStack(berserkStack)
            .WithStack(weakStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 910] Move Berserk -> (1, 0) (Attacker wins)"
        ]));
        Assert.That(session.World.State.Stacks[standStack.Id], Is.EqualTo(standStack));
    }

    [Test]
    public void GuardAttackStrongerTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition weak = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);
        UnitDefinition strong = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("strong", maxHitpoints: 100, initiative: 50, damage: 50),
            movementPoints: 6, leadership: 6);

        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Neutral, map) with { IsAi = true };
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack guardStack = TestHelpers.SimpleStack(
                new WorldStackId("Guard"),
                neutral.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("weak"), weak), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 6) with
            {
                Order = WorldStackOrder.Guard.Instance
            };
        WorldStack strongStack = TestHelpers.SimpleStack(
            new WorldStackId("Strong"),
            player.Id,
            new WorldPosition(1, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("strong"), strong), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 0);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDifficulty(WorldDifficulty.Hard)
            .WithDefinition(weak)
            .WithDefinition(strong)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(guardStack)
            .WithStack(strongStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 89] Move Guard -> (1, 0) (Defender wins)"
        ]));
    }

    [Test]
    public void NormalWithinPowerMarginTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition attacker = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 60, damage: 100),
            movementPoints: 6, leadership: 6);
        UnitDefinition defender = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 50, damage: 60),
            movementPoints: 6, leadership: 6);

        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Neutral, map) with { IsAi = true };
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack neutralStack = TestHelpers.SimpleStack(
                new WorldStackId("Neutral"),
                neutral.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("attacker"), attacker), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 6) with
            {
                Order = WorldStackOrder.Normal.Instance
            };
        WorldStack playerStack = TestHelpers.SimpleStack(
            new WorldStackId("Player"),
            player.Id,
            new WorldPosition(1, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defender), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 0);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(attacker)
            .WithDefinition(defender)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(neutralStack)
            .WithStack(playerStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.Empty);
        Assert.That(session.World.State.Stacks[neutralStack.Id], Is.EqualTo(neutralStack));
    }

    [Test]
    public void RoamWithinPowerMarginTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition attacker = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 60, damage: 100),
            movementPoints: 6, leadership: 6);
        UnitDefinition defender = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 50, damage: 60),
            movementPoints: 6, leadership: 6);

        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Neutral, map) with { IsAi = true };
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack neutralStack = TestHelpers.SimpleStack(
                new WorldStackId("Neutral"),
                neutral.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("attacker"), attacker), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 6) with
            {
                Order = WorldStackOrder.Roam.Instance
            };
        WorldStack playerStack = TestHelpers.SimpleStack(
            new WorldStackId("Player"),
            player.Id,
            new WorldPosition(1, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defender), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 0);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(attacker)
            .WithDefinition(defender)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(neutralStack)
            .WithStack(playerStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.Empty);
        Assert.That(session.World.State.Stacks[neutralStack.Id], Is.EqualTo(neutralStack));
    }

    [Test]
    public void RoamBeyondPowerMarginTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition attacker = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 60, damage: 100),
            movementPoints: 6, leadership: 6);
        UnitDefinition defender = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 50, damage: 50),
            movementPoints: 6, leadership: 6);

        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Neutral, map) with { IsAi = true };
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack neutralStack = TestHelpers.SimpleStack(
                new WorldStackId("Neutral"),
                neutral.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("attacker"), attacker), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 6) with
            {
                Order = WorldStackOrder.Roam.Instance
            };
        WorldStack playerStack = TestHelpers.SimpleStack(
            new WorldStackId("Player"),
            player.Id,
            new WorldPosition(1, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defender), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 0);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(attacker)
            .WithDefinition(defender)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(neutralStack)
            .WithStack(playerStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 547] Move Neutral -> (1, 0) (Attacker wins)"
        ]));
    }

    [Test]
    public void BerserkWithinPowerMarginTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition attacker = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 60, damage: 100),
            movementPoints: 6, leadership: 6);
        UnitDefinition defender = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 100, initiative: 50, damage: 60),
            movementPoints: 6, leadership: 6);

        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Neutral, map) with { IsAi = true };
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack neutralStack = TestHelpers.SimpleStack(
                new WorldStackId("Neutral"),
                neutral.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("attacker"), attacker), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 6) with
            {
                Order = WorldStackOrder.Berserk.Instance
            };
        WorldStack playerStack = TestHelpers.SimpleStack(
            new WorldStackId("Player"),
            player.Id,
            new WorldPosition(1, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defender), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 0);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(attacker)
            .WithDefinition(defender)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(neutralStack)
            .WithStack(playerStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 564] Move Neutral -> (1, 0) (Attacker wins)"
        ]));
    }

    [Test]
    public void BerserkKeepTargetTest()
    {
        WorldMap map = TestHelpers.SimpleMap(40, 1);

        UnitDefinition berserker = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("berserker", maxHitpoints: 100, initiative: 50, damage: 50),
            movementPoints: 4, leadership: 6);
        UnitDefinition weak = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 20, leadership: 6);

        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Neutral, map) with { IsAi = true };
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack berserkStack = TestHelpers.SimpleStack(
                new WorldStackId("Berserk"),
                neutral.Id,
                new WorldPosition(20, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("berserker"), berserker), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 4) with
            {
                Order = WorldStackOrder.Berserk.Instance
            };
        WorldStack farStack = TestHelpers.SimpleStack(
            new WorldStackId("Far"),
            player.Id,
            new WorldPosition(30, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("weak"), weak), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 0);
        WorldStack nearStack = TestHelpers.SimpleStack(
            new WorldStackId("Near"),
            player.Id,
            new WorldPosition(6, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("weak2"), weak), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(berserker)
            .WithDefinition(weak)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(berserkStack)
            .WithStack(farStack)
            .WithStack(nearStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P1 1000] Move Berserk -> (22, 0)"
        ]));

        WorldCommandResult approachResult = session.World.DispatchCommand(new WorldMoveStackCommand(nearStack.Id, new WorldPosition(16, 0)));
        Assert.That(approachResult.Error, Is.Null);

        WorldCommandResult endTurnResult = session.World.DispatchCommand(new WorldEndTurnCommand());
        Assert.That(endTurnResult.Error, Is.Null);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P1 1000] Move Berserk -> (24, 0)"
        ]));
    }

    [Test]
    public void BerserkRetargetTest()
    {
        WorldMap map = TestHelpers.SimpleMap(40, 1);

        UnitDefinition berserker = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("berserker", maxHitpoints: 100, initiative: 50, damage: 50),
            movementPoints: 4, leadership: 6);
        UnitDefinition weak = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 40, leadership: 6);

        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Neutral, map) with { IsAi = true };
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack berserkStack = TestHelpers.SimpleStack(
                new WorldStackId("Berserk"),
                neutral.Id,
                new WorldPosition(20, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("berserker"), berserker), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 4) with
            {
                Order = WorldStackOrder.Berserk.Instance
            };
        WorldStack preyStack = TestHelpers.SimpleStack(
            new WorldStackId("Prey"),
            player.Id,
            new WorldPosition(22, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("weak"), weak), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 0);
        WorldStack nearStack = TestHelpers.SimpleStack(
            new WorldStackId("Near"),
            player.Id,
            new WorldPosition(10, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("weak2"), weak), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 0);
        WorldStack otherStack = TestHelpers.SimpleStack(
            new WorldStackId("Other"),
            player.Id,
            new WorldPosition(39, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("weak3"), weak), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 40);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(berserker)
            .WithDefinition(weak)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(berserkStack)
            .WithStack(preyStack)
            .WithStack(nearStack)
            .WithStack(otherStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 910] Move Berserk -> (22, 0) (Attacker wins)"
        ]));

        WorldCommandResult firstEndTurnResult = session.World.DispatchCommand(new WorldEndTurnCommand());
        Assert.That(firstEndTurnResult.Error, Is.Null);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P1 1000] Move Berserk -> (19, 0)"
        ]));

        WorldCommandResult approachResult = session.World.DispatchCommand(new WorldMoveStackCommand(otherStack.Id, new WorldPosition(24, 0)));
        Assert.That(approachResult.Error, Is.Null);

        WorldCommandResult secondEndTurnResult = session.World.DispatchCommand(new WorldEndTurnCommand());
        Assert.That(secondEndTurnResult.Error, Is.Null);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P1 1000] Move Berserk -> (17, 0)"
        ]));
    }
}
