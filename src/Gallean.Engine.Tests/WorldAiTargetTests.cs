using Gallean.Common;

namespace Gallean.Engine.Tests;

public sealed class WorldAiTargetTests
{
    private sealed class LabelVisitor : IWorldAiTargetVisitor<Empty, string>
    {
        public string Visit(WorldAiTarget.Stack target, Empty context)
        {
            return $"stack {target.StackId}";
        }

        public string Visit(WorldAiTarget.Fortification target, Empty context)
        {
            return $"fortification {target.FortificationId}";
        }

        public string Visit(WorldAiTarget.Location target, Empty context)
        {
            return $"location ({target.Position.X}, {target.Position.Y})";
        }

        public string Visit(WorldAiTarget.Bag target, Empty context)
        {
            return $"bag {target.BagId}";
        }
    }

    private static string Format(WorldAiTarget target)
    {
        return target.Accept(new LabelVisitor(), Empty.Value);
    }

    [Test]
    public void DispatchTest()
    {
        List<WorldAiTarget> targets =
        [
            new WorldAiTarget.Stack(new WorldStackId("Stack1")),
            new WorldAiTarget.Fortification(new WorldFortificationId("City1")),
            new WorldAiTarget.Location(new WorldPosition(7, 8)),
            new WorldAiTarget.Bag(new WorldBagId("Bag1"))
        ];

        Assert.That(targets.Select(Format), Is.EqualTo([
            "stack Stack1",
            "fortification City1",
            "location (7, 8)",
            "bag Bag1"
        ]));
    }

    [Test]
    public void PositionTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 5);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(1, 2),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1), WorldResourceSet.Empty);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("City1"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        WorldBag bag = new(
            Id: new WorldBagId("Bag1"),
            Position: new WorldPosition(4, 0),
            Variant: new WorldObjectVariant(0),
            Inventory: WorldInventory.Empty,
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithCity(city)
            .WithBag(bag)
            .Build();

        List<WorldAiTarget> targets =
        [
            new WorldAiTarget.Stack(stack.Id),
            new WorldAiTarget.Fortification(city.Id),
            new WorldAiTarget.Location(new WorldPosition(7, 8)),
            new WorldAiTarget.Bag(bag.Id),
            new WorldAiTarget.Stack(new WorldStackId("Gone")),
            new WorldAiTarget.Fortification(new WorldFortificationId("Gone")),
            new WorldAiTarget.Bag(new WorldBagId("Gone"))
        ];

        List<WorldPosition?> positions =
        [
            new WorldPosition(1, 2),
            new WorldPosition(3, 3),
            new WorldPosition(7, 8),
            new WorldPosition(4, 0),
            null,
            null,
            null
        ];

        Assert.That(
            targets.Select(target => target.Accept(WorldAiTargetPositionVisitor.Instance, engine.State)),
            Is.EqualTo(positions));
    }

    [Test]
    public void MovedStackPositionTest()
    {
        WorldMap map = TestHelpers.SimpleMap(5, 3);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.SingleUnitGrid(new WorldUnitId("leader"), leader),
            movementPoints: 6);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        WorldAiTarget target = new WorldAiTarget.Stack(stack.Id);

        _ = engine.DispatchCommand(new WorldStartGameCommand());
        _ = engine.DispatchCommand(new WorldMoveStackCommand(stack.Id, new WorldPosition(2, 1)));

        Assert.That(
            target.Accept(WorldAiTargetPositionVisitor.Instance, engine.State),
            Is.EqualTo(new WorldPosition(2, 1)));
    }
}
