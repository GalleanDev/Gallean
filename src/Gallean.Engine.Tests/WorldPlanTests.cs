using System.Collections.Immutable;

namespace Gallean.Engine.Tests;

public sealed class WorldPlanTests
{
    [Test]
    public void LookupTest()
    {
        WorldMap map = TestHelpers.SimpleMap(12, 10);

        UnitDefinition unitDefinition = TestHelpers.SimpleUnitDefinition("unit", maxHitpoints: 10, initiative: 50, damage: 5);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(4, 5),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("unit"), unitDefinition), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 4);

        WorldCity city = new(
            Id: new WorldFortificationId("city"),
            NameTextId: new ScenarioTextId("Name"),
            DefinitionId: new WorldCityDefinitionId(WorldRace.Undead, 1),
            OwnerId: player.Id,
            SubraceId: TestHelpers.NeutralSubrace.Id,
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(3, 3), FortificationCalculator.CityFootprint),
            Garrison: UnitGrid<WorldUnit>.Empty,
            Inventory: WorldInventory.Empty,
            AiPriority: 3,
            RiotTurns: 0,
            GrowthTurn: 0);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital"),
            player.Id,
            new WorldPosition(9, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldResourceNode resourceNode = new(
            Id: new WorldResourceNodeId("node"),
            OwnerId: player.Id,
            Position: new WorldPosition(5, 6),
            ResourceType: WorldResourceType.Gold,
            AiPriority: 3);

        WorldRuin ruin = new(
            Id: new WorldFortificationId("ruin"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(2, 9), FortificationCalculator.RuinFootprint),
            Variant: new WorldObjectVariant(0),
            Garrison: UnitGrid<WorldUnit>.Empty,
            Resources: new WorldResourceSet(300, 0, 0, 0, 0, 0),
            Item: null,
            LooterId: null,
            AiPriority: 3);

        WorldTrainerSite site = new(
            Id: new WorldSiteId("site"),
            Footprint: FortificationCalculator.CreateFootprint(new WorldPosition(11, 9), FortificationCalculator.SiteFootprint),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            VisitorIds: [],
            AiPriority: 3);

        WorldBag bag = new(
            Id: new WorldBagId("bag"),
            Position: new WorldPosition(7, 6),
            Variant: new WorldObjectVariant(0),
            Inventory: new WorldInventory([new WorldItem(new WorldItemId("BagItem"), new ItemDefinitionId("item"), Charges: null)]),
            AiPriority: 3);

        WorldTomb tomb = new(
            Id: new WorldTombId("tomb"),
            Position: new WorldPosition(8, 6),
            OwnerId: player.Id,
            LeaderDefinitionId: new UnitDefinitionId("leader"),
            LeaderName: null,
            DestroyerPlayerId: null,
            DestroyerLeaderDefinitionId: null,
            DestroyerLeaderName: null,
            Turn: 1);

        WorldLandmark landmark = new(
            Id: new WorldLandmarkId("landmark"),
            DefinitionId: new WorldLandmarkDefinitionId("definition"),
            Footprint: new WorldFootprint(new WorldPosition(0, 5), 2, 2));

        WorldState state = new(
            map,
            new WorldTurnInfo(1, new WorldPlayerId(1)),
            WorldDifficulty.Average,
            ImmutableSortedDictionary<WorldPlayerId, WorldPlayer>.Empty.Add(player.Id, player),
            ImmutableSortedDictionary<WorldRelationId, WorldRelation>.Empty,
            ImmutableSortedDictionary<WorldStackId, WorldStack>.Empty.Add(stack.Id, stack),
            ImmutableSortedDictionary.CreateRange(
            [
                new KeyValuePair<WorldFortificationId, WorldFortification>(capital.Id, capital),
                new KeyValuePair<WorldFortificationId, WorldFortification>(city.Id, city),
                new KeyValuePair<WorldFortificationId, WorldFortification>(ruin.Id, ruin)
            ]),
            ImmutableSortedDictionary<WorldResourceNodeId, WorldResourceNode>.Empty.Add(resourceNode.Id, resourceNode),
            ImmutableSortedDictionary<WorldRodId, WorldRod>.Empty,
            ImmutableSortedDictionary<WorldBagId, WorldBag>.Empty.Add(bag.Id, bag),
            ImmutableSortedDictionary<WorldTombId, WorldTomb>.Empty.Add(tomb.Id, tomb),
            ImmutableSortedDictionary<WorldLandmarkId, WorldLandmark>.Empty.Add(landmark.Id, landmark),
            ImmutableSortedDictionary<WorldMountainId, WorldMountain>.Empty,
            ImmutableSortedDictionary<WorldSiteId, WorldSite>.Empty.Add(site.Id, site),
            ImmutableSortedDictionary<WorldSubraceId, WorldSubrace>.Empty,
            WorldScenarioState.Empty,
            ImmutableSortedDictionary<WorldPlayerId, WorldAiMemory>.Empty);

        WorldPlan plan = WorldPlan.For(state);

        Assert.That(plan.GetStackAt(new WorldPosition(4, 5)), Is.EqualTo(stack));
        Assert.That(plan.GetResourceNodeAt(new WorldPosition(5, 6)), Is.EqualTo(resourceNode));
        Assert.That(plan.GetBagAt(new WorldPosition(7, 6)), Is.EqualTo(bag));
        Assert.That(plan.GetTombsAt(new WorldPosition(8, 6)), Is.EqualTo([tomb]));

        Assert.That(plan.GetFortificationAt(new WorldPosition(3, 3)), Is.EqualTo(city));
        Assert.That(plan.GetFortificationAt(new WorldPosition(0, 0)), Is.EqualTo(city));
        Assert.That(plan.GetFortificationAt(new WorldPosition(9, 4)), Is.EqualTo(capital));
        Assert.That(plan.GetFortificationAt(new WorldPosition(5, 0)), Is.EqualTo(capital));
        Assert.That(plan.GetFortificationAt(new WorldPosition(2, 9)), Is.EqualTo(ruin));
        Assert.That(plan.GetFortificationAt(new WorldPosition(0, 7)), Is.EqualTo(ruin));
        Assert.That(plan.GetSiteAt(new WorldPosition(11, 9)), Is.EqualTo(site));
        Assert.That(plan.GetSiteAt(new WorldPosition(9, 7)), Is.EqualTo(site));
        Assert.That(plan.GetLandmarkAt(new WorldPosition(0, 5)), Is.EqualTo(landmark));
        Assert.That(plan.GetLandmarkAt(new WorldPosition(1, 6)), Is.EqualTo(landmark));

        Assert.That(plan.GetStackAt(new WorldPosition(3, 3)), Is.Null);
        Assert.That(plan.GetFortificationAt(new WorldPosition(4, 5)), Is.Null);
        Assert.That(plan.GetSiteAt(new WorldPosition(4, 5)), Is.Null);
        Assert.That(plan.GetLandmarkAt(new WorldPosition(4, 5)), Is.Null);
        Assert.That(plan.GetResourceNodeAt(new WorldPosition(9, 4)), Is.Null);
        Assert.That(plan.GetBagAt(new WorldPosition(4, 5)), Is.Null);
        Assert.That(plan.GetTombsAt(new WorldPosition(7, 6)), Is.Empty);
    }

    [Test]
    public void CapturedCapitalTest()
    {
        WorldMap map = TestHelpers.SimpleMap(20, 5);

        WorldPlayer conqueror = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer conquered = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldPlayer holdout = TestHelpers.SimplePlayer(new WorldPlayerId(3), WorldRace.Human, map);
        WorldPlayer landless = TestHelpers.SimplePlayer(new WorldPlayerId(4), WorldRace.Dwarf, map);

        WorldCapital conquerorCapital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital1"),
            conqueror.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);
        WorldCapital conqueredCapital = TestHelpers.SimpleCapital(
                new WorldFortificationId("capital2"),
                conquered.Id,
                new WorldPosition(9, 4),
                UnitGrid<WorldUnit>.Empty) with
            {
                CapturedById = conqueror.Id
            };
        WorldCapital holdoutCapturedCapital = TestHelpers.SimpleCapital(
                new WorldFortificationId("capital3"),
                holdout.Id,
                new WorldPosition(14, 4),
                UnitGrid<WorldUnit>.Empty) with
            {
                CapturedById = conqueror.Id
            };
        WorldCapital holdoutCapital = TestHelpers.SimpleCapital(
            new WorldFortificationId("capital4"),
            holdout.Id,
            new WorldPosition(19, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(conqueror.LordId, conqueror.Race, LordCategory.Warrior))
            .WithPlayer(conqueror)
            .WithDefinition(TestHelpers.SimpleLord(conquered.LordId, conquered.Race, LordCategory.Warrior))
            .WithPlayer(conquered)
            .WithDefinition(TestHelpers.SimpleLord(holdout.LordId, holdout.Race, LordCategory.Warrior))
            .WithPlayer(holdout)
            .WithDefinition(TestHelpers.SimpleLord(landless.LordId, landless.Race, LordCategory.Warrior))
            .WithPlayer(landless)
            .WithCapital(conquerorCapital)
            .WithCapital(conqueredCapital)
            .WithCapital(holdoutCapturedCapital)
            .WithCapital(holdoutCapital)
            .Build();

        WorldPlan plan = WorldPlan.For(engine.State);

        Assert.That(plan.GetCapital(conqueror.Id), Is.EqualTo(conquerorCapital));
        Assert.That(plan.GetCapital(conquered.Id), Is.Null);
        Assert.That(plan.GetCapital(holdout.Id), Is.EqualTo(holdoutCapital));
        Assert.That(plan.GetCapital(landless.Id), Is.Null);

        Assert.That(plan.IsPlayerDefeated(conqueror.Id), Is.False);
        Assert.That(plan.IsPlayerDefeated(conquered.Id), Is.True);
        Assert.That(plan.IsPlayerDefeated(holdout.Id), Is.False);
        Assert.That(plan.IsPlayerDefeated(landless.Id), Is.False);
    }

    [Test]
    public void MemoizedPerStateInstanceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldState state = new(
            map,
            new WorldTurnInfo(1, new WorldPlayerId(1)),
            WorldDifficulty.Average,
            ImmutableSortedDictionary<WorldPlayerId, WorldPlayer>.Empty.Add(player.Id, player),
            ImmutableSortedDictionary<WorldRelationId, WorldRelation>.Empty,
            ImmutableSortedDictionary<WorldStackId, WorldStack>.Empty,
            ImmutableSortedDictionary<WorldFortificationId, WorldFortification>.Empty,
            ImmutableSortedDictionary<WorldResourceNodeId, WorldResourceNode>.Empty,
            ImmutableSortedDictionary<WorldRodId, WorldRod>.Empty,
            ImmutableSortedDictionary<WorldBagId, WorldBag>.Empty,
            ImmutableSortedDictionary<WorldTombId, WorldTomb>.Empty,
            ImmutableSortedDictionary<WorldLandmarkId, WorldLandmark>.Empty,
            ImmutableSortedDictionary<WorldMountainId, WorldMountain>.Empty,
            ImmutableSortedDictionary<WorldSiteId, WorldSite>.Empty,
            ImmutableSortedDictionary<WorldSubraceId, WorldSubrace>.Empty,
            WorldScenarioState.Empty,
            ImmutableSortedDictionary<WorldPlayerId, WorldAiMemory>.Empty);

        WorldState equalState = state with { };
        WorldPlan plan = WorldPlan.For(state);

        Assert.That(WorldPlan.For(state), Is.SameAs(plan));
        Assert.That(WorldPlan.For(equalState), Is.Not.SameAs(plan));
    }
}
