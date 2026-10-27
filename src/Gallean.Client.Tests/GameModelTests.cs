using Gallean.Common;
using Gallean.Engine;
using Gallean.Engine.Tests;
using Gallean.Tests.Common;

namespace Gallean.Client.Tests;

public sealed class GameModelTests
{
    [Test]
    public async Task ExecuteAiActionTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        UnitDefinition attackerDefinition = TestHelpers.SimpleUnitDefinition("attacker", maxHitpoints: 100, initiative: 50, damage: 50) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 6,
            MovementPoints = 6
        };
        UnitDefinition defenderDefinition = TestHelpers.SimpleUnitDefinition("defender", maxHitpoints: 10, initiative: 40, damage: 5) with
        {
            Category = UnitCategory.Leader,
            LeaderCategory = UnitLeaderCategory.Fighter,
            Leadership = 6,
            MovementPoints = 6
        };
        GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };

        WorldPlayer ai = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with { IsAi = true };
        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack aiStack = TestHelpers.SimpleStack(
            new WorldStackId("AiStack"),
            ai.Id,
            new WorldPosition(1, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("attacker"), attackerDefinition), GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        WorldStack humanStack = TestHelpers.SimpleStack(
            new WorldStackId("HumanStack"),
            human.Id,
            new WorldPosition(2, 0),
            new UnitGrid<WorldUnit>.Builder().Place(TestHelpers.SimpleUnit(new WorldUnitId("defender"), defenderDefinition), GridPosition.FrontBottom, false).Build(),
            movementPoints: 6);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithGlobals(globals)
            .WithDefinition(attackerDefinition)
            .WithDefinition(defenderDefinition)
            .WithDefinition(TestHelpers.SimpleLord(ai.LordId, ai.Race, LordCategory.Warrior))
            .WithPlayer(ai)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithStack(aiStack)
            .WithStack(humanStack)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        WorldState expectedWorld = session.World.State with
        {
            Stacks = session.World.State.Stacks.SetItem(
                aiStack.Id,
                aiStack with { Direction = WorldStackDirection.SouthEast, MovementPoints = 3 }),
            AiMemories = session.World.State.AiMemories.SetItem(
                ai.Id,
                new WorldAiMemory(new WorldAiTurnMemory(session.World.State.TurnInfo.TurnNumber, ai.Id, 1, 0, 0, []))
                    .WithStackMemory(aiStack.Id, new WorldAiStackMemory(
                        WorldStackOrder.Normal.Instance,
                        new WorldAiTarget.Stack(humanStack.Id))))
        };
        BattleState expectedBattle = WorldCommandHelpers.CreateStackBattleState(session.World.State, aiStack, humanStack, engine) with
        {
            CurrentUnitTurn = new BattleUnitTurn(new BattleUnitId(0), BattleUnitTurnType.Normal),
            TurnQueue = [new BattleUnitTurn(new BattleUnitId(1), BattleUnitTurnType.Normal)],
            WaitingQueue = []
        };
        GameSnapshot expectedSnapshot = new(
            expectedWorld,
            expectedBattle,
            ActiveActions: 0,
            ViewPlayerId: human.Id);

        await gameModel.ExecuteAiActionAsync();

        Assert.That(gameModel.Snapshot, Is.EqualTo(expectedSnapshot));
        BattleModel battle = Assert.NotNull(gameModel.ActiveBattle);
        Assert.That(battle.Position, Is.EqualTo(new WorldPosition(2, 0)));
    }
}
