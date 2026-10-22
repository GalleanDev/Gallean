using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine.Tests;

public sealed class WorldAiMemoryTests
{
    [Test]
    public void ReconcileNewTurnTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        GameSession session = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession()
            .Session;

        WorldAiMemory memory = new(new WorldAiTurnMemory(0, new WorldPlayerId(2), 3, 2, 1, [new WorldStackId("stack")]));

        WorldAiMemory reconciledMemory = memory.ReconcileWith(session.World.State);

        Assert.That(memory, Is.EqualTo(new WorldAiMemory(new WorldAiTurnMemory(0, new WorldPlayerId(2), 3, 2, 1, [new WorldStackId("stack")]))));
        Assert.That(reconciledMemory, Is.EqualTo(new WorldAiMemory(new WorldAiTurnMemory(1, player.Id))));
    }

    [Test]
    public void ReconcileCurrentTurnTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        GameSession session = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession()
            .Session;

        WorldTurnInfo turnInfo = session.World.State.TurnInfo;
        WorldAiMemory memory = new(new WorldAiTurnMemory(turnInfo.TurnNumber, turnInfo.CurrentPlayerId, 3, 2, 1, [new WorldStackId("stack")]));

        WorldAiMemory reconciledMemory = memory.ReconcileWith(session.World.State);

        Assert.That(reconciledMemory, Is.EqualTo(memory));
    }

    [Test]
    public void OrderTransitionTest()
    {
        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("stack"),
                new WorldPlayerId(3),
                new WorldPosition(0, 0),
                UnitGrid<WorldUnit>.Empty,
                movementPoints: 0) with
            {
                Order = WorldStackOrder.Guard.Instance
            };
        WorldAiMemory memory = new(new WorldAiTurnMemory(2, new WorldPlayerId(3)));

        WorldAiMemory updatedMemory = memory.WithStackMemory(
            stack.Id,
            new WorldAiStackMemory(WorldStackOrder.Roam.Instance, null));

        Assert.That(memory.GetStackMemory(stack), Is.EqualTo(new WorldAiStackMemory(WorldStackOrder.Normal.Instance, null)));
        Assert.That(updatedMemory.GetStackMemory(stack), Is.EqualTo(new WorldAiStackMemory(WorldStackOrder.Roam.Instance, null)));
        Assert.That(memory, Is.EqualTo(new WorldAiMemory(new WorldAiTurnMemory(2, new WorldPlayerId(3)))));
        Assert.That(
            updatedMemory,
            Is.EqualTo(new WorldAiMemory(
                new WorldAiTurnMemory(2, new WorldPlayerId(3)),
                ImmutableSortedDictionary<WorldStackId, WorldAiStackMemory>.Empty.SetItem(
                    stack.Id,
                    new WorldAiStackMemory(WorldStackOrder.Roam.Instance, null)))));
    }

    [Test]
    public void StackMemoryRoundtripTest()
    {
        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("stack"),
                new WorldPlayerId(3),
                new WorldPosition(0, 0),
                UnitGrid<WorldUnit>.Empty,
                movementPoints: 0) with
            {
                Order = WorldStackOrder.Guard.Instance
            };
        WorldAiMemory memory = new(new WorldAiTurnMemory(2, new WorldPlayerId(3)));

        WorldAiMemory updatedMemory = memory.WithStackMemory(
            stack.Id,
            new WorldAiStackMemory(
                WorldStackOrder.Roam.Instance,
                new WorldAiTarget.Stack(new WorldStackId("enemy"))));

        Assert.That(
            memory.GetStackMemory(stack),
            Is.EqualTo(new WorldAiStackMemory(WorldStackOrder.Normal.Instance, null)));
        Assert.That(
            updatedMemory.GetStackMemory(stack),
            Is.EqualTo(new WorldAiStackMemory(
                WorldStackOrder.Roam.Instance,
                new WorldAiTarget.Stack(new WorldStackId("enemy")))));
        Assert.That(
            updatedMemory,
            Is.EqualTo(new WorldAiMemory(
                new WorldAiTurnMemory(2, new WorldPlayerId(3)),
                ImmutableSortedDictionary<WorldStackId, WorldAiStackMemory>.Empty.SetItem(
                    stack.Id,
                    new WorldAiStackMemory(
                        WorldStackOrder.Roam.Instance,
                        new WorldAiTarget.Stack(new WorldStackId("enemy")))))));
    }

    [Test]
    public void TurnTransitionTest()
    {
        WorldStackId stackId = new("stack");
        WorldAiTurnMemory turn = new(2, new WorldPlayerId(3));
        WorldAiMemory memory = new(turn);

        WorldAiTurnMemory updatedTurn = turn
            .WithStackExhausted(stackId)
            .WithAction(new WorldPurchaseUnitCommand(
                new WorldFortificationId("city"),
                new UnitDefinitionId("unit"),
                GridPosition.FrontTop));

        WorldAiMemory updatedMemory = memory with
        {
            Turn = updatedTurn
        };

        Assert.That(memory, Is.EqualTo(new WorldAiMemory(new WorldAiTurnMemory(2, new WorldPlayerId(3)))));
        Assert.That(turn, Is.EqualTo(new WorldAiTurnMemory(2, new WorldPlayerId(3))));
        Assert.That(
            updatedMemory,
            Is.EqualTo(new WorldAiMemory(
                new WorldAiTurnMemory(
                    2,
                    new WorldPlayerId(3),
                    1,
                    1,
                    0,
                    [stackId]))));
    }

    [Test]
    public void ExhaustedStacksNewTurnTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        GameSession session = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession()
            .Session;

        WorldStackId stackId = new("stack");
        WorldAiMemory memory = new(new WorldAiTurnMemory(0, new WorldPlayerId(2)).WithStackExhausted(stackId));

        WorldAiMemory reconciledMemory = memory.ReconcileWith(session.World.State);

        Assert.That(memory, Is.EqualTo(new WorldAiMemory(new WorldAiTurnMemory(0, new WorldPlayerId(2), 0, 0, 0, [stackId]))));
        Assert.That(reconciledMemory, Is.EqualTo(new WorldAiMemory(new WorldAiTurnMemory(1, player.Id))));
    }
}
