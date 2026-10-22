namespace Gallean.Engine.Tests;

public sealed class WorldMovementCalculatorTests
{
    private static WorldStack BuildStack(params WorldUnit[] units)
    {
        UnitGrid<WorldUnit>.Builder builder = new();

        for (int index = 0; index < units.Length; index++)
        {
            builder.Place(units[index], GridPosition.FromIndex(index), false);
        }

        return TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            new WorldPlayerId(1),
            new WorldPosition(0, 0),
            builder.Build(),
            movementPoints: 20);
    }

    [Test]
    public void GetMovementProfileTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .Build();

        WorldStack stack = BuildStack(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition));

        WorldMovementCalculator.MovementProfile profile = WorldMovementCalculator.GetMovementProfile(stack, definitions);

        Assert.That(profile, Is.EqualTo(new WorldMovementCalculator.MovementProfile(
            MoveAbility: UnitMoveAbility.None,
            IsWaterOnly: false,
            IsLeaderDead: false,
            IsThief: false)));
    }

    [Test]
    public void GetMovementProfileThiefTest()
    {
        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(thief)
            .Build();

        WorldStack stack = BuildStack(TestHelpers.SimpleUnit(new WorldUnitId("thief"), thief));

        WorldMovementCalculator.MovementProfile profile = WorldMovementCalculator.GetMovementProfile(stack, definitions);

        Assert.That(profile, Is.EqualTo(new WorldMovementCalculator.MovementProfile(
            MoveAbility: UnitMoveAbility.None,
            IsWaterOnly: false,
            IsLeaderDead: false,
            IsThief: true)));
    }

    [Test]
    public void WaterMovementInnateTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 6) with
            {
                MoveAbility = UnitMoveAbility.Water
            };

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .Build();

        WorldStack stack = BuildStack(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition));

        WorldMovementCalculator.MovementProfile profile = WorldMovementCalculator.GetMovementProfile(stack, definitions);

        Assert.That(profile, Is.EqualTo(new WorldMovementCalculator.MovementProfile(
            MoveAbility: UnitMoveAbility.Water,
            IsWaterOnly: false,
            IsLeaderDead: false,
            IsThief: false)));
    }

    [Test]
    public void ForestMovementInnateTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 6) with
            {
                MoveAbility = UnitMoveAbility.Forest
            };

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .Build();

        WorldStack stack = BuildStack(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition));

        WorldMovementCalculator.MovementProfile profile = WorldMovementCalculator.GetMovementProfile(stack, definitions);

        Assert.That(profile, Is.EqualTo(new WorldMovementCalculator.MovementProfile(
            MoveAbility: UnitMoveAbility.Forest,
            IsWaterOnly: false,
            IsLeaderDead: false,
            IsThief: false)));
    }

    [Test]
    public void WaterMovementModifierTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        UnitModifierDefinition waterModifier = TestHelpers.SimpleModifier("water-modifier") with
        {
            MoveAbility = UnitMoveAbility.Water
        };

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnitModifier(waterModifier)
            .Build();

        WorldStack stack = BuildStack(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition) with { Modifiers = [UnitModifier.Permanent(waterModifier.Id)] });

        WorldMovementCalculator.MovementProfile profile = WorldMovementCalculator.GetMovementProfile(stack, definitions);

        Assert.That(profile, Is.EqualTo(new WorldMovementCalculator.MovementProfile(
            MoveAbility: UnitMoveAbility.Water,
            IsWaterOnly: false,
            IsLeaderDead: false,
            IsThief: false)));
    }

    [Test]
    public void ForestMovementModifierTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        UnitModifierDefinition forestModifier = TestHelpers.SimpleModifier("forest-modifier") with
        {
            MoveAbility = UnitMoveAbility.Forest
        };

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnitModifier(forestModifier)
            .Build();

        WorldStack stack = BuildStack(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition) with { Modifiers = [UnitModifier.Permanent(forestModifier.Id)] });

        WorldMovementCalculator.MovementProfile profile = WorldMovementCalculator.GetMovementProfile(stack, definitions);

        Assert.That(profile, Is.EqualTo(new WorldMovementCalculator.MovementProfile(
            MoveAbility: UnitMoveAbility.Forest,
            IsWaterOnly: false,
            IsLeaderDead: false,
            IsThief: false)));
    }

    [Test]
    public void WaterOnlyProfileTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 6) with
            {
                IsWaterOnly = true
            };

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .Build();

        WorldStack stack = BuildStack(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition));

        WorldMovementCalculator.MovementProfile profile = WorldMovementCalculator.GetMovementProfile(stack, definitions);

        Assert.That(profile, Is.EqualTo(new WorldMovementCalculator.MovementProfile(
            MoveAbility: UnitMoveAbility.None,
            IsWaterOnly: true,
            IsLeaderDead: false,
            IsThief: false)));
    }

    [Test]
    public void DeadLeaderProfileTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .Build();

        WorldStack stack = BuildStack(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition) with { Hitpoints = 0 });

        WorldMovementCalculator.MovementProfile profile = WorldMovementCalculator.GetMovementProfile(stack, definitions);

        Assert.That(profile, Is.EqualTo(new WorldMovementCalculator.MovementProfile(
            MoveAbility: UnitMoveAbility.None,
            IsWaterOnly: false,
            IsLeaderDead: true,
            IsThief: false)));
    }

    [Test]
    public void NonLeaderMovementTest()
    {
        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 10, initiative: 10, damage: 10) with
        {
            MoveAbility = UnitMoveAbility.Flying
        };

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leaderDefinition)
            .AddUnit(soldierDefinition)
            .Build();

        WorldStack stack = BuildStack(
            TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition),
            TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition));

        WorldMovementCalculator.MovementProfile profile = WorldMovementCalculator.GetMovementProfile(stack, definitions);

        Assert.That(profile, Is.EqualTo(new WorldMovementCalculator.MovementProfile(
            MoveAbility: UnitMoveAbility.None,
            IsWaterOnly: false,
            IsLeaderDead: false,
            IsThief: false)));
    }

    [Test]
    public void GetMovementCostTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".+^~="
        ]);

        WorldMovementCalculator.MovementProfile profile = new(MoveAbility: UnitMoveAbility.None, IsWaterOnly: false, IsLeaderDead: false, IsThief: false);

        int[] costs =
        [
            WorldMovementCalculator.GetMovementCost(map, new WorldPosition(0, 0), profile),
            WorldMovementCalculator.GetMovementCost(map, new WorldPosition(1, 0), profile),
            WorldMovementCalculator.GetMovementCost(map, new WorldPosition(2, 0), profile),
            WorldMovementCalculator.GetMovementCost(map, new WorldPosition(3, 0), profile),
            WorldMovementCalculator.GetMovementCost(map, new WorldPosition(4, 0), profile)
        ];

        Assert.That(costs, Is.EqualTo([
            2,
            4,
            int.MaxValue,
            6,
            1
        ]));
    }

    [Test]
    public void WaterBonusCostTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".+^~="
        ]);

        WorldMovementCalculator.MovementProfile profile = new(MoveAbility: UnitMoveAbility.Water, IsWaterOnly: false, IsLeaderDead: false, IsThief: false);

        int[] costs =
        [
            WorldMovementCalculator.GetMovementCost(map, new WorldPosition(0, 0), profile),
            WorldMovementCalculator.GetMovementCost(map, new WorldPosition(1, 0), profile),
            WorldMovementCalculator.GetMovementCost(map, new WorldPosition(2, 0), profile),
            WorldMovementCalculator.GetMovementCost(map, new WorldPosition(3, 0), profile),
            WorldMovementCalculator.GetMovementCost(map, new WorldPosition(4, 0), profile)
        ];

        Assert.That(costs, Is.EqualTo([
            2,
            4,
            int.MaxValue,
            2,
            1
        ]));
    }

    [Test]
    public void ForestBonusCostTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".+^~="
        ]);

        WorldMovementCalculator.MovementProfile profile = new(MoveAbility: UnitMoveAbility.Forest, IsWaterOnly: false, IsLeaderDead: false, IsThief: false);

        int[] costs =
        [
            WorldMovementCalculator.GetMovementCost(map, new WorldPosition(0, 0), profile),
            WorldMovementCalculator.GetMovementCost(map, new WorldPosition(1, 0), profile),
            WorldMovementCalculator.GetMovementCost(map, new WorldPosition(2, 0), profile),
            WorldMovementCalculator.GetMovementCost(map, new WorldPosition(3, 0), profile),
            WorldMovementCalculator.GetMovementCost(map, new WorldPosition(4, 0), profile)
        ];

        Assert.That(costs, Is.EqualTo([
            2,
            2,
            int.MaxValue,
            6,
            1
        ]));
    }

    [Test]
    public void FlyingCostTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".+^~="
        ]);

        WorldMovementCalculator.MovementProfile profile = new(MoveAbility: UnitMoveAbility.Flying, IsWaterOnly: false, IsLeaderDead: false, IsThief: false);

        int[] costs =
        [
            WorldMovementCalculator.GetMovementCost(map, new WorldPosition(0, 0), profile),
            WorldMovementCalculator.GetMovementCost(map, new WorldPosition(1, 0), profile),
            WorldMovementCalculator.GetMovementCost(map, new WorldPosition(2, 0), profile),
            WorldMovementCalculator.GetMovementCost(map, new WorldPosition(3, 0), profile),
            WorldMovementCalculator.GetMovementCost(map, new WorldPosition(4, 0), profile)
        ];

        Assert.That(costs, Is.EqualTo([
            2,
            2,
            int.MaxValue,
            2,
            2
        ]));
    }

    [Test]
    public void WaterOnlyCostTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "~~~.",
            "~~~~",
            "~~~~"
        ]);

        WorldMovementCalculator.MovementProfile profile = new(MoveAbility: UnitMoveAbility.None, IsWaterOnly: true, IsLeaderDead: false, IsThief: false);

        int[] costs =
        [
            WorldMovementCalculator.GetMovementCost(map, new WorldPosition(1, 1), profile),
            WorldMovementCalculator.GetMovementCost(map, new WorldPosition(2, 1), profile),
            WorldMovementCalculator.GetMovementCost(map, new WorldPosition(3, 0), profile)
        ];

        Assert.That(costs, Is.EqualTo([
            2,
            6,
            int.MaxValue
        ]));
    }

    [Test]
    public void DeadLeaderCostTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".+^~="
        ]);

        WorldMovementCalculator.MovementProfile profile = new(MoveAbility: UnitMoveAbility.None, IsWaterOnly: false, IsLeaderDead: true, IsThief: false);

        Assert.That(WorldMovementCalculator.GetMovementCost(map, new WorldPosition(0, 0), profile), Is.EqualTo(4));
        Assert.That(WorldMovementCalculator.GetMovementCost(map, new WorldPosition(1, 0), profile), Is.EqualTo(8));
        Assert.That(WorldMovementCalculator.GetMovementCost(map, new WorldPosition(2, 0), profile), Is.EqualTo(int.MaxValue));
        Assert.That(WorldMovementCalculator.GetMovementCost(map, new WorldPosition(3, 0), profile), Is.EqualTo(12));
        Assert.That(WorldMovementCalculator.GetMovementCost(map, new WorldPosition(4, 0), profile), Is.EqualTo(2));
    }

    [Test]
    public void IsPassableTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".+^~="
        ]);

        WorldMovementCalculator.MovementProfile profile = new(MoveAbility: UnitMoveAbility.None, IsWaterOnly: false, IsLeaderDead: false, IsThief: false);

        bool[] passability =
        [
            WorldMovementCalculator.IsPassable(map, new WorldPosition(0, 0), profile),
            WorldMovementCalculator.IsPassable(map, new WorldPosition(1, 0), profile),
            WorldMovementCalculator.IsPassable(map, new WorldPosition(2, 0), profile),
            WorldMovementCalculator.IsPassable(map, new WorldPosition(3, 0), profile),
            WorldMovementCalculator.IsPassable(map, new WorldPosition(4, 0), profile)
        ];

        Assert.That(passability, Is.EqualTo([
            true,
            true,
            false,
            true,
            true
        ]));
    }

    [Test]
    public void IsPassableWaterOnlyTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            ".+^~="
        ]);

        WorldMovementCalculator.MovementProfile profile = new(MoveAbility: UnitMoveAbility.None, IsWaterOnly: true, IsLeaderDead: false, IsThief: false);

        bool[] passability =
        [
            WorldMovementCalculator.IsPassable(map, new WorldPosition(0, 0), profile),
            WorldMovementCalculator.IsPassable(map, new WorldPosition(1, 0), profile),
            WorldMovementCalculator.IsPassable(map, new WorldPosition(2, 0), profile),
            WorldMovementCalculator.IsPassable(map, new WorldPosition(3, 0), profile),
            WorldMovementCalculator.IsPassable(map, new WorldPosition(4, 0), profile)
        ];

        Assert.That(passability, Is.EqualTo([
            false,
            false,
            false,
            true,
            false
        ]));
    }
}
