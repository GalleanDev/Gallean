using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineAiTests
{
    private static UnitDefinition Leader(string id, int damage, int movementPoints)
    {
        return TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition(id, maxHitpoints: 100, initiative: 50, damage: damage),
            movementPoints: movementPoints, leadership: 6);
    }

    private static WorldStack MyStack(WorldUnitId leaderId, string id, WorldPlayerId ownerId, WorldPosition position, UnitDefinition leader, int hitpoints, int movementPoints, WorldCanonicalStackOrder order)
    {
        return TestHelpers.SimpleStack(
                new WorldStackId(id),
                ownerId,
                position,
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(leaderId, leader) with { Hitpoints = hitpoints }, GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: movementPoints) with
            {
                Order = order
            };
    }

    private static WorldStack EnemyStack(WorldUnitId unitId, string id, WorldPlayerId ownerId, WorldPosition position, UnitDefinition unit, int hitpoints)
    {
        return TestHelpers.SimpleStack(
            new WorldStackId(id),
            ownerId,
            position,
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(unitId, unit) with { Hitpoints = hitpoints }, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 0);
    }

    [Test]
    public void AttackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition attackerDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50),
                movementPoints: 6, leadership: 6) with
            {
                PurchaseCost = new WorldResourceSet(100, 0, 0, 0, 0, 0)
            };

        UnitDefinition defenderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5),
                movementPoints: 6, leadership: 6) with
            {
                PurchaseCost = new WorldResourceSet(50, 0, 0, 0, 0, 0)
            };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(1, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("attacker"), attackerDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(2, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defenderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        IReadOnlyList<TestWorldAiRunner.Trace> report = runner.PlayTurn();

        Assert.That(TestWorldAiTraceFormatter.Format(report), Is.EqualTo([
            "[P2 910] Move Stack1 -> (2, 0) (Attacker wins)"
        ]));
        Assert.That(session.World.State, Is.EqualTo(new WorldState(
            Map: map,
            TurnInfo: new WorldTurnInfo(1, player2.Id),
            Difficulty: WorldDifficulty.Average,
            Players: ImmutableSortedDictionary.CreateRange([
                new KeyValuePair<WorldPlayerId, WorldPlayer>(player1.Id, player1),
                new KeyValuePair<WorldPlayerId, WorldPlayer>(player2.Id, player2 with { Resources = new WorldResourceSet(100, 0, 0, 0, 25, 0) })
            ]),
            Relations: ImmutableSortedDictionary<WorldRelationId, WorldRelation>.Empty,
            Stacks: ImmutableSortedDictionary.CreateRange([
                new KeyValuePair<WorldStackId, WorldStack>(stack1.Id, stack1 with
                {
                    Direction = WorldStackDirection.SouthEast,
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("attacker"), attackerDefinition) with { Experience = 1 }, GridPosition.FrontBottom, false)
                        .Build(),
                    MovementPoints = 3,
                    BattlesWon = 1
                })
            ]),
            Fortifications: ImmutableSortedDictionary<WorldFortificationId, WorldFortification>.Empty,
            ResourceNodes: ImmutableSortedDictionary<WorldResourceNodeId, WorldResourceNode>.Empty,
            Rods: ImmutableSortedDictionary<WorldRodId, WorldRod>.Empty,
            Bags: ImmutableSortedDictionary<WorldBagId, WorldBag>.Empty,
            Tombs: ImmutableSortedDictionary<WorldTombId, WorldTomb>.Empty.Add(
                new WorldTombId("Tomb0001"),
                new WorldTomb(
                    new WorldTombId("Tomb0001"),
                    new WorldPosition(2, 0),
                    player2.Id, defenderDefinition.Id, LeaderName: null, player1.Id, attackerDefinition.Id, DestroyerLeaderName: null, Turn: 1)),
            Landmarks: ImmutableSortedDictionary<WorldLandmarkId, WorldLandmark>.Empty,
            Mountains: ImmutableSortedDictionary<WorldMountainId, WorldMountain>.Empty,
            Sites: ImmutableSortedDictionary<WorldSiteId, WorldSite>.Empty,
            Subraces: ImmutableSortedDictionary<WorldSubraceId, WorldSubrace>.Empty,
            ScenarioState: WorldScenarioState.Empty with { DestroyedStacks = [new ScenarioDestroyedStack(stack2.Id, stack1.OwnerId, SourceTemplateId: null)] },
            AiMemories: ImmutableSortedDictionary<WorldPlayerId, WorldAiMemory>.Empty)));
        Assert.That(session.Battle, Is.Null);
    }

    [Test]
    public void AttackRuinTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        UnitDefinition attackerDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50),
            movementPoints: 6, leadership: 6);
        UnitDefinition guardDefinition = TestHelpers.SimpleUnitDefinition("guard", maxHitpoints: 10, initiative: 40, damage: 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(2, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("attacker"), attackerDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);

        WorldRuin ruin = new(
            Id: new WorldFortificationId("Ruin1"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(2, 2), FortificationCalculator.RuinFootprint),
            Variant: new WorldObjectVariant(0),
            Garrison: new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("guard"), guardDefinition), GridPosition.FrontBottom, false)
                .Build(),
            Resources: new WorldResourceSet(200, 0, 0, 0, 0, 0),
            Item: null,
            LooterId: null,
            AiPriority: 3
        );

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(attackerDefinition)
            .WithDefinition(guardDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithRuin(ruin)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        IReadOnlyList<TestWorldAiRunner.Trace> report = runner.PlayTurn();

        Assert.That(TestWorldAiTraceFormatter.Format(report), Is.EqualTo([
            "[P2 910] Move Stack1 -> (2, 2) (Attacker wins)"
        ]));
        Assert.That(session.World.State.Fortifications[new WorldFortificationId("Ruin1")], Is.EqualTo(ruin with
        {
            Garrison = UnitGrid<WorldUnit>.Empty,
            LooterId = player.Id
        }));
        Assert.That(session.Battle, Is.Null);
    }

    [Test]
    public void AttackUnwinnableTargetTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        UnitDefinition weakDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 2, leadership: 6);

        UnitDefinition strongDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("strong", maxHitpoints: 100, initiative: 50, damage: 50),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("weak"), weakDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 2);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(1, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("strong"), strongDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 0);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(weakDefinition)
            .WithDefinition(strongDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        IReadOnlyList<TestWorldAiRunner.Trace> report = runner.PlayTurn();

        Assert.That(TestWorldAiTraceFormatter.Format(report), Is.Empty);
        Assert.That(session.World.State.Stacks[stack1.Id], Is.EqualTo(stack1));
        Assert.That(session.World.State.Stacks.Keys, Is.EqualTo([stack1.Id, stack2.Id]));
        Assert.That(session.Battle, Is.Null);
    }

    [Test]
    public void CostlyVictoryTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 2);
        UnitDefinition enemy = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 90, initiative: 60, damage: 45),
                movementPoints: 6, leadership: 6) with
            {
                ExperienceKilled = 0
            };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player1.Id, new WorldPosition(0, 0), main, 100, 2, WorldStackOrder.Normal.Instance);
        WorldStack enemyStack = EnemyStack(new WorldUnitId("enemyUnit"), "Enemy", player2.Id, new WorldPosition(1, 0), enemy, 90);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDifficulty(WorldDifficulty.Hard)
            .WithDefinition(main)
            .WithDefinition(enemy)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(mainStack)
            .WithStack(enemyStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P2 7] Move Main -> (1, 0) (Attacker wins)"]));
        Assert.That(session.World.State.Stacks.Keys, Is.EqualTo([mainStack.Id]));
        Assert.That(session.Battle, Is.Null);
    }

    [Test]
    public void MoveTowardEnemyTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        UnitDefinition weakDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 10, initiative: 40, damage: 5),
                movementPoints: 4, leadership: 6) with
            {
                PurchaseCost = new WorldResourceSet(50, 0, 0, 0, 0, 0)
            };

        UnitDefinition enemyDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 5, initiative: 1, damage: 1),
                movementPoints: 4, leadership: 6) with
            {
                PurchaseCost = new WorldResourceSet(500, 0, 0, 0, 0, 0)
            };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("weak"), weakDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 4);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(4, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemyDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 4);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(weakDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        IReadOnlyList<TestWorldAiRunner.Trace> report = runner.PlayTurn();

        Assert.That(TestWorldAiTraceFormatter.Format(report), Is.EqualTo([
            "[P1 1000] Move Stack1 -> (2, 0)"
        ]));
        Assert.That(session.World.State, Is.EqualTo(new WorldState(
            Map: map,
            TurnInfo: new WorldTurnInfo(1, player2.Id),
            Difficulty: WorldDifficulty.Average,
            Players: ImmutableSortedDictionary.CreateRange([
                new KeyValuePair<WorldPlayerId, WorldPlayer>(player1.Id, player1),
                new KeyValuePair<WorldPlayerId, WorldPlayer>(player2.Id, player2 with { Resources = new WorldResourceSet(100, 0, 0, 0, 25, 0) })
            ]),
            Relations: ImmutableSortedDictionary<WorldRelationId, WorldRelation>.Empty,
            Stacks: ImmutableSortedDictionary.CreateRange([
                new KeyValuePair<WorldStackId, WorldStack>(stack1.Id, stack1 with
                {
                    Position = new WorldPosition(2, 0),
                    Direction = WorldStackDirection.SouthEast,
                    MovementPoints = 0
                }),
                new KeyValuePair<WorldStackId, WorldStack>(stack2.Id, stack2)
            ]),
            Fortifications: ImmutableSortedDictionary<WorldFortificationId, WorldFortification>.Empty,
            ResourceNodes: ImmutableSortedDictionary<WorldResourceNodeId, WorldResourceNode>.Empty,
            Rods: ImmutableSortedDictionary<WorldRodId, WorldRod>.Empty,
            Bags: ImmutableSortedDictionary<WorldBagId, WorldBag>.Empty,
            Tombs: ImmutableSortedDictionary<WorldTombId, WorldTomb>.Empty,
            Landmarks: ImmutableSortedDictionary<WorldLandmarkId, WorldLandmark>.Empty,
            Mountains: ImmutableSortedDictionary<WorldMountainId, WorldMountain>.Empty,
            Sites: ImmutableSortedDictionary<WorldSiteId, WorldSite>.Empty,
            Subraces: ImmutableSortedDictionary<WorldSubraceId, WorldSubrace>.Empty,
            ScenarioState: WorldScenarioState.Empty,
            AiMemories: ImmutableSortedDictionary<WorldPlayerId, WorldAiMemory>.Empty)));
    }

    [Test]
    public void ExploreToEnemyTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 1);

        UnitDefinition scoutDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("scout", maxHitpoints: 100, initiative: 50, damage: 50),
                movementPoints: 4, leadership: 6) with
            {
                VisionRadius = 3
            };

        UnitDefinition enemyDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar([
                ".???????"
            ])
        };
        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Neutral, map);

        WorldStack scout = TestHelpers.SimpleStack(
            new WorldStackId("Scout"),
            player.Id,
            new WorldPosition(0, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("scout"), scoutDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 4);

        WorldStack enemy = TestHelpers.SimpleStack(
                new WorldStackId("Enemy"),
                neutral.Id,
                new WorldPosition(7, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemyDefinition), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 0) with
            {
                Order = WorldStackOrder.Stand.Instance
            };

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(scoutDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithStack(scout)
            .WithStack(enemy)
            .BuildSession();

        _ = session.World.DispatchCommand(new WorldStartGameCommand());

        TestWorldAiRunner runner = new(session, engine);

        IReadOnlyList<TestWorldAiRunner.Trace> report1 = runner.PlayTurn();
        Assert.That(TestWorldAiTraceFormatter.Format(report1), Is.EqualTo([
            "[P1 1000] Move Scout -> (2, 0)"
        ]));

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.Empty);

        IReadOnlyList<TestWorldAiRunner.Trace> report2 = runner.PlayTurn();
        Assert.That(TestWorldAiTraceFormatter.Format(report2), Is.EqualTo([
            "[P1 1000] Move Scout -> (4, 0)"
        ]));

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.Empty);

        IReadOnlyList<TestWorldAiRunner.Trace> report3 = runner.PlayTurn();
        Assert.That(TestWorldAiTraceFormatter.Format(report3), Is.EqualTo([
            "[P2 910] Move Scout -> (7, 0) (Attacker wins)"
        ]));

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.Empty);

        IReadOnlyList<TestWorldAiRunner.Trace> report4 = runner.PlayTurn();
        Assert.That(TestWorldAiTraceFormatter.Format(report4), Is.Empty);

        Assert.That(session.World.State, Is.EqualTo(new WorldState(
            Map: map,
            TurnInfo: new WorldTurnInfo(4, neutral.Id),
            Difficulty: WorldDifficulty.Average,
            Players: ImmutableSortedDictionary.CreateRange([
                new KeyValuePair<WorldPlayerId, WorldPlayer>(player.Id, player with
                {
                    Resources = new WorldResourceSet(400, 100, 0, 0, 0, 0),
                    FogOfWar = TestHelpers.BuildFogOfWar(["........"])
                }),
                new KeyValuePair<WorldPlayerId, WorldPlayer>(neutral.Id, neutral)
            ]),
            Relations: ImmutableSortedDictionary<WorldRelationId, WorldRelation>.Empty,
            Stacks: ImmutableSortedDictionary.CreateRange([
                new KeyValuePair<WorldStackId, WorldStack>(scout.Id, scout with
                {
                    Position = new WorldPosition(6, 0),
                    Direction = WorldStackDirection.SouthEast,
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("scout"), scoutDefinition) with { Experience = 1 }, GridPosition.FrontBottom, false)
                        .Build(),
                    MovementPoints = 4,
                    BattlesWon = 1
                })
            ]),
            Fortifications: ImmutableSortedDictionary<WorldFortificationId, WorldFortification>.Empty,
            ResourceNodes: ImmutableSortedDictionary<WorldResourceNodeId, WorldResourceNode>.Empty,
            Rods: ImmutableSortedDictionary<WorldRodId, WorldRod>.Empty,
            Bags: ImmutableSortedDictionary<WorldBagId, WorldBag>.Empty,
            Tombs: ImmutableSortedDictionary<WorldTombId, WorldTomb>.Empty.Add(
                new WorldTombId("Tomb0001"),
                new WorldTomb(
                    new WorldTombId("Tomb0001"),
                    new WorldPosition(7, 0),
                    neutral.Id, enemyDefinition.Id, LeaderName: null, player.Id, scoutDefinition.Id, DestroyerLeaderName: null, Turn: 3)),
            Landmarks: ImmutableSortedDictionary<WorldLandmarkId, WorldLandmark>.Empty,
            Mountains: ImmutableSortedDictionary<WorldMountainId, WorldMountain>.Empty,
            Sites: ImmutableSortedDictionary<WorldSiteId, WorldSite>.Empty,
            Subraces: ImmutableSortedDictionary<WorldSubraceId, WorldSubrace>.Empty,
            ScenarioState: WorldScenarioState.Empty with { DestroyedStacks = [new ScenarioDestroyedStack(enemy.Id, player.Id, SourceTemplateId: null)] },
            AiMemories: ImmutableSortedDictionary<WorldPlayerId, WorldAiMemory>.Empty)));
        Assert.That(session.Battle, Is.Null);
    }

    [Test]
    public void ScoutStacksTest()
    {
        WorldMap map = TestHelpers.SimpleMap(4, 4);

        UnitDefinition scoutDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("scout", maxHitpoints: 10, initiative: 50, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar([
                "....",
                "....",
                "....",
                "????"
            ])
        };

        WorldStack scout1 = TestHelpers.SimpleStack(
            new WorldStackId("Scout1"),
            player.Id,
            new WorldPosition(1, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("scout"), scoutDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 2);

        WorldStack scout2 = TestHelpers.SimpleStack(
            new WorldStackId("Scout2"),
            player.Id,
            new WorldPosition(2, 1),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("scout2"), scoutDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 2);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(scoutDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(scout1)
            .WithStack(scout2)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        IReadOnlyList<TestWorldAiRunner.Trace> report = runner.PlayTurn();
        Assert.That(TestWorldAiTraceFormatter.Format(report), Is.EqualTo([
            "[P1 1000] Move Scout1 -> (1, 1)",
            "[P1 1000] Move Scout2 -> (1, 2)"
        ]));
    }

    [Test]
    public void NewStackTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^..."
        ]);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 50),
                movementPoints: 2, leadership: 1) with
            {
                Race = WorldRace.Undead,
                PurchaseCost = new WorldResourceSet(100, 0, 0, 0, 0, 0)
            };

        UnitDefinition enemyDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            player1.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldRaceDefinition race = new(
            WorldRace.Undead,
            new GlobalTextId("Undead"),
            WorldResourceSet.Empty,
            WorldMapTerrain.Undead,
            true,
            35,
            [],
            [leaderDefinition.Id],
            [],
            ["Nigosh"],
            ["Orissia"]);

        WorldRaceDefinition enemyRace = new(
            WorldRace.Heretic,
            new GlobalTextId("Heretic"),
            WorldResourceSet.Empty,
            WorldMapTerrain.Heretic,
            true,
            35,
            [],
            [],
            [],
            [],
            []);

        WorldStack enemy = TestHelpers.SimpleStack(
            new WorldStackId("Enemy"),
            player2.Id,
            new WorldPosition(5, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("enemy"), enemyDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 2);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(enemyDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithCapital(capital)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithRace(race)
            .WithRace(enemyRace)
            .WithStack(enemy)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        IReadOnlyList<TestWorldAiRunner.Trace> report = runner.PlayTurn();
        Assert.That(TestWorldAiTraceFormatter.Format(report), Is.EqualTo([
            "[P2 0] Leader leader -> capital",
            "[P3 1000] AutoFit Stack0001",
            "[P2 910] Move Stack0001 -> (5, 4) (Attacker wins)"
        ]));
        Assert.That(session.World.State.Stacks.Keys, Is.EqualTo([new WorldStackId("Stack0001")]));
        Assert.That(session.Battle, Is.Null);
    }

    [Test]
    public void NewRodStackTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^^^",
            "^^^^^^^^",
            "^^^^^^^^",
            "^^^^^^^^",
            "^^^^...."
        ]);

        UnitDefinition rodDefinition = TestHelpers.SimpleUnitDefinition("rod", maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Leader,
            Race = WorldRace.Undead,
            LeaderCategory = UnitLeaderCategory.Rod,
            Leadership = 6,
            MovementPoints = 6,
            PurchaseCost = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        UnitDefinition warDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("war", maxHitpoints: 100, initiative: 50, damage: 50),
                movementPoints: 6, leadership: 6) with
            {
                Race = WorldRace.Undead,
                PurchaseCost = new WorldResourceSet(100, 0, 0, 0, 0, 0)
            };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldRaceDefinition race = new(
            WorldRace.Undead,
            new GlobalTextId("Undead"),
            WorldResourceSet.Empty,
            WorldMapTerrain.Undead,
            true,
            35,
            [],
            [warDefinition.Id, rodDefinition.Id],
            [],
            ["Nigosh"],
            ["Orissia"]);

        WorldStack mainStack = TestHelpers.SimpleStack(
            new WorldStackId("Main"),
            player.Id,
            new WorldPosition(7, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("war"), warDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 0);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(rodDefinition)
            .WithDefinition(warDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithRace(race)
            .WithStack(mainStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        IReadOnlyList<TestWorldAiRunner.Trace> report = runner.PlayTurn();

        Assert.That(TestWorldAiTraceFormatter.Format(report), Is.EqualTo([
            "[P2 0] Leader rod -> capital",
            "[P3 1000] AutoFit Stack0001"
        ]));

        UnitGrid<WorldUnit> expectedUnits = new UnitGrid<WorldUnit>.Builder()
            .Place(TestHelpers.SimpleUnit(new WorldUnitId("Unit0001"), rodDefinition) with { Name = "Nigosh" }, GridPosition.BackCenter, false)
            .Build();

        Assert.That(session.World.State.Stacks[new WorldStackId("Stack0001")], Is.EqualTo(TestHelpers.SimpleStack(
            new WorldStackId("Stack0001"),
            player.Id,
            new WorldPosition(4, 4),
            expectedUnits,
            movementPoints: 6)));
        Assert.That(session.World.State.Players[player.Id].Resources, Is.EqualTo(WorldResourceSet.Empty));
    }

    [Test]
    public void LeaderPreferenceTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^..."
        ]);

        UnitDefinition mageDefinition = TestHelpers.SimpleUnitDefinition("mage", maxHitpoints: 100, initiative: 50, damage: 30) with
        {
            Category = UnitCategory.Leader,
            Race = WorldRace.Undead,
            LeaderCategory = UnitLeaderCategory.Mage,
            Leadership = 6,
            MovementPoints = 2,
            PurchaseCost = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        UnitDefinition fighterDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("fighter", maxHitpoints: 100, initiative: 50, damage: 50),
                movementPoints: 2, leadership: 6) with
            {
                Race = WorldRace.Undead,
                PurchaseCost = new WorldResourceSet(100, 0, 0, 0, 0, 0)
            };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldRaceDefinition race = new(
            WorldRace.Undead,
            new GlobalTextId("Undead"),
            WorldResourceSet.Empty,
            WorldMapTerrain.Undead,
            true,
            35,
            [],
            [mageDefinition.Id, fighterDefinition.Id],
            [],
            ["Nigosh"],
            ["Orissia"]);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(mageDefinition)
            .WithDefinition(fighterDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithRace(race)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        IReadOnlyList<TestWorldAiRunner.Trace> report = runner.PlayTurn();

        Assert.That(TestWorldAiTraceFormatter.Format(report), Is.EqualTo([
            "[P2 0] Leader fighter -> capital",
            "[P3 1000] AutoFit Stack0001"
        ]));
    }

    [Test]
    public void PurchaseUnitTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^..."
        ]);

        UnitDefinition rangedDefinition = TestHelpers.SimpleUnitDefinition("ranged", maxHitpoints: 30, initiative: 50, damage: 20);

        UnitDefinition meleeDefinition = TestHelpers.SimpleUnitDefinition("melee", maxHitpoints: 60, initiative: 40, damage: 15) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("melee"), initiative: 40, damage: 15, accuracy: 100, reach: UnitAttackReach.Adjacent)
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldRaceDefinition race = new(
            WorldRace.Undead,
            new GlobalTextId("Undead"),
            WorldResourceSet.Empty,
            WorldMapTerrain.Undead,
            true,
            35,
            [],
            [],
            [rangedDefinition.Id, meleeDefinition.Id],
            [],
            []);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(rangedDefinition)
            .WithDefinition(meleeDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithRace(race)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        IReadOnlyList<TestWorldAiRunner.Trace> report = runner.PlayTurn();

        Assert.That(TestWorldAiTraceFormatter.Format(report), Is.EqualTo([
            "[P0 750] Purchase melee -> capital (0, 0)"
        ]));
    }

    [Test]
    public void PurchaseUnownedUnitTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^..."
        ]);

        UnitDefinition rangedDefinition = TestHelpers.SimpleUnitDefinition("ranged", maxHitpoints: 30, initiative: 50, damage: 20);

        UnitDefinition meleeDefinition = TestHelpers.SimpleUnitDefinition("melee", maxHitpoints: 60, initiative: 40, damage: 15) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("melee"), initiative: 40, damage: 15, accuracy: 100, reach: UnitAttackReach.Adjacent)
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("melee"), meleeDefinition), GridPosition.FrontBottom, false)
                .Build());

        WorldRaceDefinition race = new(
            WorldRace.Undead,
            new GlobalTextId("Undead"),
            WorldResourceSet.Empty,
            WorldMapTerrain.Undead,
            true,
            35,
            [],
            [],
            [rangedDefinition.Id, meleeDefinition.Id],
            [],
            []);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(rangedDefinition)
            .WithDefinition(meleeDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithRace(race)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        IReadOnlyList<TestWorldAiRunner.Trace> report = runner.PlayTurn();

        Assert.That(TestWorldAiTraceFormatter.Format(report), Is.EqualTo([
            "[P0 600] Purchase ranged -> capital (0, 1)"
        ]));
    }

    [Test]
    public void PurchaseUnownedByStackTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^..."
        ]);

        UnitDefinition fighterDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("fighter", maxHitpoints: 100, initiative: 50, damage: 50),
            movementPoints: 2, leadership: 6);

        UnitDefinition rangedDefinition = TestHelpers.SimpleUnitDefinition("ranged", maxHitpoints: 30, initiative: 50, damage: 20);

        UnitDefinition meleeDefinition = TestHelpers.SimpleUnitDefinition("melee", maxHitpoints: 60, initiative: 40, damage: 15) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("melee"), initiative: 40, damage: 15, accuracy: 100, reach: UnitAttackReach.Adjacent)
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack"),
                player.Id,
                capital.Entrance,
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("fighter"), fighterDefinition), GridPosition.FrontBottom, false)
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("melee"), meleeDefinition), GridPosition.FrontCenter, false)
                    .Build(),
                movementPoints: 0) with
            {
                AiIgnore = true
            };

        WorldRaceDefinition race = new(
            WorldRace.Undead,
            new GlobalTextId("Undead"),
            WorldResourceSet.Empty,
            WorldMapTerrain.Undead,
            true,
            35,
            [],
            [],
            [rangedDefinition.Id, meleeDefinition.Id],
            [],
            []);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(fighterDefinition)
            .WithDefinition(rangedDefinition)
            .WithDefinition(meleeDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithRace(race)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        IReadOnlyList<TestWorldAiRunner.Trace> report = runner.PlayTurn();

        Assert.That(TestWorldAiTraceFormatter.Format(report), Is.EqualTo([
            "[P0 600] Purchase ranged -> capital (0, 0)"
        ]));
    }

    [Test]
    public void PurchaseLargeUnitTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^..."
        ]);

        UnitDefinition guardDefinition = TestHelpers.SimpleUnitDefinition("guard", maxHitpoints: 10, initiative: 40, damage: 5);

        UnitDefinition largeDefinition = TestHelpers.SimpleUnitDefinition("large", maxHitpoints: 200, initiative: 30, damage: 40) with
        {
            IsLarge = true
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("guard"), guardDefinition), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("guard2"), guardDefinition), GridPosition.FrontCenter, false)
                .Build());

        WorldRaceDefinition race = new(
            WorldRace.Undead,
            new GlobalTextId("Undead"),
            WorldResourceSet.Empty,
            WorldMapTerrain.Undead,
            true,
            35,
            [],
            [],
            [largeDefinition.Id],
            [],
            []);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(guardDefinition)
            .WithDefinition(largeDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithRace(race)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        IReadOnlyList<TestWorldAiRunner.Trace> report = runner.PlayTurn();

        Assert.That(TestWorldAiTraceFormatter.Format(report), Is.EqualTo([
            "[P0 600] Purchase large -> capital (0, 2)"
        ]));
    }

    [Test]
    public void IgnoredStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);
        UnitDefinition attackerDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 100),
            movementPoints: 6, leadership: 6);
        UnitDefinition defenderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer enemyPlayer = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldStack ignoredStack = TestHelpers.SimpleStack(
                new WorldStackId("Ignored"),
                player.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("attacker"), attackerDefinition), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 6) with
            {
                AiIgnore = true
            };
        WorldStack enemyStack = TestHelpers.SimpleStack(
            new WorldStackId("Enemy"),
            enemyPlayer.Id,
            new WorldPosition(1, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defenderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 0);
        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemyPlayer.LordId, enemyPlayer.Race, LordCategory.Warrior))
            .WithPlayer(enemyPlayer)
            .WithStack(ignoredStack)
            .WithStack(enemyStack)
            .BuildSession();
        TestWorldAiRunner runner = new(session, engine);

        IReadOnlyList<TestWorldAiRunner.Trace> report = runner.PlayTurn();

        Assert.That(TestWorldAiTraceFormatter.Format(report), Is.Empty);
        Assert.That(session.Battle, Is.Null);
    }

    [Test]
    public void InvisibleEnemyTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition attackerDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50),
                movementPoints: 6, leadership: 6) with
            {
                PurchaseCost = new WorldResourceSet(100, 0, 0, 0, 0, 0)
            };

        UnitDefinition defenderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5),
                movementPoints: 6, leadership: 6) with
            {
                PurchaseCost = new WorldResourceSet(50, 0, 0, 0, 0, 0)
            };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(1, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("attacker"), attackerDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);

        WorldStack stack2 = TestHelpers.SimpleStack(
                new WorldStackId("Stack2"),
                player2.Id,
                new WorldPosition(2, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defenderDefinition), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 6) with
            {
                Concealed = true
            };

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(stack1)
            .WithStack(stack2)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.Empty);
        Assert.That(session.World.State.Stacks[stack2.Id], Is.EqualTo(stack2));
        Assert.That(session.Battle, Is.Null);
    }

    [TestCase(WorldDifficulty.Easy, "[P2 910] Move Main -> (3, 0) (Attacker wins)")]
    [TestCase(WorldDifficulty.Average, "[P2 910] Move Main -> (3, 0) (Attacker wins)")]
    [TestCase(WorldDifficulty.Hard, "[P2 450] Move Main -> (3, 0) (Attacker wins)")]
    [TestCase(WorldDifficulty.VeryHard, "[P2 450] Move Main -> (3, 0) (Attacker wins)")]
    public void AttackWinnableTargetTest(WorldDifficulty difficulty, string expectedTrace)
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 6);
        UnitDefinition weak = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 10, initiative: 40, damage: 5),
                movementPoints: 6, leadership: 6) with
            {
                ExperienceKilled = 50
            };
        UnitDefinition strong = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("strong", maxHitpoints: 500, initiative: 60, damage: 100),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player1.Id, new WorldPosition(2, 0), main, 100, 6, WorldStackOrder.Normal.Instance);
        WorldStack strongStack = EnemyStack(new WorldUnitId("strongUnit"), "Strong", player2.Id, new WorldPosition(1, 0), strong, 500);
        WorldStack weakStack = EnemyStack(new WorldUnitId("weakUnit"), "Weak", player2.Id, new WorldPosition(3, 0), weak, 10);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDifficulty(difficulty)
            .WithDefinition(main)
            .WithDefinition(weak)
            .WithDefinition(strong)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(mainStack)
            .WithStack(strongStack)
            .WithStack(weakStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([expectedTrace]));
    }

    [Test]
    public void ApproachEnemyTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 4);
        UnitDefinition weak = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 10, initiative: 40, damage: 5),
                movementPoints: 6, leadership: 6) with
            {
                ExperienceKilled = 50
            };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player1.Id, new WorldPosition(0, 0), main, 100, 4, WorldStackOrder.Normal.Instance);
        WorldStack weakStack = EnemyStack(new WorldUnitId("weakUnit"), "Weak", player2.Id, new WorldPosition(4, 0), weak, 10);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(weak)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(mainStack)
            .WithStack(weakStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P1 1000] Move Main -> (2, 0)"]));
    }

    [Test]
    public void OutmatchedNearestTest()
    {
        WorldMap map = TestHelpers.SimpleMap(9, 1);

        UnitDefinition main = Leader("main", damage: 5, movementPoints: 6);
        UnitDefinition strong = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("strong", maxHitpoints: 500, initiative: 60, damage: 100),
            movementPoints: 6, leadership: 6);
        UnitDefinition weak = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 10, initiative: 10, damage: 1),
            movementPoints: 6, leadership: 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Neutral, map);

        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player.Id, new WorldPosition(4, 0), main, 100, 6, WorldStackOrder.Roam.Instance);
        WorldStack nearStack = EnemyStack(new WorldUnitId("nearUnit"), "Near", neutral.Id, new WorldPosition(7, 0), strong, 500);
        WorldStack farStack = EnemyStack(new WorldUnitId("farUnit"), "Far", neutral.Id, new WorldPosition(0, 0), weak, 10);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(strong)
            .WithDefinition(weak)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithStack(mainStack)
            .WithStack(nearStack)
            .WithStack(farStack)
            .BuildSession();

        _ = session.World.DispatchCommand(new WorldStartGameCommand());

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P2 620] Move Main -> (0, 0) (Attacker wins)"]));
        Assert.That(session.World.State.Stacks[mainStack.Id], Is.EqualTo(mainStack with
        {
            Position = new WorldPosition(1, 0),
            Direction = WorldStackDirection.NorthWest,
            Units = new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("mainLeader"), main) with { Hitpoints = 98, Experience = 1 }, GridPosition.FrontBottom, false)
                .Build(),
            MovementPoints = 0,
            BattlesWon = 1
        }));
    }

    [Test]
    public void BlockedApproachTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 3);

        UnitDefinition main = Leader("main", damage: 5, movementPoints: 6);
        UnitDefinition strong = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("strong", maxHitpoints: 500, initiative: 60, damage: 100),
            movementPoints: 6, leadership: 6);
        UnitDefinition weak = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 10, initiative: 10, damage: 1),
            movementPoints: 6, leadership: 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Neutral, map);

        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player.Id, new WorldPosition(5, 1), main, 100, 6, WorldStackOrder.Normal.Instance);
        WorldStack guardStack = EnemyStack(new WorldUnitId("guardUnit"), "Guard", neutral.Id, new WorldPosition(7, 1), strong, 500);
        WorldStack nearStack = EnemyStack(new WorldUnitId("nearUnit"), "Near", neutral.Id, new WorldPosition(9, 1), weak, 10);
        WorldStack farStack = EnemyStack(new WorldUnitId("farUnit"), "Far", neutral.Id, new WorldPosition(0, 1), weak, 10);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(strong)
            .WithDefinition(weak)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithStack(mainStack)
            .WithStack(guardStack)
            .WithStack(nearStack)
            .WithStack(farStack)
            .BuildSession();

        _ = session.World.DispatchCommand(new WorldStartGameCommand());

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P1 1000] Move Main -> (2, 1)"]));
        Assert.That(session.World.State.Stacks[mainStack.Id], Is.EqualTo(mainStack with
        {
            Position = new WorldPosition(2, 1),
            Direction = WorldStackDirection.NorthWest,
            MovementPoints = 0
        }));
    }

    [Test]
    public void OutmatchedTargetsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(9, 1);

        UnitDefinition main = Leader("main", damage: 5, movementPoints: 6);
        UnitDefinition strong = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("strong", maxHitpoints: 500, initiative: 60, damage: 100),
            movementPoints: 6, leadership: 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Neutral, map);

        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player.Id, new WorldPosition(4, 0), main, 100, 6, WorldStackOrder.Normal.Instance);
        WorldStack nearStack = EnemyStack(new WorldUnitId("nearUnit"), "Near", neutral.Id, new WorldPosition(7, 0), strong, 500) with { Order = WorldStackOrder.Stand.Instance };
        WorldStack farStack = EnemyStack(new WorldUnitId("farUnit"), "Far", neutral.Id, new WorldPosition(0, 0), strong, 500) with { Order = WorldStackOrder.Stand.Instance };

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(strong)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithStack(mainStack)
            .WithStack(nearStack)
            .WithStack(farStack)
            .BuildSession();

        _ = session.World.DispatchCommand(new WorldStartGameCommand());

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.Empty);
        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.Empty);
        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.Empty);
        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.Empty);
        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.Empty);
        Assert.That(session.World.State.Stacks[mainStack.Id], Is.EqualTo(mainStack));
    }

    [Test]
    public void TargetWithinPowerMarginTest()
    {
        WorldMap map = TestHelpers.SimpleMap(9, 1);

        UnitDefinition main = Leader("main", damage: 5, movementPoints: 6);
        UnitDefinition strong = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("strong", maxHitpoints: 60, initiative: 60, damage: 10),
            movementPoints: 6, leadership: 6);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Neutral, map);

        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player.Id, new WorldPosition(4, 0), main, 100, 6, WorldStackOrder.Normal.Instance);
        WorldStack enemyStack = EnemyStack(new WorldUnitId("enemyUnit"), "Enemy", neutral.Id, new WorldPosition(7, 0), strong, 60) with { Order = WorldStackOrder.Stand.Instance };

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDifficulty(WorldDifficulty.Hard)
            .WithDefinition(main)
            .WithDefinition(strong)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithStack(mainStack)
            .WithStack(enemyStack)
            .BuildSession();

        _ = session.World.DispatchCommand(new WorldStartGameCommand());

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P1 333] Move Main -> (5, 0)"]));
        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.Empty);
        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.Empty);
        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.Empty);
        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.Empty);
        Assert.That(session.World.State.Stacks[mainStack.Id], Is.EqualTo(mainStack with
        {
            Position = new WorldPosition(5, 0),
            Direction = WorldStackDirection.SouthEast
        }));
    }

    [Test]
    public void HoldNearOwnCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 5);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 6);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Undead, 1), WorldResourceSet.Empty);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = WorldResourceSet.Empty
        };

        WorldCity nearCity = TestHelpers.SimpleCity(
            new WorldFortificationId("near"),
            cityDefinition,
            player.Id,
            new WorldPosition(4, 3),
            UnitGrid<WorldUnit>.Empty);

        WorldCity farCity = TestHelpers.SimpleCity(
            new WorldFortificationId("far"),
            cityDefinition,
            player.Id,
            new WorldPosition(9, 3),
            UnitGrid<WorldUnit>.Empty);

        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player.Id, nearCity.Entrance, main, 30, 6, WorldStackOrder.Normal.Instance);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(nearCity)
            .WithCity(farCity)
            .WithStack(mainStack)
            .WithAiMemory(player.Id, WorldAiMemory.Empty.WithStackMemory(
                mainStack.Id,
                new WorldAiStackMemory(WorldStackOrder.Heal.Instance, null)))
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P3 1000] AutoFit Main"]));
        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.Empty);
        Assert.That(session.World.State.Stacks[mainStack.Id], Is.EqualTo(mainStack with
        {
            Units = new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("mainLeader"), main) with { Hitpoints = 90 }, GridPosition.BackCenter, false)
                .Build()
        }));
    }

    [Test]
    public void HealedStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 6);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player.Id, new WorldPosition(0, 0), main, 100, 6, WorldStackOrder.Normal.Instance);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(mainStack)
            .WithAiMemory(player.Id, WorldAiMemory.Empty.WithStackMemory(
                mainStack.Id,
                new WorldAiStackMemory(WorldStackOrder.Heal.Instance, null)))
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.Empty);
        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.Empty);
    }

    [Test]
    public void TwoTargetsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 6);
        UnitDefinition harmless = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("harmless", maxHitpoints: 10, initiative: 40, damage: 5),
                movementPoints: 6, leadership: 6) with
            {
                ExperienceKilled = 20
            };
        UnitDefinition brawler = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("brawler", maxHitpoints: 60, initiative: 60, damage: 15),
                movementPoints: 6, leadership: 6) with
            {
                ExperienceKilled = 20
            };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player1.Id, new WorldPosition(1, 0), main, 100, 6, WorldStackOrder.Normal.Instance);
        WorldStack brawlerStack = EnemyStack(new WorldUnitId("brawlerUnit"), "Brawler", player2.Id, new WorldPosition(0, 0), brawler, 60);
        WorldStack harmlessStack = EnemyStack(new WorldUnitId("harmlessUnit"), "Harmless", player2.Id, new WorldPosition(2, 0), harmless, 10);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(harmless)
            .WithDefinition(brawler)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(mainStack)
            .WithStack(brawlerStack)
            .WithStack(harmlessStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P2 910] Move Main -> (2, 0) (Attacker wins)", "[P2 702] Move Main -> (0, 0) (Attacker wins)"]));
    }

    [Test]
    public void AttackTargetMemoryTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 6);
        UnitDefinition harmless = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("harmless", maxHitpoints: 10, initiative: 40, damage: 5),
                movementPoints: 6, leadership: 6) with
            {
                ExperienceKilled = 20
            };
        UnitDefinition brawler = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("brawler", maxHitpoints: 60, initiative: 60, damage: 40),
                movementPoints: 6, leadership: 6) with
            {
                ExperienceKilled = 20
            };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player1.Id, new WorldPosition(1, 0), main, 100, 6, WorldStackOrder.Roam.Instance);
        WorldStack brawlerStack = EnemyStack(new WorldUnitId("brawlerUnit"), "Brawler", player2.Id, new WorldPosition(0, 0), brawler, 60);
        WorldStack harmlessStack = EnemyStack(new WorldUnitId("harmlessUnit"), "Harmless", player2.Id, new WorldPosition(2, 0), harmless, 10);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(harmless)
            .WithDefinition(brawler)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(mainStack)
            .WithStack(brawlerStack)
            .WithStack(harmlessStack)
            .BuildSession();

        WorldAiController controller = new(engine);

        WorldAiActionPlan plan = controller.DecideAction(session.World.State, WorldAiMemory.Empty);

        Assert.That(plan, Is.EqualTo(new WorldAiActionPlan(
            new WorldAiActionScore(2, 852),
            new WorldAiMoveStackCommand(mainStack.Id, new WorldPosition(2, 0)),
            new WorldAiMemory(
                new WorldAiTurnMemory(1, player1.Id, 1, 0, 0, []),
                ImmutableSortedDictionary<WorldStackId, WorldAiStackMemory>.Empty.SetItem(
                    mainStack.Id,
                    new WorldAiStackMemory(
                        WorldStackOrder.Normal.Instance,
                        new WorldAiTarget.Stack(harmlessStack.Id)))))));
    }

    [Test]
    public void AttackRuinTargetMemoryTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        UnitDefinition attackerDefinition = Leader("attacker", damage: 50, movementPoints: 6);
        UnitDefinition guardDefinition = TestHelpers.SimpleUnitDefinition("guard", maxHitpoints: 10, initiative: 40, damage: 5);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = MyStack(new WorldUnitId("stack1Leader"), "Stack1", player.Id, new WorldPosition(2, 3), attackerDefinition, 100, 6, WorldStackOrder.Normal.Instance);

        WorldRuin ruin = new(
            Id: new WorldFortificationId("Ruin1"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(2, 2), FortificationCalculator.RuinFootprint),
            Variant: new WorldObjectVariant(0),
            Garrison: new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("guard"), guardDefinition), GridPosition.FrontBottom, false)
                .Build(),
            Resources: new WorldResourceSet(200, 0, 0, 0, 0, 0),
            Item: null,
            LooterId: null,
            AiPriority: 3
        );

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(attackerDefinition)
            .WithDefinition(guardDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithRuin(ruin)
            .BuildSession();

        WorldAiController controller = new(engine);

        WorldAiActionPlan plan = controller.DecideAction(session.World.State, WorldAiMemory.Empty);

        Assert.That(plan, Is.EqualTo(new WorldAiActionPlan(
            new WorldAiActionScore(2, 910),
            new WorldAiMoveStackCommand(stack.Id, new WorldPosition(2, 2)),
            new WorldAiMemory(
                new WorldAiTurnMemory(1, player.Id, 1, 0, 0, []),
                ImmutableSortedDictionary<WorldStackId, WorldAiStackMemory>.Empty.SetItem(
                    stack.Id,
                    new WorldAiStackMemory(
                        WorldStackOrder.Normal.Instance,
                        new WorldAiTarget.Fortification(ruin.Id)))))));
    }

    [Test]
    public void ExploreFogFrontierTest()
    {
        WorldMap map = TestHelpers.BuildMap(["...^....."]);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 6);
        UnitDefinition strong = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("strong", maxHitpoints: 500, initiative: 60, damage: 100),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar([".?......?"])
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player1.Id, new WorldPosition(4, 0), main, 100, 6, WorldStackOrder.Normal.Instance);
        WorldStack strongStack = EnemyStack(new WorldUnitId("strongUnit"), "Strong", player2.Id, new WorldPosition(0, 0), strong, 500);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(strong)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(mainStack)
            .WithStack(strongStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P1 1000] Move Main -> (7, 0)"]));
    }

    [Test]
    public void ExploreBehindRuinTest()
    {
        WorldMap map = TestHelpers.SimpleMap(9, 3);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar([
                "........?",
                "........?",
                "........?"
            ])
        };

        WorldRuin ruin = new(
            Id: new WorldFortificationId("Ruin1"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(2, 2), FortificationCalculator.RuinFootprint),
            Variant: new WorldObjectVariant(0),
            Garrison: UnitGrid<WorldUnit>.Empty,
            Resources: WorldResourceSet.Empty,
            Item: null,
            LooterId: player1.Id,
            AiPriority: 3
        );

        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player1.Id, new WorldPosition(4, 1), main, 100, 6, WorldStackOrder.Normal.Instance);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithRuin(ruin)
            .WithStack(mainStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P1 1000] Move Main -> (7, 1)"]));
    }

    [Test]
    public void RetreatTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^^^^^",
            "^^^^^^^^^^",
            "^^^^^^^^^^",
            "^^^^^^^^^^",
            "^^^^......"
        ]);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            player1.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player1.Id, new WorldPosition(9, 4), main, 30, 6, WorldStackOrder.Normal.Instance);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithCapital(capital)
            .WithStack(mainStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P1 1000] Move Main -> (6, 4)"]));
    }

    [Test]
    public void StandTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 6);
        UnitDefinition weak = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 10, initiative: 40, damage: 5),
                movementPoints: 6, leadership: 6) with
            {
                ExperienceKilled = 50
            };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player1.Id, new WorldPosition(2, 0), main, 100, 6, WorldStackOrder.Stand.Instance);
        WorldStack weakStack = EnemyStack(new WorldUnitId("weakUnit"), "Weak", player2.Id, new WorldPosition(3, 0), weak, 10);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(weak)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(mainStack)
            .WithStack(weakStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.Empty);
    }

    [Test]
    public void GuardTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 6);
        UnitDefinition weak = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 10, initiative: 40, damage: 5),
                movementPoints: 6, leadership: 6) with
            {
                ExperienceKilled = 50
            };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player1.Id, new WorldPosition(0, 0), main, 100, 6, WorldStackOrder.Guard.Instance);
        WorldStack weakStack = EnemyStack(new WorldUnitId("weakUnit"), "Weak", player2.Id, new WorldPosition(4, 0), weak, 10);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(weak)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(mainStack)
            .WithStack(weakStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.Empty);
    }

    [Test]
    public void GuardAttackAdjacentTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 6);
        UnitDefinition weak = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 10, initiative: 40, damage: 5),
                movementPoints: 6, leadership: 6) with
            {
                ExperienceKilled = 50
            };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player1.Id, new WorldPosition(2, 0), main, 100, 6, WorldStackOrder.Guard.Instance);
        WorldStack weakStack = EnemyStack(new WorldUnitId("weakUnit"), "Weak", player2.Id, new WorldPosition(3, 0), weak, 10);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(weak)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(mainStack)
            .WithStack(weakStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P2 910] Move Main -> (3, 0) (Attacker wins)"]));
    }

    [Test]
    public void GuardAttackWithinRangeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 6);
        UnitDefinition weak = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 10, initiative: 40, damage: 5),
                movementPoints: 6, leadership: 6) with
            {
                ExperienceKilled = 50
            };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player1.Id, new WorldPosition(2, 0), main, 100, 6, WorldStackOrder.Guard.Instance);
        WorldStack weakStack = EnemyStack(new WorldUnitId("weakUnit"), "Weak", player2.Id, new WorldPosition(4, 0), weak, 10);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(weak)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(mainStack)
            .WithStack(weakStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P2 910] Move Main -> (4, 0) (Attacker wins)"]));
    }

    [Test]
    public void GuardIgnoreStandingStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 6);
        UnitDefinition weak = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 10, initiative: 40, damage: 5),
                movementPoints: 6, leadership: 6) with
            {
                ExperienceKilled = 50
            };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player1.Id, new WorldPosition(2, 0), main, 100, 6, WorldStackOrder.Guard.Instance);
        WorldStack weakStack = EnemyStack(new WorldUnitId("weakUnit"), "Weak", player2.Id, new WorldPosition(3, 0), weak, 10) with
        {
            Order = WorldStackOrder.Stand.Instance
        };

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(weak)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(mainStack)
            .WithStack(weakStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.Empty);
    }

    [Test]
    public void GuardIgnoreCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 6);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Heretic, 1), WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player1.Id, new WorldPosition(2, 3), main, 100, 6, WorldStackOrder.Guard.Instance);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("City"),
            cityDefinition,
            player2.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(mainStack)
            .WithCity(city)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.Empty);
    }

    [Test]
    public void GuardAttackStrongerTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition weak = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);
        UnitDefinition strong = Leader("strong", damage: 50, movementPoints: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack guardStack = MyStack(new WorldUnitId("guardLeader"), "Guard", player1.Id, new WorldPosition(0, 0), weak, 10, 6, WorldStackOrder.Guard.Instance);
        WorldStack strongStack = EnemyStack(new WorldUnitId("strongUnit"), "Strong", player2.Id, new WorldPosition(1, 0), strong, 100);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDifficulty(WorldDifficulty.Hard)
            .WithDefinition(weak)
            .WithDefinition(strong)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(guardStack)
            .WithStack(strongStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P2 89] Move Guard -> (1, 0) (Defender wins)"]));
    }

    [Test]
    public void DefendStackAttackWithinRangeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 3);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 6);
        UnitDefinition ally = Leader("ally", damage: 50, movementPoints: 0);
        UnitDefinition weak = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 10, initiative: 40, damage: 5),
                movementPoints: 6, leadership: 6) with
            {
                ExperienceKilled = 50
            };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack allyStack = MyStack(new WorldUnitId("allyLeader"), "Ally", player1.Id, new WorldPosition(3, 1), ally, 100, 0, WorldStackOrder.Stand.Instance);
        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player1.Id, new WorldPosition(4, 1), main, 100, 6, new WorldStackOrder.DefendStack(allyStack.Id));
        WorldStack weakStack = EnemyStack(new WorldUnitId("weakUnit"), "Weak", player2.Id, new WorldPosition(5, 1), weak, 10);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(ally)
            .WithDefinition(weak)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(allyStack)
            .WithStack(mainStack)
            .WithStack(weakStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P2 910] Move Main -> (5, 1) (Attacker wins)"]));
    }

    [Test]
    public void SecureCityAttackBlockingStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 10);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 6);
        UnitDefinition weak = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 10, initiative: 40, damage: 5),
                movementPoints: 6, leadership: 6) with
            {
                ExperienceKilled = 50
            };

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1), WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("City"),
            cityDefinition,
            player1.Id,
            new WorldPosition(3, 9),
            UnitGrid<WorldUnit>.Empty);

        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player1.Id, new WorldPosition(7, 9), main, 100, 6, new WorldStackOrder.SecureCity(city.Id));
        WorldStack weakStack = EnemyStack(new WorldUnitId("weakUnit"), "Weak", player2.Id, new WorldPosition(5, 8), weak, 10);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(weak)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithCity(city)
            .WithStack(mainStack)
            .WithStack(weakStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 910] Move Main -> (3, 9) (Attacker wins)",
            "[P1 1000] Move Main -> (5, 8)"
        ]));
    }

    [Test]
    public void SecureCityIgnoreDistantStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 10);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 6);
        UnitDefinition weak = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 10, initiative: 40, damage: 5),
                movementPoints: 6, leadership: 6) with
            {
                ExperienceKilled = 50
            };

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1), WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("City"),
            cityDefinition,
            player1.Id,
            new WorldPosition(3, 9),
            UnitGrid<WorldUnit>.Empty);

        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player1.Id, new WorldPosition(5, 9), main, 100, 6, new WorldStackOrder.SecureCity(city.Id));
        WorldStack weakStack = EnemyStack(new WorldUnitId("weakUnit"), "Weak", player2.Id, new WorldPosition(8, 9), weak, 10);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(weak)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithCity(city)
            .WithStack(mainStack)
            .WithStack(weakStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P1 333] Move Main -> (3, 9)",
            "[P3 1000] AutoFit Main"
        ]));
    }

    [Test]
    public void MoveToLocationTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 4);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCanonicalStackOrder order = new WorldStackOrder.MoveToLocation(new ScenarioLocationId("loc"), new WorldPosition(4, 0), 0);
        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player1.Id, new WorldPosition(0, 0), main, 100, 4, order);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithStack(mainStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P1 1000] Move Main -> (2, 0)"]));
    }

    [Test]
    public void MoveToLocationHoldTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 4);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };

        WorldCanonicalStackOrder order = new WorldStackOrder.MoveToLocation(new ScenarioLocationId("loc"), new WorldPosition(2, 0), 1);
        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player1.Id, new WorldPosition(2, 0), main, 100, 4, order);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithStack(mainStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P0 1000] Order Main -> Stand"]));
    }

    [Test]
    public void MoveToBlockedLocationTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "...^"
        ]);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 4);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCanonicalStackOrder order = new WorldStackOrder.DefendLocation(new ScenarioLocationId("loc"), new WorldPosition(3, 0), 1);
        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player1.Id, new WorldPosition(0, 0), main, 100, 4, order);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithStack(mainStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P1 1000] Move Main -> (2, 0)"]));
    }

    [TestCase(WorldDifficulty.Easy, "[P2 240] Move Main -> (8, 0) (Defender wins)")]
    [TestCase(WorldDifficulty.Average, "[P2 240] Move Main -> (8, 0) (Defender wins)")]
    [TestCase(WorldDifficulty.Hard, "[P2 240] Move Main -> (8, 0) (Defender wins)")]
    [TestCase(WorldDifficulty.VeryHard, "[P2 240] Move Main -> (8, 0) (Defender wins)")]
    public void AttackStackOrderTest(WorldDifficulty difficulty, string expectedTrace)
    {
        WorldMap map = TestHelpers.SimpleMap(9, 1);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 6);
        UnitDefinition strong = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("strong", maxHitpoints: 500, initiative: 60, damage: 100),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack leftStack = EnemyStack(new WorldUnitId("leftUnit"), "Left", player2.Id, new WorldPosition(0, 0), strong, 500);
        WorldStack rightStack = EnemyStack(new WorldUnitId("rightUnit"), "Right", player2.Id, new WorldPosition(8, 0), strong, 500);

        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player1.Id, new WorldPosition(4, 0), main, 100, 6, new WorldStackOrder.AttackStack(rightStack.Id));

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDifficulty(difficulty)
            .WithDefinition(main)
            .WithDefinition(strong)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(mainStack)
            .WithStack(leftStack)
            .WithStack(rightStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([expectedTrace]));
    }

    [Test]
    public void AttackStackOrderUnwinnableTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 6);
        UnitDefinition strong = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("strong", maxHitpoints: 500, initiative: 60, damage: 100),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack strongStack = EnemyStack(new WorldUnitId("strongUnit"), "Strong", player2.Id, new WorldPosition(1, 0), strong, 500);
        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player1.Id, new WorldPosition(0, 0), main, 100, 6, new WorldStackOrder.AttackStack(strongStack.Id));

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(strong)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(mainStack)
            .WithStack(strongStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P2 240] Move Main -> (1, 0) (Defender wins)"]));
    }

    [Test]
    public void RoamUnwinnableTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 6);
        UnitDefinition strong = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("strong", maxHitpoints: 500, initiative: 60, damage: 100),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack strongStack = EnemyStack(new WorldUnitId("strongUnit"), "Strong", player2.Id, new WorldPosition(1, 0), strong, 500);
        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player1.Id, new WorldPosition(0, 0), main, 100, 6, WorldStackOrder.Roam.Instance);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(strong)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(mainStack)
            .WithStack(strongStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.Empty);
        Assert.That(session.World.State.Stacks[mainStack.Id], Is.EqualTo(mainStack));
    }

    [Test]
    public void AttackStackOrderHurtTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 3);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 6);
        UnitDefinition strong = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("strong", maxHitpoints: 500, initiative: 60, damage: 100),
            movementPoints: 6, leadership: 6);
        UnitDefinition weak = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 10, initiative: 40, damage: 5),
                movementPoints: 6, leadership: 6) with
            {
                ExperienceKilled = 50
            };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack strongStack = EnemyStack(new WorldUnitId("strongUnit"), "Strong", player2.Id, new WorldPosition(1, 0), strong, 500);
        WorldStack weakStack = EnemyStack(new WorldUnitId("weakUnit"), "Weak", player2.Id, new WorldPosition(1, 2), weak, 10);
        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player1.Id, new WorldPosition(0, 1), main, 20, 6, new WorldStackOrder.AttackStack(strongStack.Id));

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(strong)
            .WithDefinition(weak)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(mainStack)
            .WithStack(strongStack)
            .WithStack(weakStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P2 125] Move Main -> (1, 0) (Defender wins)"]));
    }

    [Test]
    public void AutoFitBeforeAttackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 4);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 6);
        UnitDefinition weak = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 10, initiative: 40, damage: 5),
                movementPoints: 6, leadership: 6) with
            {
                ExperienceKilled = 50
            };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Undead, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player1.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);
        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player1.Id, city.Entrance, main, 100, 6, WorldStackOrder.Guard.Instance);
        WorldStack weakStack = EnemyStack(new WorldUnitId("weakUnit"), "Weak", player2.Id, new WorldPosition(4, 3), weak, 10);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(weak)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithCity(city)
            .WithStack(mainStack)
            .WithStack(weakStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P3 1000] AutoFit Main",
            "[P2 910] Move Main -> (4, 3) (Attacker wins)"
        ]));
        Assert.That(session.World.State.Stacks.Keys, Is.EqualTo([mainStack.Id]));
        Assert.That(session.Battle, Is.Null);
    }

    [Test]
    public void HealBeforeAttackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 4);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 6);
        UnitDefinition weak = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 10, initiative: 40, damage: 5),
                movementPoints: 6, leadership: 6) with
            {
                ExperienceKilled = 50
            };

        WorldPlayerBuildingDefinition temple = new(
            Id: new WorldPlayerBuildingDefinitionId("temple"),
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Heal,
            UnitBranch: null,
            Level: 1,
            Cost: WorldResourceSet.Empty,
            UnitIds: []);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([temple.Id], [temple.Id])
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Undead, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player1.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);
        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player1.Id, city.Entrance, main, 50, 6, WorldStackOrder.Guard.Instance);
        WorldStack weakStack = EnemyStack(new WorldUnitId("weakUnit"), "Weak", player2.Id, new WorldPosition(4, 3), weak, 10);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(weak)
            .WithDefinition(temple)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithCity(city)
            .WithStack(mainStack)
            .WithStack(weakStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P4 500] Heal Main mainLeader",
            "[P3 1000] AutoFit Main",
            "[P2 910] Move Main -> (4, 3) (Attacker wins)"
        ]));
        Assert.That(session.World.State.Stacks.Keys, Is.EqualTo([mainStack.Id]));
        Assert.That(session.World.State.Players[player1.Id].Resources, Is.EqualTo(new WorldResourceSet(50, 0, 0, 0, 0, 0)));
        Assert.That(session.Battle, Is.Null);
    }

    [Test]
    public void AutoFitAfterEconomyTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 4);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 6);
        WorldPlayerBuildingDefinition building = new(
            Id: new WorldPlayerBuildingDefinitionId("building"),
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Heal,
            UnitBranch: null,
            Level: 1,
            Cost: new WorldResourceSet(100, 0, 0, 0, 0, 0),
            UnitIds: []);
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([building.Id], [])
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Undead, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player1.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);
        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player1.Id, city.Entrance, main, 100, 6, WorldStackOrder.Guard.Instance);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(building)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithCity(city)
            .WithStack(mainStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P1 0] Build building",
            "[P3 1000] AutoFit Main"
        ]));
        Assert.That(
            session.World.State.Players[player1.Id].Buildings,
            Is.EqualTo(new WorldPlayerBuildings([building.Id], [building.Id])));
        Assert.That(session.World.State.Players[player1.Id].Resources, Is.EqualTo(WorldResourceSet.Empty));
    }

    [Test]
    public void TemplePriorityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 4);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 6);
        WorldPlayerBuildingDefinition barracks = new(
            Id: new WorldPlayerBuildingDefinitionId("barracks"),
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Unit,
            UnitBranch: null,
            Level: 1,
            Cost: new WorldResourceSet(50, 0, 0, 0, 0, 0),
            UnitIds: []);
        WorldPlayerBuildingDefinition temple = new(
            Id: new WorldPlayerBuildingDefinitionId("temple"),
            NameTextId: new GlobalTextId("name"),
            DescriptionTextId: new GlobalTextId("description"),
            RequiredId: null,
            Category: WorldPlayerBuildingCategory.Heal,
            UnitBranch: null,
            Level: 1,
            Cost: new WorldResourceSet(100, 0, 0, 0, 0, 0),
            UnitIds: []);
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0),
            Buildings = new WorldPlayerBuildings([barracks.Id, temple.Id], [])
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Undead, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player1.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);
        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player1.Id, city.Entrance, main, 100, 6, WorldStackOrder.Guard.Instance);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(barracks)
            .WithDefinition(temple)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithCity(city)
            .WithStack(mainStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P1 0] Build temple",
            "[P3 1000] AutoFit Main"
        ]));
        Assert.That(
            session.World.State.Players[player1.Id].Buildings,
            Is.EqualTo(new WorldPlayerBuildings([barracks.Id, temple.Id], [temple.Id])));
    }

    [Test]
    public void AutoFitBeforeMoveTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 4);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 4);
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Undead, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player1.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);
        WorldStack mainStack = MyStack(
            new WorldUnitId("mainLeader"),
            "Main",
            player1.Id,
            city.Entrance,
            main,
            100,
            4,
            new WorldStackOrder.MoveToLocation(new ScenarioLocationId("target"), new WorldPosition(7, 3), 0));

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithCity(city)
            .WithStack(mainStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P3 1000] AutoFit Main",
            "[P1 1000] Move Main -> (6, 3)"
        ]));
        Assert.That(session.World.State.Stacks[mainStack.Id], Is.EqualTo(mainStack with
        {
            Position = new WorldPosition(6, 3),
            Direction = WorldStackDirection.SouthEast,
            Units = new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("mainLeader"), main), GridPosition.BackCenter, false)
                .Build(),
            MovementPoints = 0
        }));
    }

    [Test]
    public void AutoFitConvergeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 4);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 6);
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Undead, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player1.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);
        WorldStack mainStack = TestHelpers.SimpleStack(
                new WorldStackId("Main"),
                player1.Id,
                city.Entrance,
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("main"), main), GridPosition.FrontBottom, false)
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier) with { Hitpoints = 10 }, GridPosition.FrontCenter, false)
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier2"), soldier) with { Hitpoints = 20 }, GridPosition.FrontTop, false)
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier3"), soldier) with { Hitpoints = 30 }, GridPosition.BackBottom, false)
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier4"), soldier) with { Hitpoints = 40 }, GridPosition.BackCenter, false)
                    .Build(),
                movementPoints: 6) with
            {
                Order = WorldStackOrder.Guard.Instance
            };

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(soldier)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithCity(city)
            .WithStack(mainStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P3 1000] AutoFit Main"
        ]));
        Assert.That(session.World.State.Stacks[mainStack.Id], Is.EqualTo(mainStack with
        {
            Units = new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier2"), soldier) with { Hitpoints = 20 }, GridPosition.FrontCenter, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier) with { Hitpoints = 10 }, GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("main"), main), GridPosition.BackCenter, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier4"), soldier) with { Hitpoints = 40 }, GridPosition.BackBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier3"), soldier) with { Hitpoints = 30 }, GridPosition.BackTop, false)
                .Build()
        }));
        Assert.That(session.World.State.Fortifications[city.Id], Is.EqualTo(city));
    }

    [Test]
    public void AttackWithUnitLossTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 6);
        UnitDefinition support = TestHelpers.SimpleUnitDefinition("support", maxHitpoints: 10, initiative: 40, damage: 5);
        UnitDefinition enemy = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 55, initiative: 60, damage: 15),
                movementPoints: 6, leadership: 6) with
            {
                ExperienceKilled = 50
            };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack mainStack = TestHelpers.SimpleStack(
            new WorldStackId("Main"),
            player1.Id,
            new WorldPosition(1, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("main"), main), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("support"), support), GridPosition.FrontCenter, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("support2"), support), GridPosition.FrontTop, false)
                .Build(),
            movementPoints: 6);
        WorldStack enemyStack = EnemyStack(new WorldUnitId("enemyUnit"), "Enemy", player2.Id, new WorldPosition(2, 0), enemy, 55);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDifficulty(WorldDifficulty.Hard)
            .WithDefinition(main)
            .WithDefinition(support)
            .WithDefinition(enemy)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(mainStack)
            .WithStack(enemyStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P2 245] Move Main -> (2, 0) (Attacker wins)"]));
        Assert.That(session.World.State.Stacks.Keys, Is.EqualTo([mainStack.Id]));
        Assert.That(session.World.State.Stacks[mainStack.Id], Is.EqualTo(mainStack with
        {
            Direction = WorldStackDirection.SouthEast,
            Units = new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("main"), main) with { Experience = 25 }, GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("support"), support) with { Hitpoints = 0 }, GridPosition.FrontCenter, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("support2"), support) with { Experience = 25 }, GridPosition.FrontTop, false)
                .Build(),
            MovementPoints = 3,
            BattlesWon = 1
        }));
        Assert.That(session.Battle, Is.Null);
    }

    [Test]
    public void AttackGoodTradeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 6);
        UnitDefinition support = TestHelpers.SimpleUnitDefinition("support", maxHitpoints: 20, initiative: 40, damage: 5);
        UnitDefinition guard = TestHelpers.SimpleUnitDefinition("guard", maxHitpoints: 40, initiative: 60, damage: 15) with { ExperienceKilled = 200 };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack mainStack = TestHelpers.SimpleStack(
            new WorldStackId("Main"),
            player1.Id,
            new WorldPosition(2, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("main"), main) with { Experience = 90 }, GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("support"), support) with { Hitpoints = 10, Experience = 10 }, GridPosition.FrontCenter, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("support2"), support), GridPosition.FrontTop, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("support3"), support), GridPosition.BackBottom, false)
                .Build(),
            movementPoints: 6);

        WorldRuin ruin = new(
            Id: new WorldFortificationId("Ruin1"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(2, 2), FortificationCalculator.RuinFootprint),
            Variant: new WorldObjectVariant(0),
            Garrison: new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("guard"), guard), GridPosition.FrontBottom, false)
                .Build(),
            Resources: new WorldResourceSet(200, 0, 0, 0, 0, 0),
            Item: null,
            LooterId: null,
            AiPriority: 3);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDifficulty(WorldDifficulty.Hard)
            .WithDefinition(main)
            .WithDefinition(support)
            .WithDefinition(guard)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithStack(mainStack)
            .WithRuin(ruin)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P2 579] Move Main -> (2, 2) (Attacker wins)"]));
        Assert.That(session.World.State.Fortifications[ruin.Id], Is.EqualTo(ruin with
        {
            Garrison = UnitGrid<WorldUnit>.Empty,
            LooterId = player1.Id
        }));
        Assert.That(session.Battle, Is.Null);
    }

    [Test]
    public void AttackBadTradeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 4);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 6);
        UnitDefinition support = TestHelpers.SimpleUnitDefinition("support", maxHitpoints: 20, initiative: 40, damage: 5);
        UnitDefinition guard = TestHelpers.SimpleUnitDefinition("guard", maxHitpoints: 40, initiative: 60, damage: 15) with { ExperienceKilled = 40 };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Heretic, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player2.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("guard"), guard), GridPosition.FrontBottom, false)
                .Build());

        WorldStack mainStack = TestHelpers.SimpleStack(
            new WorldStackId("Main"),
            player1.Id,
            new WorldPosition(4, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("main"), main), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("support"), support) with { Hitpoints = 10, Experience = 100 }, GridPosition.FrontCenter, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("support2"), support), GridPosition.FrontTop, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("support3"), support), GridPosition.BackBottom, false)
                .Build(),
            movementPoints: 6);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDifficulty(WorldDifficulty.Hard)
            .WithDefinition(main)
            .WithDefinition(support)
            .WithDefinition(guard)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithCity(city)
            .WithStack(mainStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.Empty);
        Assert.That(session.World.State.Stacks[mainStack.Id], Is.EqualTo(mainStack));
        Assert.That(session.World.State.Fortifications[city.Id], Is.EqualTo(city));
        Assert.That(session.Battle, Is.Null);
    }

    [Test]
    public void AttackLeaderLossTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition frail = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("frail", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);
        UnitDefinition ally = TestHelpers.SimpleUnitDefinition("ally", maxHitpoints: 100, initiative: 45, damage: 30);
        UnitDefinition enemy = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 55, initiative: 60, damage: 15),
                movementPoints: 6, leadership: 6) with
            {
                ExperienceKilled = 50
            };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack mainStack = TestHelpers.SimpleStack(
            new WorldStackId("Main"),
            player1.Id,
            new WorldPosition(1, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("frail"), frail), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("ally"), ally), GridPosition.FrontCenter, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("ally2"), ally), GridPosition.FrontTop, false)
                .Build(),
            movementPoints: 6);
        WorldStack enemyStack = EnemyStack(new WorldUnitId("enemyUnit"), "Enemy", player2.Id, new WorldPosition(2, 0), enemy, 55);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDifficulty(WorldDifficulty.Hard)
            .WithDefinition(frail)
            .WithDefinition(ally)
            .WithDefinition(enemy)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(mainStack)
            .WithStack(enemyStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P2 26] Move Main -> (2, 0) (Attacker wins)"]));
        Assert.That(session.World.State.Stacks.Keys, Is.EqualTo([mainStack.Id]));
        Assert.That(session.World.State.Stacks[mainStack.Id], Is.EqualTo(mainStack with
        {
            Direction = WorldStackDirection.SouthEast,
            Units = new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("frail"), frail) with { Hitpoints = 0 }, GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("ally"), ally) with { Experience = 25 }, GridPosition.FrontCenter, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("ally2"), ally) with { Experience = 25 }, GridPosition.FrontTop, false)
                .Build(),
            MovementPoints = 3,
            BattlesWon = 1
        }));
        Assert.That(session.Battle, Is.Null);
    }

    [Test]
    public void AttackHalfStackLossTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 6);
        UnitDefinition support = TestHelpers.SimpleUnitDefinition("support", maxHitpoints: 10, initiative: 40, damage: 5);
        UnitDefinition enemy = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("enemy", maxHitpoints: 50, initiative: 60, damage: 15),
                movementPoints: 6, leadership: 6) with
            {
                ExperienceKilled = 50
            };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack mainStack = TestHelpers.SimpleStack(
            new WorldStackId("Main"),
            player1.Id,
            new WorldPosition(1, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("main"), main), GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("support"), support), GridPosition.FrontCenter, false)
                .Build(),
            movementPoints: 6);
        WorldStack enemyStack = EnemyStack(new WorldUnitId("enemyUnit"), "Enemy", player2.Id, new WorldPosition(2, 0), enemy, 50);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDifficulty(WorldDifficulty.Hard)
            .WithDefinition(main)
            .WithDefinition(support)
            .WithDefinition(enemy)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(mainStack)
            .WithStack(enemyStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P2 173] Move Main -> (2, 0) (Attacker wins)"]));
        Assert.That(session.World.State.Stacks.Keys, Is.EqualTo([mainStack.Id]));
        Assert.That(session.World.State.Stacks[mainStack.Id], Is.EqualTo(mainStack with
        {
            Direction = WorldStackDirection.SouthEast,
            Units = new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("main"), main) with { Experience = 50 }, GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("support"), support) with { Hitpoints = 0 }, GridPosition.FrontCenter, false)
                .Build(),
            MovementPoints = 3,
            BattlesWon = 1
        }));
        Assert.That(session.Battle, Is.Null);
    }

    [Test]
    public void AttackStackTradeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 6);
        UnitDefinition support = TestHelpers.SimpleUnitDefinition("support", maxHitpoints: 20, initiative: 40, damage: 5);
        UnitDefinition brute = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("brute", maxHitpoints: 55, initiative: 60, damage: 15),
                movementPoints: 6, leadership: 6) with
            {
                ExperienceKilled = 200
            };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack mainStack = TestHelpers.SimpleStack(
            new WorldStackId("Main"),
            player1.Id,
            new WorldPosition(1, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("main"), main) with { Experience = 90 }, GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("support"), support) with { Hitpoints = 10, Experience = 10 }, GridPosition.FrontCenter, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("support2"), support), GridPosition.FrontTop, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("support3"), support), GridPosition.BackBottom, false)
                .Build(),
            movementPoints: 6);
        WorldStack bruteStack = EnemyStack(new WorldUnitId("bruteUnit"), "Brute", player2.Id, new WorldPosition(2, 0), brute, 55);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDifficulty(WorldDifficulty.Hard)
            .WithDefinition(main)
            .WithDefinition(support)
            .WithDefinition(brute)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(mainStack)
            .WithStack(bruteStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P2 579] Move Main -> (2, 0) (Attacker wins)"]));
        Assert.That(session.World.State.Stacks.Keys, Is.EqualTo([mainStack.Id]));
        Assert.That(session.World.State.Stacks[mainStack.Id], Is.EqualTo(mainStack with
        {
            Direction = WorldStackDirection.SouthEast,
            Units = new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("main"), main) with { Experience = 156 }, GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("support"), support) with { Hitpoints = 0, Experience = 10 }, GridPosition.FrontCenter, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("support2"), support) with { Experience = 66 }, GridPosition.FrontTop, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("support3"), support) with { Experience = 66 }, GridPosition.BackBottom, false)
                .Build(),
            MovementPoints = 3,
            BattlesWon = 1
        }));
        Assert.That(session.Battle, Is.Null);
    }

    [Test]
    public void AttackTradeCapTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 4);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 6);
        UnitDefinition support = TestHelpers.SimpleUnitDefinition("support", maxHitpoints: 20, initiative: 40, damage: 5);
        UnitDefinition guard = TestHelpers.SimpleUnitDefinition("guard", maxHitpoints: 40, initiative: 60, damage: 15) with { ExperienceKilled = 200 };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Heretic, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player2.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("guard"), guard), GridPosition.FrontBottom, false)
                .Build());

        WorldStack mainStack = TestHelpers.SimpleStack(
            new WorldStackId("Main"),
            player1.Id,
            new WorldPosition(4, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("main"), main) with { Experience = 90 }, GridPosition.FrontBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("support"), support) with { Hitpoints = 10, Experience = 10 }, GridPosition.FrontCenter, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("support2"), support), GridPosition.FrontTop, false)
                .Build(),
            movementPoints: 6);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDifficulty(WorldDifficulty.Hard)
            .WithDefinition(main)
            .WithDefinition(support)
            .WithDefinition(guard)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithCity(city)
            .WithStack(mainStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 124] Move Main -> (3, 3) (Attacker wins)",
            "[P3 1000] AutoFit Main"
        ]));
        Assert.That(session.World.State.Stacks.Keys, Is.EqualTo([mainStack.Id]));
        Assert.That(session.World.State.Stacks[mainStack.Id], Is.EqualTo(mainStack with
        {
            Position = new WorldPosition(3, 3),
            Direction = WorldStackDirection.NorthWest,
            Units = new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("main"), main) with { Experience = 190 }, GridPosition.BackCenter, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("support2"), support) with { Experience = 100 }, GridPosition.BackBottom, false)
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("support"), support) with { Hitpoints = 0, Experience = 10 }, GridPosition.BackTop, false)
                .Build(),
            MovementPoints = 3,
            BattlesWon = 1
        }));
        Assert.That(session.World.State.Fortifications[city.Id], Is.EqualTo(city with
        {
            OwnerId = player1.Id,
            Garrison = UnitGrid<WorldUnit>.Empty
        }));
        Assert.That(session.Battle, Is.Null);
    }

    [Test]
    public void HoldGoldForLeaderTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^..."
        ]);

        UnitDefinition fighterDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("fighter", maxHitpoints: 100, initiative: 50, damage: 50),
                movementPoints: 2, leadership: 6) with
            {
                PurchaseCost = new WorldResourceSet(100, 0, 0, 0, 0, 0)
            };

        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 60, initiative: 40, damage: 15);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(60, 0, 0, 0, 0, 0)
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldRaceDefinition race = new(
            WorldRace.Undead,
            new GlobalTextId("Undead"),
            WorldResourceSet.Empty,
            WorldMapTerrain.Undead,
            true,
            35,
            [],
            [fighterDefinition.Id],
            [soldierDefinition.Id],
            [],
            []);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(fighterDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithRace(race)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.Empty);
        Assert.That(session.World.State.Players[player.Id].Resources, Is.EqualTo(new WorldResourceSet(60, 0, 0, 0, 0, 0)));
    }

    [Test]
    public void LeaderMissingPrerequisiteTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^..."
        ]);

        UnitDefinition fighterDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("fighter", maxHitpoints: 100, initiative: 50, damage: 50),
                movementPoints: 2, leadership: 6) with
            {
                PurchaseCost = new WorldResourceSet(100, 0, 0, 0, 0, 0),
                RequiredBuildingId = new WorldPlayerBuildingDefinitionId("barracks")
            };

        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 60, initiative: 40, damage: 15);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(60, 0, 0, 0, 0, 0)
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldRaceDefinition race = new(
            WorldRace.Undead,
            new GlobalTextId("Undead"),
            WorldResourceSet.Empty,
            WorldMapTerrain.Undead,
            true,
            35,
            [],
            [fighterDefinition.Id],
            [soldierDefinition.Id],
            [],
            []);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(fighterDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithRace(race)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P0 467] Purchase soldier -> capital (0, 0)"
        ]));
    }

    [Test]
    public void PreferAffordableLeaderTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^..."
        ]);

        UnitDefinition fighterDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("fighter", maxHitpoints: 100, initiative: 50, damage: 50),
                movementPoints: 2, leadership: 6) with
            {
                Race = WorldRace.Undead,
                PurchaseCost = new WorldResourceSet(500, 0, 0, 0, 0, 0)
            };

        UnitDefinition explorerDefinition = TestHelpers.SimpleUnitDefinition("explorer", maxHitpoints: 80, initiative: 50, damage: 20) with
        {
            Category = UnitCategory.Leader,
            Race = WorldRace.Undead,
            LeaderCategory = UnitLeaderCategory.Explorer,
            Leadership = 6,
            MovementPoints = 2,
            PurchaseCost = new WorldResourceSet(50, 0, 0, 0, 0, 0)
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(60, 0, 0, 0, 0, 0)
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldRaceDefinition race = new(
            WorldRace.Undead,
            new GlobalTextId("Undead"),
            WorldResourceSet.Empty,
            WorldMapTerrain.Undead,
            true,
            35,
            [],
            [fighterDefinition.Id, explorerDefinition.Id],
            [],
            ["Nigosh"],
            ["Orissia"]);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(fighterDefinition)
            .WithDefinition(explorerDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithRace(race)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 167] Leader explorer -> capital",
            "[P3 1000] AutoFit Stack0001"
        ]));
    }

    [Test]
    public void OccupiedCapitalEconomyTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^..."
        ]);

        UnitDefinition fighterDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("fighter", maxHitpoints: 100, initiative: 50, damage: 50),
                movementPoints: 2, leadership: 6) with
            {
                PurchaseCost = new WorldResourceSet(100, 0, 0, 0, 0, 0)
            };

        UnitDefinition thiefDefinition = TestHelpers.SimpleThiefDefinition() with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("thief"), initiative: 50, damage: 20, accuracy: 100, reach: UnitAttackReach.Any),
            Leadership = 6,
            MovementPoints = 2
        };

        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 60, initiative: 40, damage: 15) with
        {
            PurchaseCost = new WorldResourceSet(50, 0, 0, 0, 0, 0)
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(200, 0, 0, 0, 0, 0)
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldStack blockingStack = TestHelpers.SimpleStack(
                new WorldStackId("Blocking"),
                player.Id,
                capital.Entrance,
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thiefDefinition), GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 0) with
            {
                AiIgnore = true
            };

        WorldRaceDefinition race = new(
            WorldRace.Undead,
            new GlobalTextId("Undead"),
            WorldResourceSet.Empty,
            WorldMapTerrain.Undead,
            true,
            35,
            [],
            [fighterDefinition.Id],
            [soldierDefinition.Id],
            [],
            []);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(fighterDefinition)
            .WithDefinition(thiefDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .WithStack(blockingStack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithRace(race)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P0 700] Purchase soldier -> capital (0, 0)"
        ]));
        Assert.That(session.World.State.Players[player.Id].Resources, Is.EqualTo(new WorldResourceSet(150, 0, 0, 0, 0, 0)));
    }

    [Test]
    public void NeutralRelationStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition attackerDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50),
                movementPoints: 6, leadership: 6) with
            {
                PurchaseCost = new WorldResourceSet(100, 0, 0, 0, 0, 0)
            };

        UnitDefinition defenderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5),
                movementPoints: 6, leadership: 6) with
            {
                PurchaseCost = new WorldResourceSet(50, 0, 0, 0, 0, 0)
            };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldRelation relation = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            Current: 49,
            Previous: 49,
            Alliance: false,
            AllianceTurn: 0,
            PermanentWar: false,
            PermanentAlliance: false);

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(1, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("attacker"), attackerDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(2, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defenderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(relation)
            .WithStack(stack1)
            .WithStack(stack2)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        IReadOnlyList<TestWorldAiRunner.Trace> report = runner.PlayTurn();

        Assert.That(TestWorldAiTraceFormatter.Format(report), Is.EqualTo([
            "[P2 910] Move Stack1 -> (2, 0) (Attacker wins)"
        ]));
        Assert.That(session.World.State.Stacks.Keys, Is.EqualTo([stack1.Id]));
    }

    [Test]
    public void AlliedStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition attackerDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50),
                movementPoints: 6, leadership: 6) with
            {
                PurchaseCost = new WorldResourceSet(100, 0, 0, 0, 0, 0)
            };

        UnitDefinition defenderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5),
                movementPoints: 6, leadership: 6) with
            {
                PurchaseCost = new WorldResourceSet(50, 0, 0, 0, 0, 0)
            };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldRelation alliance = new(
            new WorldRelationId(WorldRace.Undead, WorldRace.Heretic),
            Current: 100,
            Previous: 100,
            Alliance: true,
            AllianceTurn: 1,
            PermanentWar: false,
            PermanentAlliance: false);

        WorldStack stack1 = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player1.Id,
            new WorldPosition(1, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("attacker"), attackerDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);

        WorldStack stack2 = TestHelpers.SimpleStack(
            new WorldStackId("Stack2"),
            player2.Id,
            new WorldPosition(2, 0),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defenderDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRelation(alliance)
            .WithStack(stack1)
            .WithStack(stack2)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        IReadOnlyList<TestWorldAiRunner.Trace> report = runner.PlayTurn();

        Assert.That(TestWorldAiTraceFormatter.Format(report), Is.Empty);
        Assert.That(session.World.State.Stacks[stack2.Id], Is.EqualTo(stack2));
    }

    [Test]
    public void ExploreOutmatchedTargetTest()
    {
        WorldMap map = TestHelpers.SimpleMap(9, 1);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 6);
        UnitDefinition strong = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("strong", maxHitpoints: 500, initiative: 60, damage: 100),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar(["........?"])
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player1.Id, new WorldPosition(4, 0), main, 100, 6, WorldStackOrder.Normal.Instance);
        WorldStack strongStack = EnemyStack(new WorldUnitId("strongUnit"), "Strong", player2.Id, new WorldPosition(0, 0), strong, 500);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(strong)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(mainStack)
            .WithStack(strongStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P1 1000] Move Main -> (7, 0)"]));
    }

    [Test]
    public void ExploreAroundStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(9, 2);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 6);
        UnitDefinition strong = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("strong", maxHitpoints: 500, initiative: 60, damage: 100),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar([
                "?.......?",
                "?.......?"
            ])
        };
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player1.Id, new WorldPosition(4, 1), main, 100, 6, WorldStackOrder.Normal.Instance);
        WorldStack guardStack = EnemyStack(new WorldUnitId("guardUnit"), "Guard", player2.Id, new WorldPosition(2, 0), strong, 500);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(strong)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(mainStack)
            .WithStack(guardStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P1 1000] Move Main -> (7, 0)"]));
    }

    [Test]
    public void HurtStackDistantEnemyTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 6);
        UnitDefinition weak = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player1.Id, new WorldPosition(0, 0), main, 30, 6, WorldStackOrder.Normal.Instance);
        WorldStack weakStack = EnemyStack(new WorldUnitId("weakUnit"), "Weak", player2.Id, new WorldPosition(2, 0), weak, 10);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(weak)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(mainStack)
            .WithStack(weakStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.Empty);
    }

    [Test]
    public void HurtStackAdjacentEnemyTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 6);
        UnitDefinition weak = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 10, initiative: 40, damage: 5),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player1.Id, new WorldPosition(0, 0), main, 30, 6, WorldStackOrder.Normal.Instance);
        WorldStack weakStack = EnemyStack(new WorldUnitId("weakUnit"), "Weak", player2.Id, new WorldPosition(1, 0), weak, 10);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(weak)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(mainStack)
            .WithStack(weakStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P2 847] Move Main -> (1, 0) (Attacker wins)"]));
    }

    [TestCase(WorldDifficulty.Easy, "[P2 192] Move Summon -> (1, 0) (Defender wins)")]
    [TestCase(WorldDifficulty.Average, "[P2 192] Move Summon -> (1, 0) (Defender wins)")]
    [TestCase(WorldDifficulty.Hard, "[P2 192] Move Summon -> (1, 0) (Defender wins)")]
    [TestCase(WorldDifficulty.VeryHard, "[P2 192] Move Summon -> (1, 0) (Defender wins)")]
    public void SummonAttackUnwinnableTest(WorldDifficulty difficulty, string expectedTrace)
    {
        WorldMap map = TestHelpers.SimpleMap(5, 1);

        UnitDefinition summon = Leader("summon", damage: 50, movementPoints: 6) with
        {
            Category = UnitCategory.Summon,
            Lifetime = 2
        };
        UnitDefinition strong = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("strong", maxHitpoints: 500, initiative: 60, damage: 100),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Heretic, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack strongStack = EnemyStack(new WorldUnitId("strongUnit"), "Strong", player2.Id, new WorldPosition(1, 0), strong, 500);
        WorldStack summonStack = MyStack(new WorldUnitId("summonLeader"), "Summon", player1.Id, new WorldPosition(0, 0), summon, 100, 6, WorldStackOrder.Normal.Instance);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDifficulty(difficulty)
            .WithDefinition(summon)
            .WithDefinition(strong)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(summonStack)
            .WithStack(strongStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([expectedTrace]));
    }

    [Test]
    public void SummonAttackNeutralCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition summon = Leader("summon", damage: 50, movementPoints: 6) with
        {
            Category = UnitCategory.Summon,
            Lifetime = 2
        };
        UnitDefinition strong = TestHelpers.SimpleUnitDefinition("strong", maxHitpoints: 500, initiative: 60, damage: 100);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Neutral, 1), WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Heretic, map);
        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Neutral, map);

        WorldStack summonStack = MyStack(new WorldUnitId("summonLeader"), "Summon", player1.Id, new WorldPosition(4, 4), summon, 100, 6, WorldStackOrder.Normal.Instance);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("City"),
            cityDefinition,
            neutral.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("strong"), strong), GridPosition.FrontBottom, false)
                .Build());

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDifficulty(WorldDifficulty.Hard)
            .WithDefinition(summon)
            .WithDefinition(strong)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithStack(summonStack)
            .WithCity(city)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P2 185] Move Summon -> (3, 3) (Defender wins)"]));
    }

    [Test]
    public void PurchaseNewUnitTypeTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^^^^",
            "^^^^..."
        ]);

        UnitDefinition fighterDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("fighter", maxHitpoints: 100, initiative: 50, damage: 50),
            movementPoints: 2, leadership: 6);

        UnitDefinition rangedDefinition = TestHelpers.SimpleUnitDefinition("ranged", maxHitpoints: 30, initiative: 50, damage: 20) with
        {
            PurchaseCost = new WorldResourceSet(60, 0, 0, 0, 0, 0)
        };

        UnitDefinition meleeDefinition = TestHelpers.SimpleUnitDefinition("melee", maxHitpoints: 60, initiative: 40, damage: 15) with
        {
            Attack = TestHelpers.SimpleAttack(new UnitAttackDefinitionId("melee"), initiative: 40, damage: 15, accuracy: 100, reach: UnitAttackReach.Adjacent)
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(65, 0, 0, 0, 0, 0)
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack"),
                player.Id,
                capital.Entrance,
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("fighter"), fighterDefinition), GridPosition.FrontBottom, false)
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("melee"), meleeDefinition), GridPosition.FrontCenter, false)
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("melee2"), meleeDefinition), GridPosition.FrontTop, false)
                    .Build(),
                movementPoints: 0) with
            {
                AiIgnore = true
            };

        WorldRaceDefinition race = new(
            WorldRace.Undead,
            new GlobalTextId("Undead"),
            WorldResourceSet.Empty,
            WorldMapTerrain.Undead,
            true,
            35,
            [],
            [],
            [rangedDefinition.Id, meleeDefinition.Id],
            [],
            []);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(fighterDefinition)
            .WithDefinition(rangedDefinition)
            .WithDefinition(meleeDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithRace(race)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        IReadOnlyList<TestWorldAiRunner.Trace> report = runner.PlayTurn();

        Assert.That(TestWorldAiTraceFormatter.Format(report), Is.EqualTo([
            "[P0 431] Purchase ranged -> capital (0, 0)"
        ]));
    }

    [Test]
    public void SummonAttackNearTargetTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 3);

        UnitDefinition summon = Leader("summon", damage: 50, movementPoints: 40) with
        {
            Category = UnitCategory.Summon,
            Lifetime = 2
        };
        UnitDefinition near = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("near", maxHitpoints: 100, initiative: 50, damage: 20),
            movementPoints: 6, leadership: 6);
        UnitDefinition far = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("far", maxHitpoints: 100, initiative: 50, damage: 20),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Heretic, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack summonStack = MyStack(new WorldUnitId("summonLeader"), "Summon", player1.Id, new WorldPosition(0, 1), summon, 100, 40, WorldStackOrder.Normal.Instance);
        WorldStack nearStack = EnemyStack(new WorldUnitId("nearUnit"), "Near", player2.Id, new WorldPosition(2, 1), near, 60);
        WorldStack farStack = EnemyStack(new WorldUnitId("farUnit"), "Far", player2.Id, new WorldPosition(7, 1), far, 10);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(summon)
            .WithDefinition(near)
            .WithDefinition(far)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(summonStack)
            .WithStack(nearStack)
            .WithStack(farStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P2 428] Move Summon -> (2, 1) (Attacker wins)"]));
    }

    [Test]
    public void SummonApproachDistantTargetTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 3);

        UnitDefinition summon = Leader("summon", damage: 50, movementPoints: 40) with
        {
            Category = UnitCategory.Summon,
            Lifetime = 2
        };
        UnitDefinition far = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("far", maxHitpoints: 100, initiative: 50, damage: 20),
            movementPoints: 6, leadership: 6);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Heretic, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack summonStack = MyStack(new WorldUnitId("summonLeader"), "Summon", player1.Id, new WorldPosition(0, 1), summon, 100, 40, WorldStackOrder.Normal.Instance);
        WorldStack farStack = EnemyStack(new WorldUnitId("farUnit"), "Far", player2.Id, new WorldPosition(7, 1), far, 10);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(summon)
            .WithDefinition(far)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(summonStack)
            .WithStack(farStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo(["[P1 250] Move Summon -> (5, 1)", "[P2 534] Move Summon -> (7, 1) (Attacker wins)"]));
    }

    [Test]
    public void SecureCityExitStackTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 10);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 6);
        UnitDefinition lord = Leader("lord", damage: 50, movementPoints: 6);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1), WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("City"),
            cityDefinition,
            player1.Id,
            new WorldPosition(3, 9),
            UnitGrid<WorldUnit>.Empty);

        WorldStack lordStack = MyStack(new WorldUnitId("lordLeader"), "Lord", player1.Id, new WorldPosition(3, 9), lord, 95, 6, WorldStackOrder.Stand.Instance);
        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player1.Id, new WorldPosition(4, 9), main, 100, 6, new WorldStackOrder.SecureCity(city.Id));

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(lord)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithCity(city)
            .WithStack(lordStack)
            .WithStack(mainStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P3 1000] AutoFit Lord",
            "[P2 1000] Exit Lord",
            "[P1 0] Move Main -> (3, 9)",
            "[P3 1000] AutoFit Main"
        ]));
    }

    [Test]
    public void SecureCityExitStackWoundedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(10, 10);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 6);
        UnitDefinition lord = Leader("lord", damage: 50, movementPoints: 6);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1), WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("City"),
            cityDefinition,
            player1.Id,
            new WorldPosition(3, 9),
            UnitGrid<WorldUnit>.Empty);

        WorldStack lordStack = MyStack(new WorldUnitId("lordLeader"), "Lord", player1.Id, new WorldPosition(3, 9), lord, 90, 6, WorldStackOrder.Stand.Instance);
        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player1.Id, new WorldPosition(4, 9), main, 100, 6, new WorldStackOrder.SecureCity(city.Id));

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(lord)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithCity(city)
            .WithStack(lordStack)
            .WithStack(mainStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P3 1000] AutoFit Lord"
        ]));
    }

    [Test]
    public void MoveToLocationBlockerTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 3);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 12);
        UnitDefinition weak = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 10, initiative: 40, damage: 5),
                movementPoints: 6, leadership: 6) with
            {
                ExperienceKilled = 50
            };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldCanonicalStackOrder order = new WorldStackOrder.MoveToLocation(new ScenarioLocationId("loc"), new WorldPosition(6, 1), 0);
        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player1.Id, new WorldPosition(0, 1), main, 100, 12, order);
        WorldStack weakStack = EnemyStack(new WorldUnitId("weakUnit"), "Weak", player2.Id, new WorldPosition(2, 1), weak, 10);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(weak)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(mainStack)
            .WithStack(weakStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 910] Move Main -> (6, 1) (Attacker wins)",
            "[P1 1000] Move Main -> (3, 0)"
        ]));
    }

    [Test]
    public void DefendLocationBlockerTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 3);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 12);
        UnitDefinition weak = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 10, initiative: 40, damage: 5),
                movementPoints: 6, leadership: 6) with
            {
                ExperienceKilled = 50
            };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldCanonicalStackOrder order = new WorldStackOrder.DefendLocation(new ScenarioLocationId("loc"), new WorldPosition(6, 1), 0);
        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player1.Id, new WorldPosition(0, 1), main, 100, 12, order);
        WorldStack weakStack = EnemyStack(new WorldUnitId("weakUnit"), "Weak", player2.Id, new WorldPosition(2, 1), weak, 10);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(weak)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(mainStack)
            .WithStack(weakStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 910] Move Main -> (6, 1) (Attacker wins)",
            "[P1 1000] Move Main -> (3, 0)"
        ]));
    }

    [Test]
    public void DefendStackBlockerTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 3);

        UnitDefinition main = Leader("main", damage: 50, movementPoints: 12);
        UnitDefinition ally = Leader("ally", damage: 50, movementPoints: 0);
        UnitDefinition weak = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("weak", maxHitpoints: 10, initiative: 40, damage: 5),
                movementPoints: 6, leadership: 6) with
            {
                ExperienceKilled = 50
            };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack allyStack = MyStack(new WorldUnitId("allyLeader"), "Ally", player1.Id, new WorldPosition(6, 1), ally, 100, 0, WorldStackOrder.Stand.Instance);
        WorldStack mainStack = MyStack(new WorldUnitId("mainLeader"), "Main", player1.Id, new WorldPosition(0, 1), main, 100, 12, new WorldStackOrder.DefendStack(allyStack.Id));
        WorldStack weakStack = EnemyStack(new WorldUnitId("weakUnit"), "Weak", player2.Id, new WorldPosition(2, 1), weak, 10);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(main)
            .WithDefinition(ally)
            .WithDefinition(weak)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(allyStack)
            .WithStack(mainStack)
            .WithStack(weakStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 910] Move Main -> (5, 0) (Attacker wins)",
            "[P1 1000] Move Main -> (3, 0)"
        ]));
    }

    [Test]
    public void LeaderQuotaOrderedStackTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^^^",
            "^^^^^^^^",
            "^^^^^^^^",
            "^^^^^^^^",
            "^^^^...."
        ]);

        UnitDefinition fighterDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("fighter", maxHitpoints: 100, initiative: 50, damage: 50),
                movementPoints: 6, leadership: 6) with
            {
                Race = WorldRace.Undead,
                PurchaseCost = new WorldResourceSet(100, 0, 0, 0, 0, 0)
            };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0)
        };

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            player.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldRaceDefinition race = new(
            WorldRace.Undead,
            new GlobalTextId("Undead"),
            WorldResourceSet.Empty,
            WorldMapTerrain.Undead,
            true,
            35,
            [],
            [fighterDefinition.Id],
            [],
            ["Nigosh"],
            ["Orissia"]);

        WorldStack guardStack = MyStack(new WorldUnitId("guard"), "Guard", player.Id, new WorldPosition(7, 4), fighterDefinition, 100, 0, WorldStackOrder.Stand.Instance);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(fighterDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCapital(capital)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithRace(race)
            .WithStack(guardStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 0] Leader fighter -> capital",
            "[P3 1000] AutoFit Stack0001"
        ]));
    }

    [Test]
    public void CreateRodTest()
    {
        WorldMap map = TestHelpers.SimpleMap(6, 3);

        UnitDefinition planterDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("planter", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 2) with
            {
                LeaderCategory = UnitLeaderCategory.Rod,
                LeaderAbilities = LeaderAbility.Rod
            };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(150, 0, 0, 0, 0, 0)
        };

        WorldStack planterStack = MyStack(new WorldUnitId("planter"), "Planter", player.Id, new WorldPosition(0, 1), planterDefinition, 100, 6, WorldStackOrder.Normal.Instance);
        WorldResourceNode node = new(new WorldResourceNodeId("node"), new WorldPosition(4, 1), WorldResourceType.Gold, AiPriority: 3);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(planterDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(planterStack)
            .WithResourceNode(node)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 666] Move Planter -> (2, 0)",
            "[P2 1000] Create rod Planter -> (3, 0)"
        ]));
    }

    [Test]
    public void RemoveRodTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 3);

        UnitDefinition planterDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("planter", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 10, leadership: 2) with
            {
                LeaderCategory = UnitLeaderCategory.Rod,
                LeaderAbilities = LeaderAbility.Rod
            };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldStack planterStack = MyStack(new WorldUnitId("planter"), "Planter", player1.Id, new WorldPosition(0, 1), planterDefinition, 100, 10, WorldStackOrder.Normal.Instance);
        WorldRod enemyRod = new(new WorldRodId("enemyRod"), player2.Id, new WorldPosition(5, 1));

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(planterDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithRod(enemyRod)
            .WithStack(planterStack)
            .BuildSession();

        TestWorldAiRunner runner = new(session, engine);

        Assert.That(TestWorldAiTraceFormatter.Format(runner.PlayTurn()), Is.EqualTo([
            "[P2 600] Move Planter -> (3, 0)",
            "[P2 1000] Remove rod Planter -> (5, 1)"
        ]));
        Assert.That(session.World.State.Rods.Values, Is.Empty);
    }
}
