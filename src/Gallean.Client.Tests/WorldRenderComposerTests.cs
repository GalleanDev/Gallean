using Gallean.Engine;
using Gallean.Engine.Tests;
using Gallean.Tests.Common;
using static Gallean.Client.WorldRenderState;

namespace Gallean.Client.Tests;

public sealed class WorldRenderComposerTests
{
    private static WorldRenderRequest MakeSceneRequest(WorldState state, bool fogEnabled, WorldCursor cursor)
    {
        return new WorldRenderRequest(
            state,
            state.TurnInfo.CurrentPlayerId,
            Cursor: cursor,
            FogEnabled: fogEnabled,
            Animation: null);
    }

    private static WorldRenderState Compose(
        WorldState state,
        IGameDefinitionProvider definitions,
        bool fogEnabled,
        WorldCursor cursor)
    {
        return new WorldRenderComposer(definitions, GameScenarioProvider.Empty, new TestMetadataProviderBuilder().Build()).Compose(MakeSceneRequest(state, fogEnabled, cursor));
    }

    private static (WorldState State, IGameDefinitionProvider Definitions, WorldStackId StackId) BuildStackWorld(WorldUnitId leaderId)
    {
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 5, leadership: 5);
        UnitDefinition summon = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("summon", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 5, leadership: 1) with
            {
                Category = UnitCategory.Summon
            };

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(leader).AddUnit(summon).Build();

        WorldMap map = TestHelpers.SimpleMap(4, 3);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("s1"),
            human.Id,
            new WorldPosition(1, 1),
            TestHelpers.StackUnitGrid(
                definitions,
                [(TestHelpers.SimpleUnit(leaderId, leader), GridPosition.FrontBottom)]),
            movementPoints: 0);

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .Build()
            .State;

        return (state, definitions, stack.Id);
    }

    [Test]
    public void StackLayerKeysTest()
    {
        (WorldState state, IGameDefinitionProvider definitions, _) = BuildStackWorld(new WorldUnitId("leader"));

        IAssetMetadataProvider metadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.StackUnitShadow(new UnitDefinitionId("leader"), AssetAnimationKey.StackUnitPose.Stop, Direction: 7), new AssetAnimationMetadata(FrameCount: 1, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        WorldRenderState composed = new WorldRenderComposer(definitions, GameScenarioProvider.Empty, metadata).Compose(MakeSceneRequest(state, fogEnabled: true, cursor: WorldCursor.Empty));

        Assert.That(composed.Stacks, Is.EqualTo(new MapObject[]
        {
            new Stack(
                Position: new WorldPosition(1, 1),
                Depth: 1415,
                AssetKey: new AssetAnimationKey.StackUnit(new UnitDefinitionId("leader"), AssetAnimationKey.StackUnitPose.Stop, 7),
                ShadowKey: new AssetAnimationKey.StackUnitShadow(new UnitDefinitionId("leader"), AssetAnimationKey.StackUnitPose.Stop, 7),
                AuraKey: null,
                BannerKey: new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
                Opacity: 1.0)
        }));
    }

    [Test]
    public void WaterOnlyLeaderShadowTest()
    {
        UnitDefinition merfolkLeader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("merfolk", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 5, leadership: 5) with
            {
                IsWaterOnly = true
            };
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(merfolkLeader).Build();

        WorldMap map = TestHelpers.BuildMap(["H~"]);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldStack waterStack = TestHelpers.SimpleStack(
            new WorldStackId("water"),
            human.Id,
            new WorldPosition(1, 0),
            TestHelpers.StackUnitGrid(
                definitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("merfolkLeader"), merfolkLeader), GridPosition.FrontBottom)]),
            movementPoints: 0);

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(merfolkLeader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(waterStack)
            .Build()
            .State;

        IAssetMetadataProvider metadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.StackUnitShadow(merfolkLeader.Id, AssetAnimationKey.StackUnitPose.Stop, Direction: 7), new AssetAnimationMetadata(FrameCount: 1, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        WorldRenderState composed = new WorldRenderComposer(definitions, GameScenarioProvider.Empty, metadata).Compose(MakeSceneRequest(state, fogEnabled: true, cursor: WorldCursor.Empty));

        Assert.That(composed.Stacks, Is.EqualTo(new MapObject[]
        {
            new Stack(
                Position: new WorldPosition(1, 0),
                Depth: 903,
                AssetKey: new AssetAnimationKey.StackUnit(merfolkLeader.Id, AssetAnimationKey.StackUnitPose.Stop, 7),
                ShadowKey: new AssetAnimationKey.StackUnitShadow(merfolkLeader.Id, AssetAnimationKey.StackUnitPose.Stop, Direction: 7),
                AuraKey: null,
                BannerKey: new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
                Opacity: 1.0)
        }));
    }

    [Test]
    public void BoatShadowTest()
    {
        UnitDefinition humanLeader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("humanlord", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 5, leadership: 5);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(humanLeader).Build();

        WorldMap map = TestHelpers.BuildMap(["H~"]);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldStack waterStack = TestHelpers.SimpleStack(
            new WorldStackId("water"),
            human.Id,
            new WorldPosition(1, 0),
            TestHelpers.StackUnitGrid(
                definitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("humanLeader"), humanLeader), GridPosition.FrontBottom)]),
            movementPoints: 0);

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(humanLeader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(waterStack)
            .Build()
            .State;

        IAssetMetadataProvider metadata = new TestMetadataProviderBuilder()
            .WithAnimation(new AssetAnimationKey.StackBoatShadow(WorldRace.Human, AssetAnimationKey.StackUnitPose.Stop, Direction: 7), new AssetAnimationMetadata(FrameCount: 1, Width: 4, Height: 4, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        WorldRenderState composed = new WorldRenderComposer(definitions, GameScenarioProvider.Empty, metadata).Compose(MakeSceneRequest(state, fogEnabled: true, cursor: WorldCursor.Empty));

        Assert.That(composed.Stacks, Is.EqualTo(new MapObject[]
        {
            new Stack(
                Position: new WorldPosition(1, 0),
                Depth: 903,
                AssetKey: new AssetAnimationKey.StackBoat(WorldRace.Human, AssetAnimationKey.StackUnitPose.Stop, 7),
                ShadowKey: new AssetAnimationKey.StackBoatShadow(WorldRace.Human, AssetAnimationKey.StackUnitPose.Stop, Direction: 7),
                AuraKey: null,
                BannerKey: new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
                Opacity: 1.0)
        }));
    }

    [Test]
    public void MapObjectsTest()
    {
        UnitDefinition undeadLeader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("undeadlord", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 5, leadership: 5);
        UnitDefinition humanLeader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("humanlord", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 5, leadership: 5);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(undeadLeader)
            .AddUnit(humanLeader)
            .Build();

        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^^^",
            "^^^^^^^^",
            "^^^^^^^^",
            "^^^^^^^^",
            "^^^^H=7~",
            "^^^^.^.U",
            "^^^^...."
        ]);

        WorldPlayer undead = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar([
                "????????",
                "????????",
                "????????",
                "????????",
                "????...?",
                "????....",
                "??????.."
            ])
        };

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar([
                "????????",
                "????????",
                "????????",
                "????????",
                "????....",
                "????....",
                "????...."
            ])
        };

        WorldStack undeadStack = TestHelpers.SimpleStack(
                new WorldStackId("s1"),
                undead.Id,
                new WorldPosition(4, 4),
                TestHelpers.StackUnitGrid(
                    definitions,
                    [(TestHelpers.SimpleUnit(new WorldUnitId("undeadLeader"), undeadLeader), GridPosition.FrontBottom)]),
                movementPoints: 0) with
            {
                Direction = WorldStackDirection.NorthEast
            };

        WorldStack humanStack = TestHelpers.SimpleStack(
            new WorldStackId("s2"),
            human.Id,
            new WorldPosition(5, 6),
            TestHelpers.StackUnitGrid(
                definitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("humanLeader"), humanLeader), GridPosition.FrontBottom)]),
            movementPoints: 0);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap1"),
            undead.Id,
            new WorldPosition(4, 5),
            UnitGrid<WorldUnit>.Empty);

        WorldCity city = new(
            Id: new WorldFortificationId("c1"),
            NameTextId: new ScenarioTextId("Name"),
            DefinitionId: new WorldCityDefinitionId(WorldRace.Human, 1),
            OwnerId: human.Id,
            SubraceId: TestHelpers.NeutralSubrace.Id,
            Footprint: new WorldFootprint(new WorldPosition(3, 2), 4, 4),
            Garrison: UnitGrid<WorldUnit>.Empty,
            Inventory: WorldInventory.Empty,
            AiPriority: 3,
            RiotTurns: 0,
            GrowthTurn: 0);

        WorldRuin ruin = new(
            Id: new WorldFortificationId("ru1"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: new WorldFootprint(new WorldPosition(5, 4), 3, 3),
            Variant: new WorldObjectVariant(4),
            Garrison: UnitGrid<WorldUnit>.Empty,
            Resources: WorldResourceSet.Empty,
            Item: null,
            LooterId: null,
            AiPriority: 3);

        WorldResourceNode foggedNode = new(
            Id: new WorldResourceNodeId("r1"),
            Position: new WorldPosition(7, 4),
            ResourceType: WorldResourceType.GroveMana,
            AiPriority: 3);

        WorldResourceNode visibleNode = new(
            Id: new WorldResourceNodeId("r2"),
            Position: new WorldPosition(6, 6),
            ResourceType: WorldResourceType.Gold,
            AiPriority: 3);

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(undeadLeader)
            .WithDefinition(humanLeader)
            .WithDefinition(TestHelpers.SimpleLord(undead.LordId, undead.Race, LordCategory.Warrior))
            .WithPlayer(undead)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithStack(undeadStack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(humanStack)
            .WithCapital(capital)
            .WithDefinition(TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty))
            .WithCity(city)
            .WithResourceNode(foggedNode)
            .WithResourceNode(visibleNode)
            .WithRuin(ruin)
            .Build()
            .State;

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: true, cursor: WorldCursor.Empty.SelectStack(new WorldStackId("s1")));

        Assert.That(renderState.MapObjects, Is.EqualTo(new MapObject[]
        {
            new Fortification(
                Position: new WorldPosition(4, 5),
                Depth: 2979,
                Footprint: capital.Footprint,
                AssetKey: new AssetAnimationKey.Capital(WorldRace.Undead, IsCaptured: false)),
            new Fortification(
                Position: new WorldPosition(6, 5),
                Depth: 4509,
                Footprint: city.Footprint,
                AssetKey: new AssetAnimationKey.City(1)),
            new MapTileFeature(
                Position: new WorldPosition(6, 4),
                Depth: 5508,
                AssetKey: new AssetSpriteKey.Tree(WorldMapTerrain.Neutral, 7)),
            new Fortification(
                Position: new WorldPosition(7, 6),
                Depth: 6031,
                Footprint: ruin.Footprint,
                AssetKey: new AssetAnimationKey.Ruin(IsLooted: false, 4)),
            new ResourceNode(
                Position: new WorldPosition(6, 6),
                Depth: 6547,
                AssetKey: new AssetAnimationKey.Crystal(WorldResourceType.Gold)),
            new RuinBanner(
                Position: new WorldPosition(7, 6),
                Depth: 138598,
                Footprint: ruin.Footprint,
                AssetKey: new AssetSpriteKey.RuinBanner(IsLooted: false))
        }));

        Assert.That(renderState.Stacks, Is.EqualTo(new MapObject[]
        {
            new Stack(
                Position: new WorldPosition(4, 4),
                Depth: 4487,
                AssetKey: new AssetAnimationKey.StackUnit(undeadLeader.Id, AssetAnimationKey.StackUnitPose.Stop, 4),
                ShadowKey: null,
                AuraKey: null,
                BannerKey: new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
                Opacity: 1.0)
        }));

        Assert.That(renderState.Cursors, Is.EqualTo(new Cursor[]
        {
        }));
        Assert.That(renderState.SelectedPath, Is.Null);
    }

    [Test]
    public void CapitalBehindFrontTreesTest()
    {
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().Build();

        WorldMap map = TestHelpers.BuildMap([
            ".............",
            ".............",
            ".............",
            ".............",
            ".............",
            ".............",
            ".............",
            ".............",
            ".............",
            ".............",
            ".............",
            ".....+.......",
            ".............",
            ".............",
            ".............",
            "......+.+....",
            "............."
        ]);

        WorldPlayer undead = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap1"),
            undead.Id,
            new WorldPosition(10, 14),
            UnitGrid<WorldUnit>.Empty);

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(undead.LordId, undead.Race, LordCategory.Warrior))
            .WithPlayer(undead)
            .WithCapital(capital)
            .Build()
            .State;

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: false, cursor: WorldCursor.Empty);

        Assert.That(renderState.MapObjects, Is.EqualTo(new MapObject[]
        {
            new MapTileFeature(
                Position: new WorldPosition(5, 11),
                Depth: 8580,
                AssetKey: new AssetSpriteKey.Tree(WorldMapTerrain.Neutral, 0)),
            new Fortification(
                Position: new WorldPosition(10, 14),
                Depth: 10659,
                Footprint: capital.Footprint,
                AssetKey: new AssetAnimationKey.Capital(WorldRace.Undead, IsCaptured: false)),
            new MapTileFeature(
                Position: new WorldPosition(6, 15),
                Depth: 11140,
                AssetKey: new AssetSpriteKey.Tree(WorldMapTerrain.Neutral, 0)),
            new MapTileFeature(
                Position: new WorldPosition(8, 15),
                Depth: 12164,
                AssetKey: new AssetSpriteKey.Tree(WorldMapTerrain.Neutral, 0))
        }));
    }

    [Test]
    public void StackBehindFrontTreesTest()
    {
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 5, leadership: 5);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(leader).Build();

        WorldMap map = TestHelpers.BuildMap([
            ".....",
            ".+...",
            "..+..",
            "...+.",
            "....+"
        ]);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("s1"),
            human.Id,
            new WorldPosition(1, 1),
            TestHelpers.StackUnitGrid(
                definitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom)]),
            movementPoints: 0);

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .Build()
            .State;

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: false, cursor: WorldCursor.Empty);

        Assert.That(renderState.Stacks, Is.EqualTo(new MapObject[]
        {
            new Stack(
                Position: new WorldPosition(1, 1),
                Depth: 1415,
                AssetKey: new AssetAnimationKey.StackUnit(leader.Id, AssetAnimationKey.StackUnitPose.Stop, 7),
                ShadowKey: null,
                AuraKey: null,
                BannerKey: new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
                Opacity: 1.0)
        }));
        Assert.That(renderState.MapObjects, Is.EqualTo(new MapObject[]
        {
            new MapTileFeature(
                Position: new WorldPosition(1, 1),
                Depth: 1412,
                AssetKey: new AssetSpriteKey.Tree(WorldMapTerrain.Neutral, 0)),
            new MapTileFeature(
                Position: new WorldPosition(2, 2),
                Depth: 2436,
                AssetKey: new AssetSpriteKey.Tree(WorldMapTerrain.Neutral, 0)),
            new MapTileFeature(
                Position: new WorldPosition(3, 3),
                Depth: 3460,
                AssetKey: new AssetSpriteKey.Tree(WorldMapTerrain.Neutral, 0)),
            new MapTileFeature(
                Position: new WorldPosition(4, 4),
                Depth: 4484,
                AssetKey: new AssetSpriteKey.Tree(WorldMapTerrain.Neutral, 0))
        }));
    }

    [Test]
    public void LootedRuinTest()
    {
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().Build();

        WorldMap map = TestHelpers.SimpleMap(3, 3);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar([
                "...",
                "...",
                "..."
            ])
        };

        WorldRuin ruin = new(
            Id: new WorldFortificationId("ru1"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: new WorldFootprint(new WorldPosition(0, 0), 3, 3),
            Variant: new WorldObjectVariant(2),
            Garrison: UnitGrid<WorldUnit>.Empty,
            Resources: WorldResourceSet.Empty,
            Item: null,
            LooterId: human.Id,
            AiPriority: 3);

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithRuin(ruin)
            .Build()
            .State;

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: true, cursor: WorldCursor.Empty);

        Assert.That(renderState.MapObjects, Is.EqualTo(new MapObject[]
        {
            new Fortification(
                Position: new WorldPosition(2, 2),
                Depth: 1423,
                Footprint: ruin.Footprint,
                AssetKey: new AssetAnimationKey.Ruin(IsLooted: true, 2)),
            new RuinBanner(
                Position: new WorldPosition(2, 2),
                Depth: 133990,
                Footprint: ruin.Footprint,
                AssetKey: new AssetSpriteKey.RuinBanner(IsLooted: true))
        }));
    }

    [Test]
    public void InvisibleStackTest()
    {
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 5, leadership: 5);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(leader).Build();

        WorldMap map = TestHelpers.SimpleMap(4, 3);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);

        WorldStack ownStack = TestHelpers.SimpleStack(
                new WorldStackId("s1"),
                human.Id,
                new WorldPosition(1, 1),
                TestHelpers.StackUnitGrid(definitions, [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom)]),
                movementPoints: 0) with
            {
                Concealed = true
            };

        WorldStack enemyStack = TestHelpers.SimpleStack(
                new WorldStackId("s2"),
                enemy.Id,
                new WorldPosition(3, 1),
                TestHelpers.StackUnitGrid(definitions, [(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leader), GridPosition.FrontBottom)]),
                movementPoints: 0) with
            {
                Concealed = true
            };

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(ownStack)
            .WithStack(enemyStack)
            .Build()
            .State;

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: false, cursor: WorldCursor.Empty);

        Assert.That(renderState.Stacks, Is.EqualTo(new MapObject[]
        {
            new Stack(
                Position: new WorldPosition(1, 1),
                Depth: 1415,
                AssetKey: new AssetAnimationKey.StackUnit(leader.Id, AssetAnimationKey.StackUnitPose.Stop, 7),
                ShadowKey: null,
                AuraKey: null,
                BannerKey: new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
                Opacity: 0.5)
        }));
        Assert.That(renderState.Minimap.Markers, Is.EqualTo(new MinimapState.Marker[]
        {
            new(new WorldPosition(1, 1), Theme.World.Race.HumanColor, MinimapState.MarkerType.Stack)
        }));
    }

    [Test]
    public void StackOnCityShieldTest()
    {
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 5, leadership: 5);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(leader).Build();

        WorldMap map = TestHelpers.SimpleMap(5, 5);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldCity city = new(
            Id: new WorldFortificationId("c1"),
            NameTextId: new ScenarioTextId("Name"),
            DefinitionId: new WorldCityDefinitionId(WorldRace.Human, 1),
            OwnerId: human.Id,
            SubraceId: TestHelpers.NeutralSubrace.Id,
            Footprint: new WorldFootprint(new WorldPosition(1, 1), 4, 4),
            Garrison: UnitGrid<WorldUnit>.Empty,
            Inventory: WorldInventory.Empty,
            AiPriority: 3,
            RiotTurns: 0,
            GrowthTurn: 0);

        WorldStack stackInCity = TestHelpers.SimpleStack(
            new WorldStackId("s1"),
            human.Id,
            new WorldPosition(4, 4),
            TestHelpers.StackUnitGrid(definitions, [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom)]),
            movementPoints: 0);

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithDefinition(TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty))
            .WithCity(city)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stackInCity)
            .Build()
            .State;

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: false, cursor: WorldCursor.Empty);

        Assert.That(renderState.MapObjects, Has.None.InstanceOf<Stack>());
        Assert.That(renderState.MapObjects, Is.EqualTo(new MapObject[]
        {
            new Fortification(
                Position: new WorldPosition(4, 4),
                Depth: 2973,
                Footprint: city.Footprint,
                AssetKey: new AssetAnimationKey.City(1))
        }));
        Assert.That(renderState.Stacks, Is.EqualTo(new MapObject[]
        {
            new CityShield(
                Position: new WorldPosition(4, 4),
                Depth: 4516,
                Footprint: city.Footprint,
                AssetKey: new AssetSpriteKey.CityShield(WorldRace.Human, IsCapital: false))
        }));
    }

    [Test]
    public void StackOnCapitalShieldTest()
    {
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 5, leadership: 5);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(leader).Build();

        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer undead = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap1"),
            undead.Id,
            new WorldPosition(5, 5),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stackInCapital = TestHelpers.SimpleStack(
            new WorldStackId("s1"),
            undead.Id,
            new WorldPosition(5, 5),
            TestHelpers.StackUnitGrid(definitions, [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom)]),
            movementPoints: 0);

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(undead.LordId, undead.Race, LordCategory.Warrior))
            .WithPlayer(undead)
            .WithCapital(capital)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stackInCapital)
            .Build()
            .State;

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: false, cursor: WorldCursor.Empty);

        Assert.That(renderState.MapObjects, Is.EqualTo(new MapObject[]
        {
            new Fortification(
                Position: new WorldPosition(5, 5),
                Depth: 3491,
                Footprint: capital.Footprint,
                AssetKey: new AssetAnimationKey.Capital(WorldRace.Undead, IsCaptured: false))
        }));
        Assert.That(renderState.Stacks, Is.EqualTo(new MapObject[]
        {
            new CityShield(
                Position: new WorldPosition(5, 5),
                Depth: 5540,
                Footprint: capital.Footprint,
                AssetKey: new AssetSpriteKey.CityShield(WorldRace.Undead, IsCapital: true))
        }));
    }

    [Test]
    public void StackOnCapturedCapitalTest()
    {
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 5, leadership: 5);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(leader).Build();

        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer undead = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer heretic = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldCapital capital = TestHelpers.SimpleCapital(
                new WorldFortificationId("cap1"),
                heretic.Id,
                new WorldPosition(5, 5),
                UnitGrid<WorldUnit>.Empty) with
            {
                CapturedById = undead.Id
            };

        WorldStack stackOnCapital = TestHelpers.SimpleStack(
            new WorldStackId("s1"),
            undead.Id,
            new WorldPosition(5, 5),
            TestHelpers.StackUnitGrid(definitions, [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom)]),
            movementPoints: 0);

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(undead.LordId, undead.Race, LordCategory.Warrior))
            .WithPlayer(undead)
            .WithDefinition(TestHelpers.SimpleLord(heretic.LordId, heretic.Race, LordCategory.Warrior))
            .WithPlayer(heretic)
            .WithCapital(capital)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stackOnCapital)
            .Build()
            .State;

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: false, cursor: WorldCursor.Empty);

        Assert.That(renderState.MapObjects, Is.EqualTo(new MapObject[]
        {
            new Fortification(
                Position: new WorldPosition(5, 5),
                Depth: 3491,
                Footprint: capital.Footprint,
                AssetKey: new AssetAnimationKey.Capital(WorldRace.Heretic, IsCaptured: true))
        }));
        Assert.That(renderState.Stacks, Is.EqualTo(new MapObject[]
        {
            new Stack(
                Position: new WorldPosition(5, 5),
                Depth: 5511,
                AssetKey: new AssetAnimationKey.StackUnit(leader.Id, AssetAnimationKey.StackUnitPose.Stop, 7),
                ShadowKey: null,
                AuraKey: null,
                BannerKey: new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
                Opacity: 1.0)
        }));
    }

    [Test]
    public void RodsTest()
    {
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().Build();

        WorldMap map = TestHelpers.SimpleMap(4, 3);

        WorldPlayer undead = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar([
                "??..",
                "??..",
                "??.."
            ])
        };

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Human, map);

        WorldRod revealedRod = new(
            Id: new WorldRodId("rod1"),
            OwnerId: undead.Id,
            Position: new WorldPosition(3, 1));

        WorldRod foggedRod = new(
            Id: new WorldRodId("rod2"),
            OwnerId: human.Id,
            Position: new WorldPosition(0, 0));

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(undead.LordId, undead.Race, LordCategory.Warrior))
            .WithPlayer(undead)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithRod(revealedRod)
            .WithRod(foggedRod)
            .Build()
            .State;

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: true, cursor: WorldCursor.Empty);

        Assert.That(renderState.MapObjects, Is.EqualTo(new MapObject[]
        {
            new Rod(
                Position: new WorldPosition(3, 1),
                Depth: 2449,
                AssetKey: new AssetSpriteKey.Rod(WorldRace.Undead))
        }));
    }

    [Test]
    public void GarrisonOnlyShieldTest()
    {
        UnitDefinition guard = TestHelpers.SimpleUnitDefinition("guard", maxHitpoints: 30, initiative: 40, damage: 5);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(guard).Build();

        WorldMap map = TestHelpers.SimpleMap(5, 5);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldCity garrisonedCity = new(
            Id: new WorldFortificationId("c1"),
            NameTextId: new ScenarioTextId("Name"),
            DefinitionId: new WorldCityDefinitionId(WorldRace.Human, 1),
            OwnerId: human.Id,
            SubraceId: TestHelpers.NeutralSubrace.Id,
            Footprint: new WorldFootprint(new WorldPosition(1, 1), 4, 4),
            Garrison: TestHelpers.SingleUnitGrid(new WorldUnitId("guard"), guard),
            Inventory: WorldInventory.Empty,
            AiPriority: 3,
            RiotTurns: 0,
            GrowthTurn: 0);

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(guard)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithDefinition(TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty))
            .WithCity(garrisonedCity)
            .Build()
            .State;

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: false, cursor: WorldCursor.Empty);

        Assert.That(renderState.MapObjects, Is.EqualTo(new MapObject[]
        {
            new Fortification(
                Position: new WorldPosition(4, 4),
                Depth: 2973,
                Footprint: garrisonedCity.Footprint,
                AssetKey: new AssetAnimationKey.City(1))
        }));
    }

    [Test]
    public void SitesAndLandmarksTest()
    {
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().Build();

        WorldMap map = TestHelpers.SimpleMap(8, 6);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar([
                "????????",
                "????.???",
                "?..?????",
                "????????",
                "????????",
                "???????."
            ])
        };

        WorldMageSite mageSite = new(
            Id: new WorldSiteId("s1"),
            Footprint: new WorldFootprint(new WorldPosition(0, 0), 3, 3),
            Variant: new WorldObjectVariant(2),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            Spells: [],
            VisitorIds: [],
            AiPriority: 3);

        WorldTrainerSite trainerSite = new(
            Id: new WorldSiteId("s2"),
            Footprint: new WorldFootprint(new WorldPosition(5, 3), 3, 3),
            Variant: new WorldObjectVariant(3),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            VisitorIds: [],
            AiPriority: 3);

        WorldLandmark visibleLandmark = new(
            Id: new WorldLandmarkId("lm1"),
            DefinitionId: new WorldLandmarkDefinitionId("lmark1"),
            Footprint: new WorldFootprint(new WorldPosition(4, 0), 2, 2));

        WorldLandmark foggedLandmark = new(
            Id: new WorldLandmarkId("lm2"),
            DefinitionId: new WorldLandmarkDefinitionId("lmark2"),
            Footprint: new WorldFootprint(new WorldPosition(0, 5), 1, 1));

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithSite(mageSite)
            .WithSite(trainerSite)
            .WithLandmark(visibleLandmark)
            .WithLandmark(foggedLandmark)
            .Build()
            .State;

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: true, cursor: WorldCursor.Empty);

        Assert.That(renderState.MapObjects, Is.EqualTo(new MapObject[]
        {
            new Site(
                Position: new WorldPosition(2, 2),
                Depth: 1422,
                Footprint: mageSite.Footprint,
                AssetKey: new AssetAnimationKey.Site(AssetAnimationKey.SiteType.Mage, 2)),
            new Landmark(
                Position: new WorldPosition(4, 0),
                Depth: 2956,
                Footprint: visibleLandmark.Footprint,
                AssetKey: new AssetAnimationKey.Landmark(new WorldLandmarkDefinitionId("lmark1"))),
            new Site(
                Position: new WorldPosition(7, 5),
                Depth: 5518,
                Footprint: trainerSite.Footprint,
                AssetKey: new AssetAnimationKey.Site(AssetAnimationKey.SiteType.Trainer, 3)),
            new LocationBanner(
                Position: new WorldPosition(2, 2),
                Depth: 133990,
                Footprint: mageSite.Footprint,
                AssetKey: new AssetAnimationKey.SiteBanner(AssetAnimationKey.SiteType.Mage)),
            new LocationBanner(
                Position: new WorldPosition(7, 5),
                Depth: 138086,
                Footprint: trainerSite.Footprint,
                AssetKey: new AssetAnimationKey.SiteBanner(AssetAnimationKey.SiteType.Trainer))
        }));
    }

    [Test]
    public void MountainsTest()
    {
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().Build();

        WorldMap map = TestHelpers.SimpleMap(8, 6);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar([
                "????.???",
                "????????",
                "????????",
                "????????",
                ".???????",
                "???????."
            ])
        };

        WorldMountain visibleMountain = new(
            Id: new WorldMountainId(1),
            Footprint: new WorldFootprint(new WorldPosition(4, 0), 2, 2),
            Race: WorldRace.Neutral,
            Variant: new WorldObjectVariant(3));

        WorldMountain dwarfMountain = new(
            Id: new WorldMountainId(2),
            Footprint: new WorldFootprint(new WorldPosition(7, 5), 1, 1),
            Race: WorldRace.Dwarf,
            Variant: new WorldObjectVariant(0));

        WorldMountain hiddenMountain = new(
            Id: new WorldMountainId(3),
            Footprint: new WorldFootprint(new WorldPosition(0, 3), 1, 1),
            Race: WorldRace.Neutral,
            Variant: new WorldObjectVariant(1));

        WorldMountain clippedMountain = new(
            Id: new WorldMountainId(4),
            Footprint: new WorldFootprint(new WorldPosition(0, 4), 3, 3),
            Race: WorldRace.Neutral,
            Variant: new WorldObjectVariant(5));

        WorldMountain risingMountain = new(
            Id: new WorldMountainId(5),
            Footprint: new WorldFootprint(new WorldPosition(5, 2), 1, 1),
            Race: WorldRace.Neutral,
            Variant: new WorldObjectVariant(4));

        WorldMountain edgeMountain = new(
            Id: new WorldMountainId(6),
            Footprint: new WorldFootprint(new WorldPosition(1, 4), 1, 1),
            Race: WorldRace.Neutral,
            Variant: new WorldObjectVariant(2));

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithMountain(visibleMountain)
            .WithMountain(dwarfMountain)
            .WithMountain(hiddenMountain)
            .WithMountain(clippedMountain)
            .WithMountain(risingMountain)
            .WithMountain(edgeMountain)
            .Build()
            .State;

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: true, cursor: WorldCursor.Empty);

        Assert.That(renderState.MapObjects, Is.EqualTo(new MapObject[]
        {
            new Mountain(
                Position: new WorldPosition(4, 0),
                Depth: 2952,
                Footprint: visibleMountain.Footprint,
                AssetKey: new AssetSpriteKey.Mountain(WorldRace.Neutral, 2, 3)),
            new Mountain(
                Position: new WorldPosition(0, 4),
                Depth: 3464,
                Footprint: clippedMountain.Footprint,
                AssetKey: new AssetSpriteKey.Mountain(WorldRace.Neutral, 3, 5)),
            new Mountain(
                Position: new WorldPosition(5, 2),
                Depth: 3976,
                Footprint: risingMountain.Footprint,
                AssetKey: new AssetSpriteKey.Mountain(WorldRace.Neutral, 1, 4)),
            new Mountain(
                Position: new WorldPosition(7, 5),
                Depth: 6536,
                Footprint: dwarfMountain.Footprint,
                AssetKey: new AssetSpriteKey.Mountain(WorldRace.Dwarf, 1, 0))
        }));
    }

    [Test]
    public void BagTest()
    {
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().Build();

        WorldMap map = TestHelpers.BuildMap([
            "....",
            ".~.."
        ]);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar([
                "????",
                "...."
            ])
        };

        WorldBag landBag = new(
            Id: new WorldBagId("b1"),
            Position: new WorldPosition(0, 1),
            Variant: new WorldObjectVariant(2),
            Inventory: WorldInventory.Empty,
            AiPriority: 3);

        WorldBag waterBag = new(
            Id: new WorldBagId("b2"),
            Position: new WorldPosition(1, 1),
            Variant: new WorldObjectVariant(1),
            Inventory: WorldInventory.Empty,
            AiPriority: 3);

        WorldBag foggedBag = new(
            Id: new WorldBagId("b3"),
            Position: new WorldPosition(0, 0),
            Variant: new WorldObjectVariant(0),
            Inventory: WorldInventory.Empty,
            AiPriority: 3);

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithBag(landBag)
            .WithBag(waterBag)
            .WithBag(foggedBag)
            .Build()
            .State;

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: true, cursor: WorldCursor.Empty);

        Assert.That(renderState.MapObjects, Is.EqualTo(new MapObject[]
        {
            new Bag(
                Position: new WorldPosition(0, 1),
                Depth: 905,
                AssetKey: new AssetSpriteKey.Bag(IsWater: false, 2)),
            new Bag(
                Position: new WorldPosition(1, 1),
                Depth: 1417,
                AssetKey: new AssetSpriteKey.Bag(IsWater: true, 1))
        }));
    }

    [Test]
    public void WaterStackTest()
    {
        UnitDefinition humanLeader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("humanlord", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 5, leadership: 5);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(humanLeader).Build();

        WorldMap map = TestHelpers.BuildMap(["H~"]);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldStack landStack = TestHelpers.SimpleStack(
            new WorldStackId("land"),
            human.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                definitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("humanLeader"), humanLeader), GridPosition.FrontBottom)]),
            movementPoints: 0);

        WorldStack waterStack = TestHelpers.SimpleStack(
            new WorldStackId("water"),
            human.Id,
            new WorldPosition(1, 0),
            TestHelpers.StackUnitGrid(
                definitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("humanLeader2"), humanLeader), GridPosition.FrontBottom)]),
            movementPoints: 0);

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(humanLeader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithStack(landStack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(waterStack)
            .Build()
            .State;

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: true, cursor: WorldCursor.Empty);

        WorldRenderState expected = new(
            Terrain: new MapTerrain(
                map,
                [
                    new MapTile(
                        new WorldPosition(0, 0),
                        new MapTileSurface.Ground(WorldMapTerrain.Human),
                        RoadKey: null),
                    new MapTile(
                        new WorldPosition(1, 0),
                        new MapTileSurface.Water(),
                        RoadKey: null)
                ],
                [WorldMapTerrain.Human],
                [
                    new MapTileBorder.Coast(
                        new WorldPosition(1, 0),
                        new AssetSpriteKey.GroundBorder(
                            AssetSpriteKey.GroundBorderSet.Coast,
                            new AssetSpriteKey.GroundBorderShape.Edges(AssetSpriteKey.GroundBorderEdges.West),
                            19504349),
                        WorldMapTerrain.Human,
                        new AssetSpriteKey.GroundBorder(
                            AssetSpriteKey.GroundBorderSet.Mask,
                            new AssetSpriteKey.GroundBorderShape.Edges(AssetSpriteKey.GroundBorderEdges.West),
                            19504349))
                ]),
            FogTiles: [],
            MapObjects: [],
            Stacks:
            [
                new Stack(
                    Position: new WorldPosition(0, 0),
                    Depth: 391,
                    AssetKey: new AssetAnimationKey.StackUnit(humanLeader.Id, AssetAnimationKey.StackUnitPose.Stop, 7),
                    ShadowKey: null,
                    AuraKey: null,
                    BannerKey: new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
                    Opacity: 1.0),
                new Stack(
                    Position: new WorldPosition(1, 0),
                    Depth: 903,
                    AssetKey: new AssetAnimationKey.StackBoat(WorldRace.Human, AssetAnimationKey.StackUnitPose.Stop, 7),
                    ShadowKey: null,
                    AuraKey: null,
                    BannerKey: new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
                    Opacity: 1.0)
            ],
            Minimap: new MinimapState(
                2,
                1,
                [],
                [
                    new MinimapState.Marker(new WorldPosition(0, 0), Theme.World.Race.HumanColor, MinimapState.MarkerType.Stack),
                    new MinimapState.Marker(new WorldPosition(1, 0), Theme.World.Race.HumanColor, MinimapState.MarkerType.Stack)
                ]),
            Cursors: [],
            Reach: null,
            Highlights: new HighlightOverlay([]),
            SelectedPath: null,
            Animation: null,
            ObjectiveMarkers: [], AmbientSounds: [],
            Clouds: []);

        Assert.That(renderState, Is.EqualTo(expected));
    }

    [Test]
    public void WaterOnlyLeaderStackTest()
    {
        UnitDefinition merfolkLeader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("merfolk", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 5, leadership: 5) with
            {
                IsWaterOnly = true
            };
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(merfolkLeader).Build();

        WorldMap map = TestHelpers.BuildMap(["H~"]);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldStack waterStack = TestHelpers.SimpleStack(
            new WorldStackId("water"),
            human.Id,
            new WorldPosition(1, 0),
            TestHelpers.StackUnitGrid(
                definitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("merfolkLeader"), merfolkLeader), GridPosition.FrontBottom)]),
            movementPoints: 0);

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(merfolkLeader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(waterStack)
            .Build()
            .State;

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: true, cursor: WorldCursor.Empty);

        WorldRenderState expected = new(
            Terrain: new MapTerrain(
                map,
                [
                    new MapTile(
                        new WorldPosition(0, 0),
                        new MapTileSurface.Ground(WorldMapTerrain.Human),
                        RoadKey: null),
                    new MapTile(
                        new WorldPosition(1, 0),
                        new MapTileSurface.Water(),
                        RoadKey: null)
                ],
                [WorldMapTerrain.Human],
                [
                    new MapTileBorder.Coast(
                        new WorldPosition(1, 0),
                        new AssetSpriteKey.GroundBorder(
                            AssetSpriteKey.GroundBorderSet.Coast,
                            new AssetSpriteKey.GroundBorderShape.Edges(AssetSpriteKey.GroundBorderEdges.West),
                            19504349),
                        WorldMapTerrain.Human,
                        new AssetSpriteKey.GroundBorder(
                            AssetSpriteKey.GroundBorderSet.Mask,
                            new AssetSpriteKey.GroundBorderShape.Edges(AssetSpriteKey.GroundBorderEdges.West),
                            19504349))
                ]),
            FogTiles: [],
            MapObjects: [],
            Stacks:
            [
                new Stack(
                    Position: new WorldPosition(1, 0),
                    Depth: 903,
                    AssetKey: new AssetAnimationKey.StackUnit(merfolkLeader.Id, AssetAnimationKey.StackUnitPose.Stop, 7),
                    ShadowKey: null,
                    AuraKey: null,
                    BannerKey: new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
                    Opacity: 1.0)
            ],
            Minimap: new MinimapState(
                2,
                1,
                [],
                [
                    new MinimapState.Marker(new WorldPosition(1, 0), Theme.World.Race.HumanColor, MinimapState.MarkerType.Stack)
                ]),
            Cursors: [],
            Reach: null,
            Highlights: new HighlightOverlay([]),
            SelectedPath: null,
            Animation: null,
            ObjectiveMarkers: [], AmbientSounds: [],
            Clouds: []);

        Assert.That(renderState, Is.EqualTo(expected));
    }

    [Test]
    public void NeutralWaterStackTest()
    {
        UnitDefinition neutralLeader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("wolf", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 5, leadership: 5) with
            {
                Race = WorldRace.Neutral
            };
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(neutralLeader).Build();

        WorldMap map = TestHelpers.BuildMap(["H~"]);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldStack waterStack = TestHelpers.SimpleStack(
            new WorldStackId("water"),
            human.Id,
            new WorldPosition(1, 0),
            TestHelpers.StackUnitGrid(
                definitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("neutralLeader"), neutralLeader), GridPosition.FrontBottom)]),
            movementPoints: 0);

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(neutralLeader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(waterStack)
            .Build()
            .State;

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: true, cursor: WorldCursor.Empty);

        WorldRenderState expected = new(
            Terrain: new MapTerrain(
                map,
                [
                    new MapTile(
                        new WorldPosition(0, 0),
                        new MapTileSurface.Ground(WorldMapTerrain.Human),
                        RoadKey: null),
                    new MapTile(
                        new WorldPosition(1, 0),
                        new MapTileSurface.Water(),
                        RoadKey: null)
                ],
                [WorldMapTerrain.Human],
                [
                    new MapTileBorder.Coast(
                        new WorldPosition(1, 0),
                        new AssetSpriteKey.GroundBorder(
                            AssetSpriteKey.GroundBorderSet.Coast,
                            new AssetSpriteKey.GroundBorderShape.Edges(AssetSpriteKey.GroundBorderEdges.West),
                            19504349),
                        WorldMapTerrain.Human,
                        new AssetSpriteKey.GroundBorder(
                            AssetSpriteKey.GroundBorderSet.Mask,
                            new AssetSpriteKey.GroundBorderShape.Edges(AssetSpriteKey.GroundBorderEdges.West),
                            19504349))
                ]),
            FogTiles: [],
            MapObjects: [],
            Stacks:
            [
                new Stack(
                    Position: new WorldPosition(1, 0),
                    Depth: 903,
                    AssetKey: new AssetAnimationKey.StackBoat(WorldRace.Neutral, AssetAnimationKey.StackUnitPose.Stop, 7),
                    ShadowKey: null,
                    AuraKey: null,
                    BannerKey: new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
                    Opacity: 1.0)
            ],
            Minimap: new MinimapState(
                2,
                1,
                [],
                [
                    new MinimapState.Marker(new WorldPosition(1, 0), Theme.World.Race.HumanColor, MinimapState.MarkerType.Stack)
                ]),
            Cursors: [],
            Reach: null,
            Highlights: new HighlightOverlay([]),
            SelectedPath: null,
            Animation: null,
            ObjectiveMarkers: [], AmbientSounds: [],
            Clouds: []);

        Assert.That(renderState, Is.EqualTo(expected));
    }

    [Test]
    public void WaterStackMoveTest()
    {
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 5, leadership: 5);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(leader).Build();

        WorldMap map = TestHelpers.SimpleMap(2, 1, '~');

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldStack waterStack = TestHelpers.SimpleStack(
            new WorldStackId("water"),
            human.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                definitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom)]),
            movementPoints: 0);

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(waterStack)
            .Build()
            .State;

        WorldRenderRequest request = MakeSceneRequest(state, fogEnabled: true, cursor: WorldCursor.Empty) with
        {
            Animation = new WorldAnimationState.StackMove(
                waterStack.Id,
                [
                    new WorldAnimationState.StackMoveStep(new WorldPosition(0, 0), WorldStackDirection.SouthEast, TimeSpan.Zero),
                    new WorldAnimationState.StackMoveStep(new WorldPosition(1, 0), WorldStackDirection.SouthEast, TimeSpan.FromMilliseconds(200))
                ])
        };

        WorldRenderState renderState = new WorldRenderComposer(definitions, GameScenarioProvider.Empty, new TestMetadataProviderBuilder().Build()).Compose(request);

        Assert.That(renderState.Animation, Is.EqualTo(new StackMove(
            waterStack.Id,
            new AssetSoundKey.StackBoatMove(),
            new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
            [
                new StackMoveStep(
                    new WorldPosition(0, 0),
                    391,
                    new AssetAnimationKey.StackBoat(WorldRace.Human, AssetAnimationKey.StackUnitPose.Move, 6),
                    ShadowKey: null,
                    AuraKey: null,
                    TimeSpan.Zero),
                new StackMoveStep(
                    new WorldPosition(1, 0),
                    903,
                    new AssetAnimationKey.StackBoat(WorldRace.Human, AssetAnimationKey.StackUnitPose.Move, 6),
                    ShadowKey: null,
                    AuraKey: null,
                    TimeSpan.FromMilliseconds(200))
            ],
            Opacity: 1.0,
            IsObjective: false,
            Highlight: null)));
    }

    [Test]
    public void WaterStackMoveToLandTest()
    {
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 5, leadership: 5);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(leader).Build();

        WorldMap map = TestHelpers.BuildMap(["~."]);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldStack waterStack = TestHelpers.SimpleStack(
            new WorldStackId("water"),
            human.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                definitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom)]),
            movementPoints: 0);

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(waterStack)
            .Build()
            .State;

        WorldRenderRequest request = MakeSceneRequest(state, fogEnabled: true, cursor: WorldCursor.Empty) with
        {
            Animation = new WorldAnimationState.StackMove(
                waterStack.Id,
                [
                    new WorldAnimationState.StackMoveStep(new WorldPosition(0, 0), WorldStackDirection.SouthEast, TimeSpan.Zero),
                    new WorldAnimationState.StackMoveStep(new WorldPosition(1, 0), WorldStackDirection.SouthEast, TimeSpan.FromMilliseconds(200))
                ])
        };

        WorldRenderState renderState = new WorldRenderComposer(definitions, GameScenarioProvider.Empty, new TestMetadataProviderBuilder().Build()).Compose(request);

        Assert.That(renderState.Animation, Is.EqualTo(new StackMove(
            waterStack.Id,
            new AssetSoundKey.StackUnitMove(leader.Id, Variant: 1),
            new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
            [
                new StackMoveStep(
                    new WorldPosition(0, 0),
                    391,
                    new AssetAnimationKey.StackBoat(WorldRace.Human, AssetAnimationKey.StackUnitPose.Move, 6),
                    ShadowKey: null,
                    AuraKey: null,
                    TimeSpan.Zero),
                new StackMoveStep(
                    new WorldPosition(1, 0),
                    903,
                    new AssetAnimationKey.StackUnit(leader.Id, AssetAnimationKey.StackUnitPose.Move, 6),
                    ShadowKey: null,
                    AuraKey: null,
                    TimeSpan.FromMilliseconds(200))
            ],
            Opacity: 1.0,
            IsObjective: false,
            Highlight: null)));
    }

    [Test]
    public void StackDestroyTest()
    {
        (WorldState state, IGameDefinitionProvider definitions, WorldStackId stackId) = BuildStackWorld(new WorldUnitId("leader"));

        IAssetMetadataProvider metadata = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.StackDestroyEffect(),
                new AssetAnimationMetadata(FrameCount: 16, Width: 69, Height: 124, OffsetX: -33, OffsetY: -111, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        WorldRenderRequest request = MakeSceneRequest(state with { Stacks = state.Stacks.Remove(stackId) }, fogEnabled: true, cursor: WorldCursor.Empty) with
        {
            Animation = new WorldAnimationState.StackDestroy(new WorldPosition(1, 1), new UnitDefinitionId("leader"))
        };

        WorldRenderState renderState = new WorldRenderComposer(definitions, GameScenarioProvider.Empty, metadata).Compose(request);

        Assert.That(renderState.Stacks, Is.Empty);
        Assert.That(renderState.Animation, Is.EqualTo(new Effect(
            new AssetSoundKey.StackDestroy(),
            new WorldPosition(1, 1),
            132671,
            new AssetAnimationKey.StackDestroyEffect(),
            FrameCount: 16)));

        WorldAnimation animation = Assert.NotNull(renderState.Animation);

        Assert.That(animation.Duration, Is.EqualTo(TimeSpan.FromSeconds(16.0 / 15.0)));
    }

    [Test]
    public void StackSummonDestroyTest()
    {
        (WorldState state, IGameDefinitionProvider definitions, WorldStackId stackId) = BuildStackWorld(new WorldUnitId("leader"));

        IAssetMetadataProvider metadata = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.StackSummonDestroyEffect(),
                new AssetAnimationMetadata(FrameCount: 16, Width: 69, Height: 124, OffsetX: -33, OffsetY: -111, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        WorldRenderRequest request = MakeSceneRequest(state with { Stacks = state.Stacks.Remove(stackId) }, fogEnabled: true, cursor: WorldCursor.Empty) with
        {
            Animation = new WorldAnimationState.StackDestroy(new WorldPosition(1, 1), new UnitDefinitionId("summon"))
        };

        WorldRenderState renderState = new WorldRenderComposer(definitions, GameScenarioProvider.Empty, metadata).Compose(request);

        Assert.That(renderState.Animation, Is.EqualTo(new Effect(
            new AssetSoundKey.StackSummonDestroy(),
            new WorldPosition(1, 1),
            132671,
            new AssetAnimationKey.StackSummonDestroyEffect(),
            FrameCount: 16)));
    }

    [Test]
    public void StackRemoveTest()
    {
        (WorldState state, IGameDefinitionProvider definitions, WorldStackId stackId) = BuildStackWorld(new WorldUnitId("leader"));

        IAssetMetadataProvider metadata = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.StackDestroyEffect(),
                new AssetAnimationMetadata(FrameCount: 16, Width: 69, Height: 124, OffsetX: -33, OffsetY: -111, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        WorldRenderRequest request = MakeSceneRequest(state with { Stacks = state.Stacks.Remove(stackId) }, fogEnabled: true, cursor: WorldCursor.Empty) with
        {
            Animation = new WorldAnimationState.StackRemove(new WorldPosition(1, 1), new UnitDefinitionId("leader"))
        };

        WorldRenderState renderState = new WorldRenderComposer(definitions, GameScenarioProvider.Empty, metadata).Compose(request);

        Assert.That(renderState.Animation, Is.EqualTo(new Effect(
            SoundKey: null,
            new WorldPosition(1, 1),
            132671,
            new AssetAnimationKey.StackDestroyEffect(),
            FrameCount: 16)));
    }

    [Test]
    public void StackCreateTest()
    {
        (WorldState state, IGameDefinitionProvider definitions, _) = BuildStackWorld(new WorldUnitId("leader"));

        IAssetMetadataProvider metadata = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.StackCreateEffect(),
                new AssetAnimationMetadata(FrameCount: 16, Width: 88, Height: 163, OffsetX: -42, OffsetY: -146, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        WorldRenderRequest request = MakeSceneRequest(state, fogEnabled: true, cursor: WorldCursor.Empty) with
        {
            Animation = new WorldAnimationState.StackCreate(new WorldPosition(1, 1))
        };

        WorldRenderState renderState = new WorldRenderComposer(definitions, GameScenarioProvider.Empty, metadata).Compose(request);

        Assert.That(renderState.Animation, Is.EqualTo(new Effect(
            new AssetSoundKey.StackCreate(),
            new WorldPosition(1, 1),
            132672,
            new AssetAnimationKey.StackCreateEffect(),
            FrameCount: 16)));

        WorldAnimation animation = Assert.NotNull(renderState.Animation);

        Assert.That(animation.Duration, Is.EqualTo(TimeSpan.FromSeconds(16.0 / 15.0)));
    }

    [Test]
    public void StackOwnerChangeTest()
    {
        (WorldState state, IGameDefinitionProvider definitions, _) = BuildStackWorld(new WorldUnitId("leader"));

        IAssetMetadataProvider metadata = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.StackOwnerChangeEffect(),
                new AssetAnimationMetadata(FrameCount: 26, Width: 88, Height: 163, OffsetX: -42, OffsetY: -146, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        WorldRenderRequest request = MakeSceneRequest(state, fogEnabled: true, cursor: WorldCursor.Empty) with
        {
            Animation = new WorldAnimationState.StackOwnerChange(new WorldPosition(1, 1))
        };

        WorldRenderState renderState = new WorldRenderComposer(definitions, GameScenarioProvider.Empty, metadata).Compose(request);

        Assert.That(renderState.Animation, Is.EqualTo(new Effect(
            SoundKey: null,
            new WorldPosition(1, 1),
            132673,
            new AssetAnimationKey.StackOwnerChangeEffect(),
            FrameCount: 26)));

        WorldAnimation animation = Assert.NotNull(renderState.Animation);

        Assert.That(animation.Duration, Is.EqualTo(TimeSpan.FromSeconds(26.0 / 15.0)));
    }

    [Test]
    public void StackBattleTest()
    {
        (WorldState state, IGameDefinitionProvider definitions, _) = BuildStackWorld(new WorldUnitId("leader"));

        IAssetMetadataProvider metadata = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.StackBattleEffect(),
                new AssetAnimationMetadata(FrameCount: 17, Width: 86, Height: 67, OffsetX: -43, OffsetY: -34, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        WorldRenderRequest request = MakeSceneRequest(state, fogEnabled: true, cursor: WorldCursor.Empty) with
        {
            Animation = new WorldAnimationState.StackBattle(new WorldPosition(1, 1))
        };

        WorldRenderState renderState = new WorldRenderComposer(definitions, GameScenarioProvider.Empty, metadata).Compose(request);

        Assert.That(renderState.Animation, Is.EqualTo(new StackBattle(
            new WorldPosition(1, 1),
            132910,
            new AssetAnimationKey.StackBattleEffect(),
            FrameCount: 17)));
        Assert.That(renderState.Stacks, Is.EqualTo(new MapObject[]
        {
            new Stack(
                Position: new WorldPosition(1, 1),
                Depth: 1415,
                AssetKey: new AssetAnimationKey.StackUnit(new UnitDefinitionId("leader"), AssetAnimationKey.StackUnitPose.Stop, 7),
                ShadowKey: null,
                AuraKey: null,
                BannerKey: new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
                Opacity: 1.0)
        }));

        WorldAnimation animation = Assert.NotNull(renderState.Animation);

        Assert.That(animation.Duration, Is.EqualTo(TimeSpan.MaxValue));
    }

    [Test]
    public void SummonSpellTest()
    {
        (WorldState state, IGameDefinitionProvider definitions, WorldStackId stackId) = BuildStackWorld(new WorldUnitId("leader"));
        WorldSpellDefinitionId spellId = new("G000SS0008");

        IAssetMetadataProvider metadata = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.WorldSpellEffect(spellId, AssetAnimationKey.WorldSpellEffectLayer.Under, Sublayer: 0),
                new AssetAnimationMetadata(FrameCount: 46, Width: 108, Height: 314, OffsetX: -52, OffsetY: -284, TriggerFrame: 39, SecondTriggerFrame: null))
            .WithAnimation(
                new AssetAnimationKey.WorldSpellEffect(spellId, AssetAnimationKey.WorldSpellEffectLayer.Over, Sublayer: 0),
                new AssetAnimationMetadata(FrameCount: 46, Width: 110, Height: 305, OffsetX: -53, OffsetY: -282, TriggerFrame: 39, SecondTriggerFrame: null))
            .Build();

        WorldRenderRequest request = MakeSceneRequest(state, fogEnabled: true, cursor: WorldCursor.Empty) with
        {
            Animation = new WorldAnimationState.SpellCast(spellId, new WorldPosition(1, 1), stackId)
        };

        WorldRenderState renderState = new WorldRenderComposer(definitions, GameScenarioProvider.Empty, metadata).Compose(request);

        Assert.That(renderState.Stacks, Is.Empty);
        Assert.That(renderState.Animation, Is.EqualTo(new SpellCast(
            new AssetSoundKey.WorldSpellCast(spellId),
            new WorldPosition(1, 1),
            [
                new SpellCastLayer.Effect(new AssetAnimationKey.WorldSpellEffect(spellId, AssetAnimationKey.WorldSpellEffectLayer.Under, Sublayer: 0), 1362, FrameCount: 46),
                new SpellCastLayer.Summon(
                    new Stack(
                        new WorldPosition(1, 1),
                        1415,
                        new AssetAnimationKey.StackUnit(new UnitDefinitionId("leader"), AssetAnimationKey.StackUnitPose.Stop, Direction: 7),
                        ShadowKey: null,
                        AuraKey: null,
                        new AssetSpriteKey.StackBanner(4),
                        Opacity: 1.0),
                    RevealFrame: 39),
                new SpellCastLayer.Effect(new AssetAnimationKey.WorldSpellEffect(spellId, AssetAnimationKey.WorldSpellEffectLayer.Over, Sublayer: 0), 264199, FrameCount: 46)
            ],
            stackId,
            FrameCount: 46)));

        WorldAnimation animation = Assert.NotNull(renderState.Animation);

        Assert.That(animation.Duration, Is.EqualTo(TimeSpan.FromSeconds(46.0 / 15.0)));
    }

    [Test]
    public void SpellEffectLayerDepthsTest()
    {
        (WorldState state, IGameDefinitionProvider definitions, _) = BuildStackWorld(new WorldUnitId("leader"));
        WorldSpellDefinitionId spellId = new("G000SS0008");

        TestMetadataProviderBuilder metadataBuilder = new();

        foreach (AssetAnimationKey.WorldSpellEffectLayer effectLayer in Enum.GetValues<AssetAnimationKey.WorldSpellEffectLayer>())
        {
            for (int sublayer = 0; sublayer < AssetAnimationKey.WorldSpellEffect.SublayerCount; sublayer++)
            {
                metadataBuilder.WithAnimation(
                    new AssetAnimationKey.WorldSpellEffect(spellId, effectLayer, sublayer),
                    new AssetAnimationMetadata(FrameCount: 12, Width: 64, Height: 64, OffsetX: -32, OffsetY: -32, TriggerFrame: null, SecondTriggerFrame: null));
            }
        }

        WorldRenderRequest request = MakeSceneRequest(state, fogEnabled: true, cursor: WorldCursor.Empty) with
        {
            Animation = new WorldAnimationState.SpellCast(spellId, new WorldPosition(1, 1), SummonedStackId: null)
        };

        WorldRenderState renderState = new WorldRenderComposer(definitions, GameScenarioProvider.Empty, metadataBuilder.Build()).Compose(request);
        SpellCast spellCast = Assert.IsOfType<SpellCast>(renderState.Animation);

        Assert.That(spellCast.Layers, Is.EqualTo([
            new SpellCastLayer.Effect(new AssetAnimationKey.WorldSpellEffect(spellId, AssetAnimationKey.WorldSpellEffectLayer.Ground, Sublayer: 0), 1036, FrameCount: 12),
            new SpellCastLayer.Effect(new AssetAnimationKey.WorldSpellEffect(spellId, AssetAnimationKey.WorldSpellEffectLayer.Ground, Sublayer: 1), 1037, FrameCount: 12),
            new SpellCastLayer.Effect(new AssetAnimationKey.WorldSpellEffect(spellId, AssetAnimationKey.WorldSpellEffectLayer.Under, Sublayer: 0), 1362, FrameCount: 12),
            new SpellCastLayer.Effect(new AssetAnimationKey.WorldSpellEffect(spellId, AssetAnimationKey.WorldSpellEffectLayer.Under, Sublayer: 1), 1363, FrameCount: 12),
            new SpellCastLayer.Effect(new AssetAnimationKey.WorldSpellEffect(spellId, AssetAnimationKey.WorldSpellEffectLayer.Front, Sublayer: 0), 1512, FrameCount: 12),
            new SpellCastLayer.Effect(new AssetAnimationKey.WorldSpellEffect(spellId, AssetAnimationKey.WorldSpellEffectLayer.Front, Sublayer: 1), 1513, FrameCount: 12),
            new SpellCastLayer.Effect(new AssetAnimationKey.WorldSpellEffect(spellId, AssetAnimationKey.WorldSpellEffectLayer.Over, Sublayer: 0), 264199, FrameCount: 12),
            new SpellCastLayer.Effect(new AssetAnimationKey.WorldSpellEffect(spellId, AssetAnimationKey.WorldSpellEffectLayer.Over, Sublayer: 1), 264200, FrameCount: 12),
            new SpellCastLayer.Effect(new AssetAnimationKey.WorldSpellEffect(spellId, AssetAnimationKey.WorldSpellEffectLayer.Top, Sublayer: 0), 395778, FrameCount: 12),
            new SpellCastLayer.Effect(new AssetAnimationKey.WorldSpellEffect(spellId, AssetAnimationKey.WorldSpellEffectLayer.Top, Sublayer: 1), 395779, FrameCount: 12)
        ]));
    }

    [Test]
    public void SpellWithoutEffectArtTest()
    {
        (WorldState state, IGameDefinitionProvider definitions, _) = BuildStackWorld(new WorldUnitId("leader"));
        WorldSpellDefinitionId spellId = new("G000SS0002");

        WorldRenderRequest request = MakeSceneRequest(state, fogEnabled: true, cursor: WorldCursor.Empty) with
        {
            Animation = new WorldAnimationState.SpellCast(spellId, new WorldPosition(1, 1), SummonedStackId: null)
        };

        WorldRenderState renderState = new WorldRenderComposer(definitions, GameScenarioProvider.Empty, new TestMetadataProviderBuilder().Build()).Compose(request);

        Assert.That(renderState.Animation, Is.EqualTo(new SpellCast(
            new AssetSoundKey.WorldSpellCast(spellId),
            new WorldPosition(1, 1),
            [],
            SummonedStackId: null,
            FrameCount: 0)));

        WorldAnimation animation = Assert.NotNull(renderState.Animation);

        Assert.That(animation.Duration, Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public void FlyingLeaderStackTest()
    {
        UnitDefinition flyingLeader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("flyer", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 5, leadership: 5) with
            {
                MoveAbility = UnitMoveAbility.Flying
            };
        UnitDefinition footLeader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("foot", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 5, leadership: 5);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(flyingLeader)
            .AddUnit(footLeader)
            .Build();

        WorldMap map = TestHelpers.SimpleMap(2, 1, 'H');

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldStack flyingStack = TestHelpers.SimpleStack(
            new WorldStackId("flying"),
            human.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                definitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("flyingLeader"), flyingLeader), GridPosition.FrontBottom)]),
            movementPoints: 0);

        WorldStack footStack = TestHelpers.SimpleStack(
            new WorldStackId("foot"),
            human.Id,
            new WorldPosition(1, 0),
            TestHelpers.StackUnitGrid(
                definitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("footLeader"), footLeader), GridPosition.FrontBottom)]),
            movementPoints: 0);

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(flyingLeader)
            .WithDefinition(footLeader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(flyingStack)
            .WithStack(footStack)
            .Build()
            .State;

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: true, cursor: WorldCursor.Empty);

        Assert.That(renderState.Stacks, Is.EqualTo(new MapObject[]
        {
            new Stack(
                Position: new WorldPosition(0, 0),
                Depth: 391,
                AssetKey: new AssetAnimationKey.StackUnit(flyingLeader.Id, AssetAnimationKey.StackUnitPose.Move, 7),
                ShadowKey: null,
                AuraKey: null,
                BannerKey: new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
                Opacity: 1.0),
            new Stack(
                Position: new WorldPosition(1, 0),
                Depth: 903,
                AssetKey: new AssetAnimationKey.StackUnit(footLeader.Id, AssetAnimationKey.StackUnitPose.Stop, 7),
                ShadowKey: null,
                AuraKey: null,
                BannerKey: new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
                Opacity: 1.0)
        }));
    }

    [Test]
    public void FlyingLeaderOnWaterStackTest()
    {
        UnitDefinition flyingLeader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("flyer", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 5, leadership: 5) with
            {
                MoveAbility = UnitMoveAbility.Flying
            };
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(flyingLeader).Build();

        WorldMap map = TestHelpers.BuildMap(["H~"]);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldStack flyingStack = TestHelpers.SimpleStack(
            new WorldStackId("flying"),
            human.Id,
            new WorldPosition(1, 0),
            TestHelpers.StackUnitGrid(
                definitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("flyingLeader"), flyingLeader), GridPosition.FrontBottom)]),
            movementPoints: 0);

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(flyingLeader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(flyingStack)
            .Build()
            .State;

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: true, cursor: WorldCursor.Empty);

        Assert.That(renderState.Stacks, Is.EqualTo(new MapObject[]
        {
            new Stack(
                Position: new WorldPosition(1, 0),
                Depth: 903,
                AssetKey: new AssetAnimationKey.StackUnit(flyingLeader.Id, AssetAnimationKey.StackUnitPose.Move, 7),
                ShadowKey: null,
                AuraKey: null,
                BannerKey: new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
                Opacity: 1.0)
        }));
    }

    [Test]
    public void NeutralStackTest()
    {
        UnitDefinition humanLeader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("humanlord", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 5, leadership: 5);
        UnitDefinition neutralLeader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("wolf", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 5, leadership: 5);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(humanLeader)
            .AddUnit(neutralLeader)
            .Build();

        WorldMap map = TestHelpers.BuildMap(["H."]);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer neutral = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Neutral, map);

        WorldStack playerStack = TestHelpers.SimpleStack(
            new WorldStackId("player"),
            human.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                definitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("humanLeader"), humanLeader), GridPosition.FrontBottom)]),
            movementPoints: 0);

        WorldSubrace wolfSubrace = new(
            Id: new WorldSubraceId("wolves"),
            PlayerId: neutral.Id,
            Subrace: WorldSubraceType.NeutralWolf,
            BannerVariant: new WorldObjectVariant(12));

        WorldStack neutralStack = TestHelpers.SimpleStack(
                new WorldStackId("neutral"),
                neutral.Id,
                new WorldPosition(1, 0),
                TestHelpers.StackUnitGrid(
                    definitions,
                    [(TestHelpers.SimpleUnit(new WorldUnitId("neutralLeader"), neutralLeader), GridPosition.FrontBottom)]),
                movementPoints: 0) with
            {
                SubraceId = wolfSubrace.Id
            };

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(humanLeader)
            .WithDefinition(neutralLeader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithDefinition(TestHelpers.SimpleLord(neutral.LordId, neutral.Race, LordCategory.Warrior))
            .WithPlayer(neutral)
            .WithStack(playerStack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(neutralStack)
            .WithSubrace(wolfSubrace)
            .Build()
            .State;

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: true, cursor: WorldCursor.Empty);

        WorldRenderState expected = new(
            Terrain: new MapTerrain(
                map,
                [
                    new MapTile(
                        new WorldPosition(0, 0),
                        new MapTileSurface.Ground(WorldMapTerrain.Human),
                        RoadKey: null),
                    new MapTile(
                        new WorldPosition(1, 0),
                        new MapTileSurface.Ground(WorldMapTerrain.Neutral),
                        RoadKey: null)
                ],
                [WorldMapTerrain.Neutral, WorldMapTerrain.Human],
                [
                    new MapTileBorder.Blend(
                        new WorldPosition(1, 0),
                        new AssetSpriteKey.GroundBorder(
                            AssetSpriteKey.GroundBorderSet.Mask,
                            new AssetSpriteKey.GroundBorderShape.Edges(AssetSpriteKey.GroundBorderEdges.West),
                            19504349),
                        WorldMapTerrain.Human)
                ]),
            FogTiles: [],
            MapObjects: [],
            Stacks:
            [
                new Stack(
                    Position: new WorldPosition(0, 0),
                    Depth: 391,
                    AssetKey: new AssetAnimationKey.StackUnit(humanLeader.Id, AssetAnimationKey.StackUnitPose.Stop, 7),
                    ShadowKey: null,
                    AuraKey: null,
                    BannerKey: new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
                    Opacity: 1.0),
                new Stack(
                    Position: new WorldPosition(1, 0),
                    Depth: 903,
                    AssetKey: new AssetAnimationKey.StackUnit(neutralLeader.Id, AssetAnimationKey.StackUnitPose.Stop, 7),
                    ShadowKey: null,
                    AuraKey: null,
                    BannerKey: new AssetSpriteKey.StackBanner(wolfSubrace.BannerVariant.Value),
                    Opacity: 1.0)
            ],
            Minimap: new MinimapState(
                2,
                1,
                [],
                [
                    new MinimapState.Marker(new WorldPosition(1, 0), Theme.World.Race.NeutralColor, MinimapState.MarkerType.Stack),
                    new MinimapState.Marker(new WorldPosition(0, 0), Theme.World.Race.HumanColor, MinimapState.MarkerType.Stack)
                ]),
            Cursors: [],
            Reach: null,
            Highlights: new HighlightOverlay([]),
            SelectedPath: null,
            Animation: null,
            ObjectiveMarkers: [], AmbientSounds: [],
            Clouds: []);

        Assert.That(renderState, Is.EqualTo(expected));
    }

    [Test]
    public void FoggedObjectsTest()
    {
        UnitDefinition humanLeader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("humanlord", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 5, leadership: 5);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(humanLeader).Build();

        WorldMap map = TestHelpers.BuildMap([
            "^^^^^",
            "^^^^^",
            "^^^^^",
            "^^^HH"
        ]);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar([
                "?????",
                "?????",
                "?????",
                "????."
            ])
        };

        WorldCity foggedCity = new(
            Id: new WorldFortificationId("c1"),
            NameTextId: new ScenarioTextId("Name"),
            DefinitionId: new WorldCityDefinitionId(WorldRace.Human, 1),
            OwnerId: human.Id,
            SubraceId: TestHelpers.NeutralSubrace.Id,
            Footprint: new WorldFootprint(new WorldPosition(0, 0), 4, 4),
            Garrison: UnitGrid<WorldUnit>.Empty,
            Inventory: WorldInventory.Empty,
            AiPriority: 3,
            RiotTurns: 0,
            GrowthTurn: 0);

        WorldStack visibleStack = TestHelpers.SimpleStack(
            new WorldStackId("s1"),
            human.Id,
            new WorldPosition(4, 3),
            TestHelpers.StackUnitGrid(
                definitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("humanLeader"), humanLeader), GridPosition.FrontBottom)]),
            movementPoints: 0);

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(humanLeader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithDefinition(TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty))
            .WithCity(foggedCity)
            .WithStack(visibleStack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .Build()
            .State;

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: true, cursor: WorldCursor.Empty);

        Assert.That(renderState.MapObjects, Is.Empty);
        Assert.That(renderState.Stacks, Is.EqualTo(new MapObject[]
        {
            new Stack(
                Position: new WorldPosition(4, 3),
                Depth: 3975,
                AssetKey: new AssetAnimationKey.StackUnit(humanLeader.Id, AssetAnimationKey.StackUnitPose.Stop, 7),
                ShadowKey: null,
                AuraKey: null,
                BannerKey: new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
                Opacity: 1.0)
        }));

        Assert.That(renderState.Terrain.MapTiles, Is.EqualTo([
            new MapTile(
                new WorldPosition(0, 0),
                new MapTileSurface.Ground(WorldMapTerrain.Neutral),
                RoadKey: null),
            new MapTile(
                new WorldPosition(1, 0),
                new MapTileSurface.Ground(WorldMapTerrain.Neutral),
                RoadKey: null),
            new MapTile(
                new WorldPosition(2, 0),
                new MapTileSurface.Ground(WorldMapTerrain.Neutral),
                RoadKey: null),
            new MapTile(
                new WorldPosition(3, 0),
                new MapTileSurface.Ground(WorldMapTerrain.Neutral),
                RoadKey: null),
            new MapTile(
                new WorldPosition(4, 0),
                new MapTileSurface.Ground(WorldMapTerrain.Neutral),
                RoadKey: null),
            new MapTile(
                new WorldPosition(0, 1),
                new MapTileSurface.Ground(WorldMapTerrain.Neutral),
                RoadKey: null),
            new MapTile(
                new WorldPosition(1, 1),
                new MapTileSurface.Ground(WorldMapTerrain.Neutral),
                RoadKey: null),
            new MapTile(
                new WorldPosition(2, 1),
                new MapTileSurface.Ground(WorldMapTerrain.Neutral),
                RoadKey: null),
            new MapTile(
                new WorldPosition(3, 1),
                new MapTileSurface.Ground(WorldMapTerrain.Neutral),
                RoadKey: null),
            new MapTile(
                new WorldPosition(4, 1),
                new MapTileSurface.Ground(WorldMapTerrain.Neutral),
                RoadKey: null),
            new MapTile(
                new WorldPosition(0, 2),
                new MapTileSurface.Ground(WorldMapTerrain.Neutral),
                RoadKey: null),
            new MapTile(
                new WorldPosition(1, 2),
                new MapTileSurface.Ground(WorldMapTerrain.Neutral),
                RoadKey: null),
            new MapTile(
                new WorldPosition(2, 2),
                new MapTileSurface.Ground(WorldMapTerrain.Neutral),
                RoadKey: null),
            new MapTile(
                new WorldPosition(3, 2),
                new MapTileSurface.Ground(WorldMapTerrain.Neutral),
                RoadKey: null),
            new MapTile(
                new WorldPosition(4, 2),
                new MapTileSurface.Ground(WorldMapTerrain.Neutral),
                RoadKey: null),
            new MapTile(
                new WorldPosition(0, 3),
                new MapTileSurface.Ground(WorldMapTerrain.Neutral),
                RoadKey: null),
            new MapTile(
                new WorldPosition(1, 3),
                new MapTileSurface.Ground(WorldMapTerrain.Neutral),
                RoadKey: null),
            new MapTile(
                new WorldPosition(2, 3),
                new MapTileSurface.Ground(WorldMapTerrain.Neutral),
                RoadKey: null),
            new MapTile(
                new WorldPosition(3, 3),
                new MapTileSurface.Ground(WorldMapTerrain.Human),
                RoadKey: null),
            new MapTile(
                new WorldPosition(4, 3),
                new MapTileSurface.Ground(WorldMapTerrain.Human),
                RoadKey: null)
        ]));

        Assert.That(renderState.FogTiles, Is.EqualTo([
            new FogTile(new WorldPosition(0, 0)),
            new FogTile(new WorldPosition(1, 0)),
            new FogTile(new WorldPosition(2, 0)),
            new FogTile(new WorldPosition(3, 0)),
            new FogTile(new WorldPosition(4, 0)),
            new FogTile(new WorldPosition(0, 1)),
            new FogTile(new WorldPosition(1, 1)),
            new FogTile(new WorldPosition(2, 1)),
            new FogTile(new WorldPosition(3, 1)),
            new FogTile(new WorldPosition(4, 1)),
            new FogTile(new WorldPosition(0, 2)),
            new FogTile(new WorldPosition(1, 2)),
            new FogTile(new WorldPosition(2, 2)),
            new FogTile(new WorldPosition(3, 2)),
            new FogTile(new WorldPosition(4, 2)),
            new FogTile(new WorldPosition(0, 3)),
            new FogTile(new WorldPosition(1, 3)),
            new FogTile(new WorldPosition(2, 3)),
            new FogTile(new WorldPosition(3, 3))
        ]));

        Assert.That(renderState.Cursors, Is.Empty);
        Assert.That(renderState.SelectedPath, Is.Null);
    }

    [Test]
    public void FogTintTest()
    {
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().Build();

        WorldMap map = TestHelpers.SimpleMap(4, 1);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar(["..??"])
        };

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .Build()
            .State;

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: true, cursor: WorldCursor.Empty);

        WorldRenderState expected = new(
            Terrain: new MapTerrain(
                map,
                [
                    new MapTile(
                        new WorldPosition(0, 0),
                        new MapTileSurface.Ground(WorldMapTerrain.Neutral),
                        RoadKey: null),
                    new MapTile(
                        new WorldPosition(1, 0),
                        new MapTileSurface.Ground(WorldMapTerrain.Neutral),
                        RoadKey: null),
                    new MapTile(
                        new WorldPosition(2, 0),
                        new MapTileSurface.Ground(WorldMapTerrain.Neutral),
                        RoadKey: null),
                    new MapTile(
                        new WorldPosition(3, 0),
                        new MapTileSurface.Ground(WorldMapTerrain.Neutral),
                        RoadKey: null)
                ],
                [WorldMapTerrain.Neutral],
                []),
            FogTiles:
            [
                new FogTile(new WorldPosition(2, 0)),
                new FogTile(new WorldPosition(3, 0))
            ],
            MapObjects: [],
            Stacks: [],
            Minimap: new MinimapState(
                4,
                1,
                [
                    new WorldPosition(2, 0),
                    new WorldPosition(3, 0)
                ],
                []),
            Cursors: [],
            Reach: null,
            Highlights: new HighlightOverlay([]),
            SelectedPath: null,
            Animation: null,
            ObjectiveMarkers: [], AmbientSounds: [],
            Clouds: []);

        Assert.That(renderState, Is.EqualTo(expected));
    }

    [Test]
    public void RoadKeyTest()
    {
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().Build();

        WorldMap map = TestHelpers.SimpleMap(2, 1, '=');

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar(["?."])
        };

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .Build()
            .State;

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: true, cursor: WorldCursor.Empty);

        WorldRenderState expected = new(
            Terrain: new MapTerrain(
                map,
                [
                    new MapTile(
                        new WorldPosition(0, 0),
                        new MapTileSurface.Ground(WorldMapTerrain.Neutral),
                        RoadKey: new AssetSpriteKey.Road(AssetSpriteKey.RoadConnections.East)),
                    new MapTile(
                        new WorldPosition(1, 0),
                        new MapTileSurface.Ground(WorldMapTerrain.Neutral),
                        RoadKey: new AssetSpriteKey.Road(AssetSpriteKey.RoadConnections.West))
                ],
                [WorldMapTerrain.Neutral],
                []),
            FogTiles:
            [
                new FogTile(new WorldPosition(0, 0))
            ],
            MapObjects: [],
            Stacks: [],
            Minimap: new MinimapState(
                2,
                1,
                [
                    new WorldPosition(0, 0)
                ],
                []),
            Cursors: [],
            Reach: null,
            Highlights: new HighlightOverlay([]),
            SelectedPath: null,
            Animation: null,
            ObjectiveMarkers: [], AmbientSounds: [],
            Clouds: []);

        Assert.That(renderState, Is.EqualTo(expected));
    }

    [Test]
    public void BordersTest()
    {
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().Build();

        WorldMap map = TestHelpers.BuildMap([
            "H..",
            "...",
            "..~"
        ]);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar([
                "...",
                "...",
                "..."
            ])
        };

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .Build()
            .State;

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: true, cursor: WorldCursor.Empty);

        Assert.That(renderState.Terrain.Borders, Is.EqualTo(new MapTileBorder[]
        {
            new MapTileBorder.Blend(
                new WorldPosition(1, 0),
                new AssetSpriteKey.GroundBorder(
                    AssetSpriteKey.GroundBorderSet.Mask,
                    new AssetSpriteKey.GroundBorderShape.Edges(AssetSpriteKey.GroundBorderEdges.West),
                    19504349),
                WorldMapTerrain.Human),
            new MapTileBorder.Blend(
                new WorldPosition(0, 1),
                new AssetSpriteKey.GroundBorder(
                    AssetSpriteKey.GroundBorderSet.Mask,
                    new AssetSpriteKey.GroundBorderShape.Edges(AssetSpriteKey.GroundBorderEdges.North),
                    187676067),
                WorldMapTerrain.Human),
            new MapTileBorder.Blend(
                new WorldPosition(1, 1),
                new AssetSpriteKey.GroundBorder(
                    AssetSpriteKey.GroundBorderSet.Mask,
                    new AssetSpriteKey.GroundBorderShape.Corners(AssetSpriteKey.GroundBorderCorners.NorthWest),
                    250666057),
                WorldMapTerrain.Human),
            new MapTileBorder.Coast(
                new WorldPosition(2, 2),
                new AssetSpriteKey.GroundBorder(
                    AssetSpriteKey.GroundBorderSet.Coast,
                    new AssetSpriteKey.GroundBorderShape.Edges(AssetSpriteKey.GroundBorderEdges.North | AssetSpriteKey.GroundBorderEdges.West),
                    1904502480),
                WorldMapTerrain.Neutral,
                new AssetSpriteKey.GroundBorder(
                    AssetSpriteKey.GroundBorderSet.Mask,
                    new AssetSpriteKey.GroundBorderShape.Edges(AssetSpriteKey.GroundBorderEdges.North | AssetSpriteKey.GroundBorderEdges.West),
                    1904502480))
        }));
    }

    [Test]
    public void MixedTerrainCoastTest()
    {
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().Build();

        WorldMap map = TestHelpers.BuildMap([
            ".~R"
        ]);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar([
                "..."
            ])
        };

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .Build()
            .State;

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: true, cursor: WorldCursor.Empty);

        Assert.That(renderState.Terrain.Borders, Is.EqualTo(new MapTileBorder[]
        {
            new MapTileBorder.Coast(
                new WorldPosition(1, 0),
                new AssetSpriteKey.GroundBorder(
                    AssetSpriteKey.GroundBorderSet.Coast,
                    new AssetSpriteKey.GroundBorderShape.Edges(AssetSpriteKey.GroundBorderEdges.West),
                    19504349),
                WorldMapTerrain.Neutral,
                new AssetSpriteKey.GroundBorder(
                    AssetSpriteKey.GroundBorderSet.Mask,
                    new AssetSpriteKey.GroundBorderShape.Edges(AssetSpriteKey.GroundBorderEdges.West),
                    19504349)),
            new MapTileBorder.Coast(
                new WorldPosition(1, 0),
                new AssetSpriteKey.GroundBorder(
                    AssetSpriteKey.GroundBorderSet.Coast,
                    new AssetSpriteKey.GroundBorderShape.Edges(AssetSpriteKey.GroundBorderEdges.East),
                    19504349),
                WorldMapTerrain.Heretic,
                new AssetSpriteKey.GroundBorder(
                    AssetSpriteKey.GroundBorderSet.Mask,
                    new AssetSpriteKey.GroundBorderShape.Edges(AssetSpriteKey.GroundBorderEdges.East),
                    19504349))
        }));
    }

    [Test]
    public void FogDisabledTest()
    {
        UnitDefinition humanLeader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("humanlord", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 5, leadership: 5);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(humanLeader).Build();

        WorldMap map = TestHelpers.BuildMap(["H="]);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar(["??"])
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("s1"),
            human.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                definitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("humanLeader"), humanLeader), GridPosition.FrontBottom)]),
            movementPoints: 0);

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(humanLeader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .Build()
            .State;

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: false, cursor: WorldCursor.Empty);

        WorldRenderState expected = new(
            Terrain: new MapTerrain(
                map,
                [
                    new MapTile(
                        new WorldPosition(0, 0),
                        new MapTileSurface.Ground(WorldMapTerrain.Human),
                        RoadKey: null),
                    new MapTile(
                        new WorldPosition(1, 0),
                        new MapTileSurface.Ground(WorldMapTerrain.Neutral),
                        RoadKey: new AssetSpriteKey.Road(AssetSpriteKey.RoadConnections.None))
                ],
                [WorldMapTerrain.Neutral, WorldMapTerrain.Human],
                [
                    new MapTileBorder.Blend(
                        new WorldPosition(1, 0),
                        new AssetSpriteKey.GroundBorder(
                            AssetSpriteKey.GroundBorderSet.Mask,
                            new AssetSpriteKey.GroundBorderShape.Edges(AssetSpriteKey.GroundBorderEdges.West),
                            19504349),
                        WorldMapTerrain.Human)
                ]),
            FogTiles: [],
            MapObjects: [],
            Stacks:
            [
                new Stack(
                    Position: new WorldPosition(0, 0),
                    Depth: 391,
                    AssetKey: new AssetAnimationKey.StackUnit(humanLeader.Id, AssetAnimationKey.StackUnitPose.Stop, 7),
                    ShadowKey: null,
                    AuraKey: null,
                    BannerKey: new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
                    Opacity: 1.0)
            ],
            Minimap: new MinimapState(
                2,
                1,
                [],
                [
                    new MinimapState.Marker(new WorldPosition(0, 0), Theme.World.Race.HumanColor, MinimapState.MarkerType.Stack)
                ]),
            Cursors: [],
            Reach: null,
            Highlights: new HighlightOverlay([]),
            SelectedPath: null,
            Animation: null,
            ObjectiveMarkers: [], AmbientSounds: [],
            Clouds: []);

        Assert.That(renderState, Is.EqualTo(expected));
    }

    [Test]
    public void FogDisabledAndRevealedTest()
    {
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().Build();

        WorldMap map = TestHelpers.SimpleMap(2, 1, 'H');

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar([".?"])
        };

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .Build()
            .State;

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: false, cursor: WorldCursor.Empty);

        WorldRenderState expected = new(
            Terrain: new MapTerrain(
                map,
                [
                    new MapTile(
                        new WorldPosition(0, 0),
                        new MapTileSurface.Ground(WorldMapTerrain.Human),
                        RoadKey: null),
                    new MapTile(
                        new WorldPosition(1, 0),
                        new MapTileSurface.Ground(WorldMapTerrain.Human),
                        RoadKey: null)
                ],
                [WorldMapTerrain.Human],
                []),
            FogTiles: [],
            MapObjects: [],
            Stacks: [],
            Minimap: new MinimapState(
                2,
                1,
                [],
                []),
            Cursors: [],
            Reach: null,
            Highlights: new HighlightOverlay([]),
            SelectedPath: null,
            Animation: null,
            ObjectiveMarkers: [], AmbientSounds: [],
            Clouds: []);

        Assert.That(renderState, Is.EqualTo(expected));
    }

    [Test]
    public void UnchangedStateReusesWorldTest()
    {
        (WorldState state, IGameDefinitionProvider definitions, _) = BuildStackWorld(new WorldUnitId("leader"));

        WorldRenderComposer composer = new(definitions, GameScenarioProvider.Empty, new TestMetadataProviderBuilder().Build());
        WorldRenderState first = composer.Compose(MakeSceneRequest(state, fogEnabled: true, cursor: WorldCursor.Empty));
        WorldRenderState second = composer.Compose(MakeSceneRequest(state, fogEnabled: true, cursor: WorldCursor.Empty));

        Assert.That(second, Is.SameAs(first));
    }

    [Test]
    public void StackMoveReusesTerrainTest()
    {
        (WorldState state, IGameDefinitionProvider definitions, WorldStackId stackId) = BuildStackWorld(new WorldUnitId("leader"));

        WorldStack moved = state.Stacks[stackId] with { Position = new WorldPosition(2, 1) };
        WorldState movedState = state with { Stacks = state.Stacks.SetItem(stackId, moved) };

        WorldRenderComposer composer = new(definitions, GameScenarioProvider.Empty, new TestMetadataProviderBuilder().Build());
        WorldRenderState first = composer.Compose(MakeSceneRequest(state, fogEnabled: true, cursor: WorldCursor.Empty));
        WorldRenderState second = composer.Compose(MakeSceneRequest(movedState, fogEnabled: true, cursor: WorldCursor.Empty));

        Assert.That(second, Is.Not.SameAs(first));
        Assert.That(second.Terrain, Is.SameAs(first.Terrain));
        Assert.That(second.FogTiles, Is.SameAs(first.FogTiles));
        Assert.That(second.MapObjects, Is.SameAs(first.MapObjects));
        Assert.That(second.Stacks, Is.Not.SameAs(first.Stacks));
    }

    [Test]
    public void FogRevealReusesTerrainTest()
    {
        (WorldState state, IGameDefinitionProvider definitions, _) = BuildStackWorld(new WorldUnitId("leader"));

        WorldPlayer player = state.Players[new WorldPlayerId(1)];
        WorldPlayer revealed = player with
        {
            FogOfWar = TestHelpers.BuildFogOfWar([
                "....",
                "...?",
                "...."
            ])
        };
        WorldState revealedState = state with { Players = state.Players.SetItem(player.Id, revealed) };

        WorldRenderComposer composer = new(definitions, GameScenarioProvider.Empty, new TestMetadataProviderBuilder().Build());
        WorldRenderState first = composer.Compose(MakeSceneRequest(state, fogEnabled: true, cursor: WorldCursor.Empty));
        WorldRenderState second = composer.Compose(MakeSceneRequest(revealedState, fogEnabled: true, cursor: WorldCursor.Empty));

        Assert.That(second, Is.Not.SameAs(first));
        Assert.That(second.Terrain, Is.SameAs(first.Terrain));
        Assert.That(second.FogTiles, Is.Not.SameAs(first.FogTiles));
    }

    [Test]
    public void AnimationOnlyReusesTerrainTest()
    {
        (WorldState state, IGameDefinitionProvider definitions, WorldStackId stackId) = BuildStackWorld(new WorldUnitId("leader"));

        WorldRenderComposer composer = new(definitions, GameScenarioProvider.Empty, new TestMetadataProviderBuilder().Build());
        WorldRenderState first = composer.Compose(MakeSceneRequest(state, fogEnabled: true, cursor: WorldCursor.Empty));
        WorldRenderState second = composer.Compose(MakeSceneRequest(state, fogEnabled: true, cursor: WorldCursor.Empty) with
        {
            Animation = new WorldAnimationState.StackMove(
                stackId,
                [
                    new WorldAnimationState.StackMoveStep(new WorldPosition(2, 1), WorldStackDirection.NorthWest, TimeSpan.Zero),
                    new WorldAnimationState.StackMoveStep(new WorldPosition(1, 1), WorldStackDirection.NorthWest, TimeSpan.FromMilliseconds(200))
                ])
        });

        Assert.That(second, Is.Not.SameAs(first));
        Assert.That(second.Terrain, Is.SameAs(first.Terrain));
        Assert.That(second.FogTiles, Is.SameAs(first.FogTiles));
        Assert.That(second.MapObjects, Is.SameAs(first.MapObjects));
        Assert.That(second.Stacks, Is.Not.SameAs(first.Stacks));
        Assert.That(second.Minimap, Is.SameAs(first.Minimap));
    }

    [Test]
    public void SameAnimationTest()
    {
        (WorldState state, IGameDefinitionProvider definitions, WorldStackId stackId) = BuildStackWorld(new WorldUnitId("leader"));

        WorldAnimationState.StackMove animation = new(
            stackId,
            [
                new WorldAnimationState.StackMoveStep(new WorldPosition(2, 1), WorldStackDirection.NorthWest, TimeSpan.Zero),
                new WorldAnimationState.StackMoveStep(new WorldPosition(1, 1), WorldStackDirection.NorthWest, TimeSpan.FromMilliseconds(200))
            ]);

        WorldRenderComposer composer = new(definitions, GameScenarioProvider.Empty, new TestMetadataProviderBuilder().Build());
        WorldRenderState first = composer.Compose(MakeSceneRequest(state, fogEnabled: true, cursor: WorldCursor.Empty) with
        {
            Animation = animation
        });
        WorldRenderState second = composer.Compose(MakeSceneRequest(state, fogEnabled: false, cursor: WorldCursor.Empty) with
        {
            Animation = animation
        });

        Assert.That(first.Animation, Is.Not.Null);
        Assert.That(second, Is.Not.SameAs(first));
        Assert.That(second.Animation, Is.SameAs(first.Animation));
    }

    [Test]
    public void RepeatedAnimationTest()
    {
        (WorldState state, IGameDefinitionProvider definitions, WorldStackId _) = BuildStackWorld(new WorldUnitId("leader"));

        WorldRenderComposer composer = new(definitions, GameScenarioProvider.Empty, new TestMetadataProviderBuilder().Build());
        WorldRenderState first = composer.Compose(MakeSceneRequest(state, fogEnabled: true, cursor: WorldCursor.Empty) with
        {
            Animation = new WorldAnimationState.Pause(TimeSpan.FromSeconds(0.75))
        });
        WorldRenderState second = composer.Compose(MakeSceneRequest(state, fogEnabled: true, cursor: WorldCursor.Empty) with
        {
            Animation = new WorldAnimationState.Pause(TimeSpan.FromSeconds(0.75))
        });

        Assert.That(first.Animation, Is.Not.Null);
        Assert.That(second.Animation, Is.Not.SameAs(first.Animation));
    }

    [Test]
    public void BattleTransitionTest()
    {
        (WorldState state, IGameDefinitionProvider definitions, WorldStackId _) = BuildStackWorld(new WorldUnitId("leader"));

        WorldRenderComposer composer = new(definitions, GameScenarioProvider.Empty, new TestMetadataProviderBuilder().Build());
        WorldRenderState start = composer.Compose(MakeSceneRequest(state, fogEnabled: true, cursor: WorldCursor.Empty) with
        {
            Animation = new WorldAnimationState.BattleStart(new WorldPosition(2, 1))
        });
        WorldRenderState end = composer.Compose(MakeSceneRequest(state, fogEnabled: true, cursor: WorldCursor.Empty) with
        {
            Animation = new WorldAnimationState.BattleEnd(new WorldPosition(2, 1))
        });

        Assert.That(start.Animation, Is.EqualTo(new BattleTransition(new WorldPosition(2, 1), BattleTransitionType.In)));
        Assert.That(end.Animation, Is.EqualTo(new BattleTransition(new WorldPosition(2, 1), BattleTransitionType.Out)));
    }

    [Test]
    public void MinimapTest()
    {
        UnitDefinition humanLeader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("humanlord", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 5, leadership: 5);
        UnitDefinition undeadLeader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("undeadlord", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 5, leadership: 5);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(humanLeader)
            .AddUnit(undeadLeader)
            .Build();

        WorldMap map = TestHelpers.BuildMap([
            "H.U..",
            "..~..",
            ".....",
            "...+.",
            "..^.."
        ]);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar([
                ".?...",
                "..?..",
                ".....",
                ".....",
                "....."
            ])
        };

        WorldPlayer undead = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);

        WorldStack humanStack = TestHelpers.SimpleStack(
            new WorldStackId("s1"),
            human.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("humanLeader"), humanLeader), GridPosition.FrontBottom)]),
            movementPoints: 0);

        WorldStack undeadStack = TestHelpers.SimpleStack(
            new WorldStackId("s2"),
            undead.Id,
            new WorldPosition(1, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("undeadLeader"), undeadLeader), GridPosition.FrontBottom)]),
            movementPoints: 0);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("cap1"),
            undead.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldCity city = new(
            Id: new WorldFortificationId("c1"),
            NameTextId: new ScenarioTextId("Name"),
            DefinitionId: new WorldCityDefinitionId(WorldRace.Human, 1),
            OwnerId: human.Id,
            SubraceId: TestHelpers.NeutralSubrace.Id,
            Footprint: new WorldFootprint(new WorldPosition(1, 0), 4, 4),
            Garrison: UnitGrid<WorldUnit>.Empty,
            Inventory: WorldInventory.Empty,
            AiPriority: 3,
            RiotTurns: 0,
            GrowthTurn: 0);

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(humanLeader)
            .WithDefinition(undeadLeader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithDefinition(TestHelpers.SimpleLord(undead.LordId, undead.Race, LordCategory.Warrior))
            .WithPlayer(undead)
            .WithStack(humanStack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(undeadStack)
            .WithCapital(capital)
            .WithDefinition(TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty))
            .WithCity(city)
            .Build()
            .State;

        WorldRenderState renderState = Compose(state, stackDefinitions, fogEnabled: true, cursor: WorldCursor.Empty);

        MinimapState expected = new(
            5,
            5,
            [
                new WorldPosition(1, 0),
                new WorldPosition(2, 1)
            ],
            [
                new MinimapState.Marker(new WorldPosition(4, 3), Theme.World.Race.HumanColor, MinimapState.MarkerType.City),
                new MinimapState.Marker(new WorldPosition(4, 4), Theme.World.Race.UndeadColor, MinimapState.MarkerType.Capital),
                new MinimapState.Marker(new WorldPosition(0, 0), Theme.World.Race.HumanColor, MinimapState.MarkerType.Stack)
            ]);

        Assert.That(renderState.Minimap, Is.EqualTo(expected));
    }

    [Test]
    public void MinimapFogDisabledTest()
    {
        UnitDefinition humanLeader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("humanlord", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 5, leadership: 5);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(humanLeader)
            .Build();

        WorldMap map = TestHelpers.BuildMap(["H~"]);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar(["??"])
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("s1"),
            human.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("humanLeader"), humanLeader), GridPosition.FrontBottom)]),
            movementPoints: 0);

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(humanLeader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .Build()
            .State;

        WorldRenderState renderState = Compose(state, stackDefinitions, fogEnabled: false, cursor: WorldCursor.Empty);

        MinimapState expected = new(
            2,
            1,
            [],
            [
                new MinimapState.Marker(new WorldPosition(0, 0), Theme.World.Race.HumanColor, MinimapState.MarkerType.Stack)
            ]);

        Assert.That(renderState.Minimap, Is.EqualTo(expected));
    }

    [Test]
    public void MountainRemoveTest()
    {
        (WorldState state, IGameDefinitionProvider definitions, _) = BuildStackWorld(new WorldUnitId("leader"));

        IAssetMetadataProvider metadata = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.MountainRemoveEffect(5),
                new AssetAnimationMetadata(FrameCount: 16, Width: 342, Height: 321, OffsetX: -165, OffsetY: -242, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        WorldRenderRequest request = MakeSceneRequest(state, fogEnabled: true, cursor: WorldCursor.Empty) with
        {
            Animation = new WorldAnimationState.MountainRemove(new WorldPosition(1, 1), Radius: 2)
        };

        WorldRenderState renderState = new WorldRenderComposer(definitions, GameScenarioProvider.Empty, metadata).Compose(request);

        Assert.That(renderState.Animation, Is.EqualTo(new Effect(
            new AssetSoundKey.Occupy(),
            new WorldPosition(1, 1),
            132690,
            new AssetAnimationKey.MountainRemoveEffect(5),
            FrameCount: 16)));

        WorldAnimation animation = Assert.NotNull(renderState.Animation);

        Assert.That(animation.Duration, Is.EqualTo(TimeSpan.FromSeconds(16.0 / 15.0)));
    }

    [Test]
    public void LandmarkWaterKeysTest()
    {
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().Build();

        WorldMap map = TestHelpers.BuildMap([
            "~~~~....",
            "~~~~....",
            "~~......",
            "........"
        ]);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldLandmark openSea = new(
            Id: new WorldLandmarkId("lm1"),
            DefinitionId: new WorldLandmarkDefinitionId("lmark1"),
            Footprint: new WorldFootprint(new WorldPosition(0, 0), 2, 2));

        WorldLandmark shore = new(
            Id: new WorldLandmarkId("lm2"),
            DefinitionId: new WorldLandmarkDefinitionId("lmark2"),
            Footprint: new WorldFootprint(new WorldPosition(1, 2), 2, 2));

        WorldLandmark inland = new(
            Id: new WorldLandmarkId("lm3"),
            DefinitionId: new WorldLandmarkDefinitionId("lmark3"),
            Footprint: new WorldFootprint(new WorldPosition(5, 0), 2, 2));

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithLandmark(openSea)
            .WithLandmark(shore)
            .WithLandmark(inland)
            .Build()
            .State;

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: false, cursor: WorldCursor.Empty);

        Assert.That(renderState.MapObjects, Is.EqualTo(new MapObject[]
        {
            new Landmark(
                Position: new WorldPosition(0, 0),
                Depth: 908,
                Footprint: openSea.Footprint,
                AssetKey: new AssetAnimationKey.WaterLandmark(new WorldLandmarkDefinitionId("lmark1"))),
            new Landmark(
                Position: new WorldPosition(1, 2),
                Depth: 2444,
                Footprint: shore.Footprint,
                AssetKey: new AssetAnimationKey.Landmark(new WorldLandmarkDefinitionId("lmark2"))),
            new Landmark(
                Position: new WorldPosition(5, 0),
                Depth: 3468,
                Footprint: inland.Footprint,
                AssetKey: new AssetAnimationKey.Landmark(new WorldLandmarkDefinitionId("lmark3")))
        }));
    }

    [Test]
    public void ObjectShadowsTest()
    {
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().Build();

        WorldMap map = TestHelpers.SimpleMap(10, 10);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldCity shadowedCity = new(
            Id: new WorldFortificationId("c1"),
            NameTextId: new ScenarioTextId("Name"),
            DefinitionId: new WorldCityDefinitionId(WorldRace.Human, 2),
            OwnerId: human.Id,
            SubraceId: TestHelpers.NeutralSubrace.Id,
            Footprint: new WorldFootprint(new WorldPosition(0, 0), 4, 4),
            Garrison: UnitGrid<WorldUnit>.Empty,
            Inventory: WorldInventory.Empty,
            AiPriority: 3,
            RiotTurns: 0,
            GrowthTurn: 0);

        WorldCity plainCity = new(
            Id: new WorldFortificationId("c2"),
            NameTextId: new ScenarioTextId("Name"),
            DefinitionId: new WorldCityDefinitionId(WorldRace.Human, 3),
            OwnerId: human.Id,
            SubraceId: TestHelpers.NeutralSubrace.Id,
            Footprint: new WorldFootprint(new WorldPosition(5, 0), 4, 4),
            Garrison: UnitGrid<WorldUnit>.Empty,
            Inventory: WorldInventory.Empty,
            AiPriority: 3,
            RiotTurns: 0,
            GrowthTurn: 0);

        WorldMerchantSite shadowedSite = new(
            Id: new WorldSiteId("s1"),
            Footprint: new WorldFootprint(new WorldPosition(1, 6), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            Items: [],
            VisitorIds: [],
            AiPriority: 3);

        WorldMerchantSite plainSite = new(
            Id: new WorldSiteId("s2"),
            Footprint: new WorldFootprint(new WorldPosition(6, 6), 3, 3),
            Variant: new WorldObjectVariant(1),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            Items: [],
            VisitorIds: [],
            AiPriority: 3);

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithDefinition(TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 2), WorldResourceSet.Empty))
            .WithDefinition(TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 3), WorldResourceSet.Empty))
            .WithCity(shadowedCity)
            .WithCity(plainCity)
            .WithSite(shadowedSite)
            .WithSite(plainSite)
            .Build()
            .State;

        IAssetMetadataProvider metadata = new TestMetadataProviderBuilder()
            .WithAnimation(
                new AssetAnimationKey.CityShadow(2),
                new AssetAnimationMetadata(FrameCount: 20, Width: 56, Height: 47, OffsetX: -142, OffsetY: -37, TriggerFrame: null, SecondTriggerFrame: null))
            .WithAnimation(
                new AssetAnimationKey.SiteShadow(AssetAnimationKey.SiteType.Merchant, 0),
                new AssetAnimationMetadata(FrameCount: 20, Width: 114, Height: 60, OffsetX: -80, OffsetY: -27, TriggerFrame: null, SecondTriggerFrame: null))
            .Build();

        WorldRenderState renderState = new WorldRenderComposer(definitions, GameScenarioProvider.Empty, metadata)
            .Compose(MakeSceneRequest(state, fogEnabled: false, cursor: WorldCursor.Empty));

        Assert.That(renderState.MapObjects, Is.EqualTo(new MapObject[]
        {
            new ObjectShadow(
                Position: new WorldPosition(3, 3),
                Depth: 1546,
                Footprint: shadowedCity.Footprint,
                AssetKey: new AssetAnimationKey.CityShadow(2)),
            new Fortification(
                Position: new WorldPosition(3, 3),
                Depth: 1949,
                Footprint: shadowedCity.Footprint,
                AssetKey: new AssetAnimationKey.City(2)),
            new Fortification(
                Position: new WorldPosition(8, 3),
                Depth: 4509,
                Footprint: plainCity.Footprint,
                AssetKey: new AssetAnimationKey.City(3)),
            new ObjectShadow(
                Position: new WorldPosition(3, 8),
                Depth: 4618,
                Footprint: shadowedSite.Footprint,
                AssetKey: new AssetAnimationKey.SiteShadow(AssetAnimationKey.SiteType.Merchant, 0)),
            new Site(
                Position: new WorldPosition(3, 8),
                Depth: 5006,
                Footprint: shadowedSite.Footprint,
                AssetKey: new AssetAnimationKey.Site(AssetAnimationKey.SiteType.Merchant, 0)),
            new Site(
                Position: new WorldPosition(8, 8),
                Depth: 7566,
                Footprint: plainSite.Footprint,
                AssetKey: new AssetAnimationKey.Site(AssetAnimationKey.SiteType.Merchant, 1)),
            new LocationBanner(
                Position: new WorldPosition(3, 8),
                Depth: 137574,
                Footprint: shadowedSite.Footprint,
                AssetKey: new AssetAnimationKey.SiteBanner(AssetAnimationKey.SiteType.Merchant)),
            new LocationBanner(
                Position: new WorldPosition(8, 8),
                Depth: 140134,
                Footprint: plainSite.Footprint,
                AssetKey: new AssetAnimationKey.SiteBanner(AssetAnimationKey.SiteType.Merchant))
        }));
    }

    [Test]
    public void BlendOrderTest()
    {
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().Build();

        WorldMap map = TestHelpers.BuildMap([
            ".RUHED"
        ]);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar([
                "......"
            ])
        };

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .Build()
            .State;

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: true, cursor: WorldCursor.Empty);

        Assert.That(renderState.Terrain.Borders, Is.EqualTo(new MapTileBorder[]
        {
            new MapTileBorder.Blend(
                new WorldPosition(0, 0),
                new AssetSpriteKey.GroundBorder(
                    AssetSpriteKey.GroundBorderSet.Mask,
                    new AssetSpriteKey.GroundBorderShape.Edges(AssetSpriteKey.GroundBorderEdges.East),
                    2017529470),
                WorldMapTerrain.Heretic),
            new MapTileBorder.Blend(
                new WorldPosition(1, 0),
                new AssetSpriteKey.GroundBorder(
                    AssetSpriteKey.GroundBorderSet.Mask,
                    new AssetSpriteKey.GroundBorderShape.Edges(AssetSpriteKey.GroundBorderEdges.East),
                    19504349),
                WorldMapTerrain.Undead),
            new MapTileBorder.Blend(
                new WorldPosition(2, 0),
                new AssetSpriteKey.GroundBorder(
                    AssetSpriteKey.GroundBorderSet.Mask,
                    new AssetSpriteKey.GroundBorderShape.Edges(AssetSpriteKey.GroundBorderEdges.East),
                    709674654),
                WorldMapTerrain.Human),
            new MapTileBorder.Blend(
                new WorldPosition(3, 0),
                new AssetSpriteKey.GroundBorder(
                    AssetSpriteKey.GroundBorderSet.Mask,
                    new AssetSpriteKey.GroundBorderShape.Edges(AssetSpriteKey.GroundBorderEdges.East),
                    2040537341),
                WorldMapTerrain.Elf),
            new MapTileBorder.Blend(
                new WorldPosition(4, 0),
                new AssetSpriteKey.GroundBorder(
                    AssetSpriteKey.GroundBorderSet.Mask,
                    new AssetSpriteKey.GroundBorderShape.Edges(AssetSpriteKey.GroundBorderEdges.East),
                    81175858),
                WorldMapTerrain.Dwarf)
        }));
    }

    [Test]
    public void PathDestinationCursorTest()
    {
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 5, leadership: 5);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(leader).Build();

        WorldMap map = TestHelpers.BuildMap(["..^"]);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("s1"),
            human.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                definitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom)]),
            movementPoints: 5);

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .Build()
            .State;

        WorldPath path = new([
            new WorldPathSegment(new WorldPosition(0, 0), new WorldPosition(1, 0), Cost: 2, Turn: 0)
        ]);

        WorldCursor cursor = WorldCursor.Empty.SelectStack(stack.Id) with
        {
            ReachablePositions = [new WorldPosition(1, 0)],
            HoverTile = new WorldPosition(1, 0),
            HoverTarget = new WorldPosition(1, 0),
            HoverPath = new WorldPathSelection(new WorldPosition(1, 0), path, new WorldPath([]), Encounter: null)
        };

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: false, cursor: cursor);

        Assert.That(renderState.Cursors, Is.EqualTo(new Cursor[]
        {
            new(new WorldPosition(1, 0), 832, Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, 1.0)
        }));
    }

    [Test]
    public void LargeLeaderCursorTest()
    {
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 5, leadership: 5) with
            {
                IsLarge = true
            };
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(leader).Build();

        WorldMap map = TestHelpers.BuildMap(["..."]);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldStack selected = TestHelpers.SimpleStack(
            new WorldStackId("s1"),
            human.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                definitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom)]),
            movementPoints: 5);

        WorldStack hovered = TestHelpers.SimpleStack(
            new WorldStackId("s2"),
            human.Id,
            new WorldPosition(2, 0),
            TestHelpers.StackUnitGrid(
                definitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("leader2"), leader), GridPosition.FrontBottom)]),
            movementPoints: 5);

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(selected)
            .WithStack(hovered)
            .Build()
            .State;

        WorldPath path = new([
            new WorldPathSegment(new WorldPosition(0, 0), new WorldPosition(1, 0), Cost: 2, Turn: 0)
        ]);

        WorldCursor cursor = WorldCursor.Empty.SelectStack(selected.Id) with
        {
            ReachablePositions = [new WorldPosition(1, 0), new WorldPosition(2, 0)],
            HoverObject = new WorldPosition(2, 0),
            HoverTile = new WorldPosition(1, 0),
            HoverTarget = new WorldPosition(1, 0),
            HoverPath = new WorldPathSelection(new WorldPosition(1, 0), path, new WorldPath([]), Encounter: null)
        };

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: false, cursor: cursor);

        Assert.That(renderState.Cursors, Is.EqualTo(new Cursor[]
        {
            new(new WorldPosition(1, 0), 832, Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, 1.0),
            new(new WorldPosition(2, 0), 1054, Theme.World.HoverCursorInnerColor, Theme.World.HoverCursorOuterColor, 1.3)
        }));
    }

    [Test]
    public void ImpassableHoverCursorTest()
    {
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 5, leadership: 5);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(leader).Build();

        WorldMap map = TestHelpers.BuildMap(["..^"]);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("s1"),
            human.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                definitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom)]),
            movementPoints: 5);

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .Build()
            .State;

        WorldCursor cursor = WorldCursor.Empty.SelectStack(stack.Id) with { HoverTile = new WorldPosition(2, 0), HoverTarget = new WorldPosition(2, 0) };

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: false, cursor: cursor);

        Assert.That(renderState.Cursors, Is.EqualTo(new Cursor[]
        {
        }));
    }

    [Test]
    public void FoggedHoverCursorTest()
    {
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 5, leadership: 5);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(leader).Build();

        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar(["..?"])
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("s1"),
            human.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                definitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom)]),
            movementPoints: 5);

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .Build()
            .State;

        WorldCursor cursor = WorldCursor.Empty.SelectStack(stack.Id) with { HoverTile = new WorldPosition(2, 0), HoverTarget = new WorldPosition(2, 0) };

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: true, cursor: cursor);

        Assert.That(renderState.Cursors, Is.EqualTo(new Cursor[]
        {
        }));
    }

    [Test]
    public void FoggedTargetCursorTest()
    {
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 5, leadership: 5);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(leader).Build();

        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar(["..?"])
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("s1"),
            human.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                definitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom)]),
            movementPoints: 5);

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .Build()
            .State;

        WorldCursor cursor = WorldCursor.Empty.SelectStack(stack.Id) with
        {
            Action = new WorldCursorAction.CastSpell(new WorldSpellDefinitionId("sp1")),
            HoverTile = new WorldPosition(2, 0)
        };

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: true, cursor: cursor);

        Assert.That(renderState.Cursors, Is.EqualTo(new Cursor[]
        {
        }));
    }

    [Test]
    public void ObjectiveMarkersTest()
    {
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 5, leadership: 5);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(leader).Build();

        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Human, 1),
            WorldResourceSet.Empty);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            human.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("s1"),
            human.Id,
            new WorldPosition(6, 6),
            TestHelpers.StackUnitGrid(
                definitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom)]),
            movementPoints: 5);

        ScenarioEventDefinition victory = new(
            new ScenarioEventId("win"),
            AffectedRaces: [WorldRace.Human],
            VerifiedRaces: [WorldRace.Human],
            Conditions:
            [
                new ScenarioEventOwningCityCondition(city.Id),
                new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Stack(stack.Id))
            ],
            Effects: [new ScenarioEventWinEffect(human.Id)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        (WorldEngine engine, IEngineContext context) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithCity(city)
            .WithStack(stack)
            .WithScenarioEvent(victory)
            .BuildEngineWithContext();

        WorldRenderState renderState = new WorldRenderComposer(definitions, context.Scenario, new TestMetadataProviderBuilder().Build())
            .Compose(MakeSceneRequest(engine.State, fogEnabled: false, cursor: WorldCursor.Empty));

        Assert.That(renderState.ObjectiveMarkers, Is.EqualTo(
        [
            new ObjectiveMarker(new WorldFootprint(new WorldPosition(1, 1), 4, 4), HasRunes: true),
            new ObjectiveMarker(new WorldFootprint(new WorldPosition(6, 6), 1, 1), HasRunes: false)
        ]));
    }

    [Test]
    public void MovingObjectiveStackTest()
    {
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 5, leadership: 5);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(leader).Build();

        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Human, 1),
            WorldResourceSet.Empty);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            human.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("s1"),
            human.Id,
            new WorldPosition(6, 6),
            TestHelpers.StackUnitGrid(
                definitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom)]),
            movementPoints: 5);

        ScenarioEventDefinition victory = new(
            new ScenarioEventId("win"),
            AffectedRaces: [WorldRace.Human],
            VerifiedRaces: [WorldRace.Human],
            Conditions:
            [
                new ScenarioEventOwningCityCondition(city.Id),
                new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Stack(stack.Id))
            ],
            Effects: [new ScenarioEventWinEffect(human.Id)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        (WorldEngine engine, IEngineContext context) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithCity(city)
            .WithStack(stack)
            .WithScenarioEvent(victory)
            .BuildEngineWithContext();

        WorldRenderRequest request = MakeSceneRequest(engine.State, fogEnabled: false, cursor: WorldCursor.Empty) with
        {
            Animation = new WorldAnimationState.StackMove(
                stack.Id,
                [
                    new WorldAnimationState.StackMoveStep(new WorldPosition(5, 6), WorldStackDirection.SouthEast, TimeSpan.Zero),
                    new WorldAnimationState.StackMoveStep(new WorldPosition(6, 6), WorldStackDirection.SouthEast, TimeSpan.FromMilliseconds(200))
                ])
        };

        WorldRenderState renderState = new WorldRenderComposer(definitions, context.Scenario, new TestMetadataProviderBuilder().Build()).Compose(request);

        Assert.That(renderState.ObjectiveMarkers, Is.EqualTo(
        [
            new ObjectiveMarker(new WorldFootprint(new WorldPosition(1, 1), 4, 4), HasRunes: true)
        ]));
        Assert.That(renderState.Animation, Is.EqualTo(new StackMove(
            stack.Id,
            new AssetSoundKey.StackUnitMove(leader.Id, Variant: 3),
            new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
            [
                new StackMoveStep(
                    new WorldPosition(5, 6),
                    6023,
                    new AssetAnimationKey.StackUnit(leader.Id, AssetAnimationKey.StackUnitPose.Move, 6),
                    ShadowKey: null,
                    AuraKey: null,
                    TimeSpan.Zero),
                new StackMoveStep(
                    new WorldPosition(6, 6),
                    6535,
                    new AssetAnimationKey.StackUnit(leader.Id, AssetAnimationKey.StackUnitPose.Move, 6),
                    ShadowKey: null,
                    AuraKey: null,
                    TimeSpan.FromMilliseconds(200))
            ],
            Opacity: 1.0,
            IsObjective: true,
            Highlight: null)));
    }

    [Test]
    public void FoggedObjectiveMarkerTest()
    {
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 5, leadership: 5);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(leader).Build();

        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar([
                "????????",
                "????????",
                "????????",
                "????????",
                "????????",
                "????????",
                "????????",
                "????????"
            ])
        };

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("s1"),
            human.Id,
            new WorldPosition(6, 6),
            TestHelpers.StackUnitGrid(
                definitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom)]),
            movementPoints: 5);

        ScenarioEventDefinition victory = new(
            new ScenarioEventId("win"),
            AffectedRaces: [WorldRace.Human],
            VerifiedRaces: [WorldRace.Human],
            Conditions: [new ScenarioEventDestroyingStackCondition(new ScenarioStackReference.Stack(stack.Id))],
            Effects: [new ScenarioEventWinEffect(human.Id)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        (WorldEngine engine, IEngineContext context) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithScenarioEvent(victory)
            .BuildEngineWithContext();

        WorldRenderState renderState = new WorldRenderComposer(definitions, context.Scenario, new TestMetadataProviderBuilder().Build())
            .Compose(MakeSceneRequest(engine.State, fogEnabled: true, cursor: WorldCursor.Empty));

        Assert.That(renderState.ObjectiveMarkers, Is.Empty);
    }

    [Test]
    public void HoverPathTest()
    {
        (WorldState state, IGameDefinitionProvider definitions, WorldStackId stackId) = BuildStackWorld(new WorldUnitId("leader"));

        WorldPath path = new([
            new WorldPathSegment(new WorldPosition(1, 1), new WorldPosition(2, 1), Cost: 2, Turn: 0),
            new WorldPathSegment(new WorldPosition(2, 1), new WorldPosition(3, 1), Cost: 3, Turn: 0)
        ]);

        WorldCursor cursor = WorldCursor.Empty.SelectStack(stackId) with
        {
            HoverPath = new WorldPathSelection(new WorldPosition(3, 1), path, new WorldPath([]), Encounter: null)
        };

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: true, cursor: cursor);

        Assert.That(renderState.SelectedPath, Is.EqualTo(new PathOverlay(new WorldPosition(1, 1), [
            new PathStep(new WorldPosition(2, 1), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "2"),
            new PathStep(new WorldPosition(3, 1), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "5")
        ])));
    }

    [Test]
    public void InterceptedHoverPathTest()
    {
        (WorldState state, IGameDefinitionProvider definitions, WorldStackId stackId) = BuildStackWorld(new WorldUnitId("leader"));

        WorldPath path = new([
            new WorldPathSegment(new WorldPosition(1, 1), new WorldPosition(2, 1), Cost: 2, Turn: 0)
        ]);
        WorldPath remainder = new([
            new WorldPathSegment(new WorldPosition(2, 1), new WorldPosition(3, 1), Cost: 2, Turn: 0),
            new WorldPathSegment(new WorldPosition(3, 1), new WorldPosition(4, 1), Cost: 2, Turn: 0)
        ]);

        WorldCursor cursor = WorldCursor.Empty.SelectStack(stackId) with
        {
            HoverPath = new WorldPathSelection(new WorldPosition(4, 1), path, remainder, new WorldPathEncounter.Battle(new WorldPosition(3, 0)))
        };

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: true, cursor: cursor);

        Assert.That(renderState.SelectedPath, Is.EqualTo(new PathOverlay(new WorldPosition(1, 1), [
            new PathStep(new WorldPosition(2, 1), Theme.World.Path.AttackInnerColor, Theme.World.Path.AttackOuterColor, "2"),
            new PathStep(new WorldPosition(3, 1), Theme.World.Path.UnreachedInnerColor, Theme.World.Path.UnreachedOuterColor, "4"),
            new PathStep(new WorldPosition(4, 1), Theme.World.Path.UnreachedInnerColor, Theme.World.Path.UnreachedOuterColor, "6")
        ])));
        Assert.That(renderState.Cursors, Is.Empty);
    }

    [Test]
    public void AttackHoverPathTest()
    {
        (WorldState state, IGameDefinitionProvider definitions, WorldStackId stackId) = BuildStackWorld(new WorldUnitId("leader"));

        WorldPath path = new([
            new WorldPathSegment(new WorldPosition(1, 1), new WorldPosition(2, 1), Cost: 2, Turn: 0)
        ]);
        WorldPath remainder = new([
            new WorldPathSegment(new WorldPosition(2, 1), new WorldPosition(3, 1), Cost: 2, Turn: 0)
        ]);

        WorldCursor cursor = WorldCursor.Empty.SelectStack(stackId) with
        {
            HoverPath = new WorldPathSelection(new WorldPosition(3, 1), path, remainder, new WorldPathEncounter.Battle(new WorldPosition(3, 1)))
        };

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: true, cursor: cursor);

        Assert.That(renderState.SelectedPath, Is.EqualTo(new PathOverlay(new WorldPosition(1, 1), [
            new PathStep(new WorldPosition(2, 1), Theme.World.Path.AttackInnerColor, Theme.World.Path.AttackOuterColor, "2"),
            new PathStep(new WorldPosition(3, 1), Theme.World.Path.AttackInnerColor, Theme.World.Path.AttackOuterColor, "4")
        ])));
        Assert.That(renderState.Cursors, Is.EqualTo(new Cursor[]
        {
            new(new WorldPosition(3, 1), 2368, Theme.World.Path.AttackInnerColor, Theme.World.Path.AttackOuterColor, 1.0)
        }));
    }

    [Test]
    public void BagHoverPathTest()
    {
        (WorldState state, IGameDefinitionProvider definitions, WorldStackId stackId) = BuildStackWorld(new WorldUnitId("leader"));

        WorldPath path = new([
            new WorldPathSegment(new WorldPosition(1, 1), new WorldPosition(2, 1), Cost: 2, Turn: 0)
        ]);
        WorldPath remainder = new([
            new WorldPathSegment(new WorldPosition(2, 1), new WorldPosition(3, 1), Cost: 2, Turn: 0)
        ]);

        WorldCursor cursor = WorldCursor.Empty.SelectStack(stackId) with
        {
            HoverPath = new WorldPathSelection(new WorldPosition(3, 1), path, remainder, new WorldPathEncounter.Bag(new WorldBagId("Bag")))
        };

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: true, cursor: cursor);

        Assert.That(renderState.SelectedPath, Is.EqualTo(new PathOverlay(new WorldPosition(1, 1), [
            new PathStep(new WorldPosition(2, 1), Theme.World.Path.ValidInnerColor, Theme.World.Path.ValidOuterColor, "2"),
            new PathStep(new WorldPosition(3, 1), Theme.World.Path.UnreachedInnerColor, Theme.World.Path.UnreachedOuterColor, "4")
        ])));
    }

    [Test]
    public void AmbientSoundsTest()
    {
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().Build();

        WorldMap map = TestHelpers.SimpleMap(16, 10);

        WorldPlayer undead = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldCapital capital = TestHelpers.SimpleCapital(
            new WorldFortificationId("f1"),
            undead.Id,
            new WorldPosition(4, 4),
            UnitGrid<WorldUnit>.Empty);

        WorldCity city = new(
            Id: new WorldFortificationId("f2"),
            NameTextId: new ScenarioTextId("Name"),
            DefinitionId: new WorldCityDefinitionId(WorldRace.Human, 1),
            OwnerId: undead.Id,
            SubraceId: TestHelpers.NeutralSubrace.Id,
            Footprint: new WorldFootprint(new WorldPosition(6, 0), 4, 4),
            Garrison: UnitGrid<WorldUnit>.Empty,
            Inventory: WorldInventory.Empty,
            AiPriority: 3,
            RiotTurns: 0,
            GrowthTurn: 0);

        WorldRuin ruin = new(
            Id: new WorldFortificationId("f3"),
            NameTextId: new ScenarioTextId("Ruin"),
            Footprint: new WorldFootprint(new WorldPosition(12, 0), 3, 3),
            Variant: new WorldObjectVariant(4),
            Garrison: UnitGrid<WorldUnit>.Empty,
            Resources: WorldResourceSet.Empty,
            Item: null,
            LooterId: null,
            AiPriority: 3);

        WorldLandmark waterfall = new(
            Id: new WorldLandmarkId("lm1"),
            DefinitionId: new WorldLandmarkDefinitionId("g000mg0006"),
            Footprint: new WorldFootprint(new WorldPosition(0, 8), 2, 2));

        WorldLandmark unmapped = new(
            Id: new WorldLandmarkId("lm2"),
            DefinitionId: new WorldLandmarkDefinitionId("g000mg0003"),
            Footprint: new WorldFootprint(new WorldPosition(4, 8), 1, 1));

        WorldMageSite mageSite = new(
            Id: new WorldSiteId("s1"),
            Footprint: new WorldFootprint(new WorldPosition(0, 5), 3, 3),
            Variant: new WorldObjectVariant(2),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            Spells: [],
            VisitorIds: [],
            AiPriority: 3);

        WorldMerchantSite windmill = new(
            Id: new WorldSiteId("s2"),
            Footprint: new WorldFootprint(new WorldPosition(4, 5), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            Items: [],
            VisitorIds: [],
            AiPriority: 3);

        WorldMerchantSite shop = new(
            Id: new WorldSiteId("s3"),
            Footprint: new WorldFootprint(new WorldPosition(8, 5), 3, 3),
            Variant: new WorldObjectVariant(1),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            Items: [],
            VisitorIds: [],
            AiPriority: 3);

        WorldMercenarySite mercenary = new(
            Id: new WorldSiteId("s4"),
            Footprint: new WorldFootprint(new WorldPosition(12, 5), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            Units: [],
            VisitorIds: [],
            AiPriority: 3);

        WorldResourceNode goldMine = new(
            Id: new WorldResourceNodeId("r1"),
            Position: new WorldPosition(8, 8),
            ResourceType: WorldResourceType.Gold,
            AiPriority: 3);

        WorldResourceNode manaNode = new(
            Id: new WorldResourceNodeId("r2"),
            Position: new WorldPosition(10, 8),
            ResourceType: WorldResourceType.DeathMana,
            AiPriority: 3);

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(undead.LordId, undead.Race, LordCategory.Warrior))
            .WithPlayer(undead)
            .WithCapital(capital)
            .WithDefinition(TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty))
            .WithCity(city)
            .WithRuin(ruin)
            .WithLandmark(waterfall)
            .WithLandmark(unmapped)
            .WithSite(mageSite)
            .WithSite(windmill)
            .WithSite(shop)
            .WithSite(mercenary)
            .WithResourceNode(goldMine)
            .WithResourceNode(manaNode)
            .Build()
            .State;

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: false, cursor: WorldCursor.Empty);

        Assert.That(renderState.AmbientSounds, Is.EqualTo(new AmbientSound[]
        {
            new(new AssetSoundKey.AmbientCapital(WorldRace.Undead), capital.Footprint),
            new(new AssetSoundKey.Ambient(AssetSoundKey.AmbientSoundType.City), city.Footprint),
            new(new AssetSoundKey.AmbientRuin(1), ruin.Footprint),
            new(new AssetSoundKey.Ambient(AssetSoundKey.AmbientSoundType.Waterfall), waterfall.Footprint),
            new(new AssetSoundKey.Ambient(AssetSoundKey.AmbientSoundType.MagicTower), mageSite.Footprint),
            new(new AssetSoundKey.Ambient(AssetSoundKey.AmbientSoundType.Windmill), windmill.Footprint),
            new(new AssetSoundKey.Ambient(AssetSoundKey.AmbientSoundType.GoldMine), new WorldFootprint(goldMine.Position, 1, 1))
        }));
    }

    [Test]
    public void AmbientSoundsUnderFogTest()
    {
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().Build();

        WorldMap map = TestHelpers.SimpleMap(8, 6);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar([
                "????????",
                "????.???",
                "?..?????",
                "????????",
                "????????",
                "???????."
            ])
        };

        WorldLandmark visibleWaterfall = new(
            Id: new WorldLandmarkId("lm1"),
            DefinitionId: new WorldLandmarkDefinitionId("g000mg0006"),
            Footprint: new WorldFootprint(new WorldPosition(4, 0), 2, 2));

        WorldLandmark foggedWaterfall = new(
            Id: new WorldLandmarkId("lm2"),
            DefinitionId: new WorldLandmarkDefinitionId("g000mg0007"),
            Footprint: new WorldFootprint(new WorldPosition(0, 5), 1, 1));

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithLandmark(visibleWaterfall)
            .WithLandmark(foggedWaterfall)
            .Build()
            .State;

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: true, cursor: WorldCursor.Empty);

        Assert.That(renderState.AmbientSounds, Is.EqualTo(new AmbientSound[]
        {
            new(new AssetSoundKey.Ambient(AssetSoundKey.AmbientSoundType.Waterfall), visibleWaterfall.Footprint)
        }));
    }

    [Test]
    public void InvisibleStackMoveTest()
    {
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 5, leadership: 5);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(leader).Build();

        WorldMap map = TestHelpers.SimpleMap(2, 1);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("s1"),
                human.Id,
                new WorldPosition(0, 0),
                TestHelpers.StackUnitGrid(definitions, [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom)]),
                movementPoints: 0) with
            {
                Concealed = true
            };

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .Build()
            .State;

        WorldRenderRequest request = MakeSceneRequest(state, fogEnabled: false, cursor: WorldCursor.Empty) with
        {
            Animation = new WorldAnimationState.StackMove(
                stack.Id,
                [
                    new WorldAnimationState.StackMoveStep(new WorldPosition(0, 0), WorldStackDirection.SouthEast, TimeSpan.Zero),
                    new WorldAnimationState.StackMoveStep(new WorldPosition(1, 0), WorldStackDirection.SouthEast, TimeSpan.FromMilliseconds(200))
                ])
        };

        WorldRenderState renderState = new WorldRenderComposer(definitions, GameScenarioProvider.Empty, new TestMetadataProviderBuilder().Build()).Compose(request);

        Assert.That(renderState.Animation, Is.EqualTo(new StackMove(
            stack.Id,
            new AssetSoundKey.StackUnitMove(leader.Id, Variant: 1),
            new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
            [
                new StackMoveStep(
                    new WorldPosition(0, 0),
                    391,
                    new AssetAnimationKey.StackUnit(leader.Id, AssetAnimationKey.StackUnitPose.Move, 6),
                    ShadowKey: null,
                    AuraKey: null,
                    TimeSpan.Zero),
                new StackMoveStep(
                    new WorldPosition(1, 0),
                    903,
                    new AssetAnimationKey.StackUnit(leader.Id, AssetAnimationKey.StackUnitPose.Move, 6),
                    ShadowKey: null,
                    AuraKey: null,
                    TimeSpan.FromMilliseconds(200))
            ],
            Opacity: 0.5,
            IsObjective: false,
            Highlight: null)));
    }

    [Test]
    public void GroundTerrainOrderTest()
    {
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().Build();

        WorldMap map = TestHelpers.BuildMap([
            "DEHUR.~"
        ]);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar([
                "......."
            ])
        };

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .Build()
            .State;

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: true, cursor: WorldCursor.Empty);

        Assert.That(renderState.Terrain.GroundTerrains, Is.EqualTo([
            WorldMapTerrain.Neutral,
            WorldMapTerrain.Heretic,
            WorldMapTerrain.Undead,
            WorldMapTerrain.Human,
            WorldMapTerrain.Elf,
            WorldMapTerrain.Dwarf
        ]));
    }

    [Test]
    public void TombTest()
    {
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().Build();

        WorldMap map = TestHelpers.BuildMap([
            "....",
            ".~.."
        ]);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            FogOfWar = TestHelpers.BuildFogOfWar([
                "????",
                "...."
            ])
        };

        WorldBag bag = new(
            Id: new WorldBagId("b1"),
            Position: new WorldPosition(0, 1),
            Variant: new WorldObjectVariant(2),
            Inventory: WorldInventory.Empty,
            AiPriority: 3);

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithBag(bag)
            .WithTomb(new WorldTomb(new WorldTombId("t1"), new WorldPosition(0, 1), human.Id, new UnitDefinitionId("leader"), LeaderName: null, DestroyerPlayerId: null, DestroyerLeaderDefinitionId: null, DestroyerLeaderName: null, Turn: 1))
            .WithTomb(new WorldTomb(new WorldTombId("t2"), new WorldPosition(1, 1), human.Id, new UnitDefinitionId("leader"), LeaderName: null, DestroyerPlayerId: null, DestroyerLeaderDefinitionId: null, DestroyerLeaderName: null, Turn: 1))
            .WithTomb(new WorldTomb(new WorldTombId("t3"), new WorldPosition(0, 0), human.Id, new UnitDefinitionId("leader"), LeaderName: null, DestroyerPlayerId: null, DestroyerLeaderDefinitionId: null, DestroyerLeaderName: null, Turn: 1))
            .WithTomb(new WorldTomb(new WorldTombId("t4"), new WorldPosition(0, 1), human.Id, new UnitDefinitionId("leader"), LeaderName: null, DestroyerPlayerId: null, DestroyerLeaderDefinitionId: null, DestroyerLeaderName: null, Turn: 2))
            .Build()
            .State;

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: true, cursor: WorldCursor.Empty);

        Assert.That(renderState.MapObjects, Is.EqualTo(new MapObject[]
        {
            new Tomb(
                Position: new WorldPosition(0, 1),
                Depth: 901,
                AssetKey: new AssetSpriteKey.Tombstone(IsWater: false)),
            new Bag(
                Position: new WorldPosition(0, 1),
                Depth: 905,
                AssetKey: new AssetSpriteKey.Bag(IsWater: false, 2)),
            new Tomb(
                Position: new WorldPosition(1, 1),
                Depth: 1413,
                AssetKey: new AssetSpriteKey.Tombstone(IsWater: true))
        }));
    }

    [Test]
    public void MovingSelectedStackTest()
    {
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 5, leadership: 5);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(leader).Build();

        WorldMap map = TestHelpers.SimpleMap(3, 1);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("s1"),
            human.Id,
            new WorldPosition(1, 0),
            TestHelpers.StackUnitGrid(
                definitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom)]),
            movementPoints: 5);

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .Build()
            .State;

        WorldRenderRequest request = MakeSceneRequest(state, fogEnabled: false, cursor: WorldCursor.Empty.SelectStack(stack.Id)) with
        {
            Animation = new WorldAnimationState.StackMove(
                stack.Id,
                [
                    new WorldAnimationState.StackMoveStep(new WorldPosition(0, 0), WorldStackDirection.SouthEast, TimeSpan.Zero),
                    new WorldAnimationState.StackMoveStep(new WorldPosition(1, 0), WorldStackDirection.SouthEast, TimeSpan.FromMilliseconds(200))
                ])
        };

        WorldRenderState renderState = new WorldRenderComposer(definitions, GameScenarioProvider.Empty, new TestMetadataProviderBuilder().Build()).Compose(request);

        Assert.That(renderState.Cursors, Is.Empty);
        Assert.That(renderState.Animation, Is.EqualTo(new StackMove(
            stack.Id,
            new AssetSoundKey.StackUnitMove(leader.Id, Variant: 1),
            new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
            [
                new StackMoveStep(
                    new WorldPosition(0, 0),
                    391,
                    new AssetAnimationKey.StackUnit(leader.Id, AssetAnimationKey.StackUnitPose.Move, 6),
                    ShadowKey: null,
                    AuraKey: null,
                    TimeSpan.Zero),
                new StackMoveStep(
                    new WorldPosition(1, 0),
                    903,
                    new AssetAnimationKey.StackUnit(leader.Id, AssetAnimationKey.StackUnitPose.Move, 6),
                    ShadowKey: null,
                    AuraKey: null,
                    TimeSpan.FromMilliseconds(200))
            ],
            Opacity: 1.0,
            IsObjective: false,
            Highlight: Theme.World.Highlight.SelectedColor)));
    }

    [Test]
    public void ReachTest()
    {
        (WorldState state, IGameDefinitionProvider definitions, WorldStackId stackId) = BuildStackWorld(new WorldUnitId("leader"));

        WorldCursor cursor = WorldCursor.Empty.SelectStack(stackId) with
        {
            ReachablePositions = [new WorldPosition(0, 1), new WorldPosition(2, 1)]
        };

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: false, cursor: cursor);

        Assert.That(renderState.Reach, Is.EqualTo(new ReachOverlay([new WorldPosition(1, 1), new WorldPosition(0, 1), new WorldPosition(2, 1)])));
    }

    [Test]
    public void HoverOutsideReachTest()
    {
        (WorldState state, IGameDefinitionProvider definitions, WorldStackId stackId) = BuildStackWorld(new WorldUnitId("leader"));

        WorldCursor cursor = WorldCursor.Empty.SelectStack(stackId) with
        {
            ReachablePositions = [new WorldPosition(0, 1)],
            HoverObject = new WorldPosition(1, 1),
            HoverTile = new WorldPosition(2, 1),
            HoverTarget = new WorldPosition(2, 1)
        };

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: false, cursor: cursor);

        Assert.That(renderState.Cursors, Is.Empty);
    }

    [Test]
    public void SelectedStackHighlightTest()
    {
        (WorldState state, IGameDefinitionProvider definitions, WorldStackId stackId) = BuildStackWorld(new WorldUnitId("leader"));

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: false, cursor: WorldCursor.Empty.SelectStack(stackId));

        Assert.That(renderState.Highlights, Is.EqualTo(new HighlightOverlay(
        [
            new Highlight(
                new Stack(
                    Position: new WorldPosition(1, 1),
                    Depth: 1415,
                    AssetKey: new AssetAnimationKey.StackUnit(new UnitDefinitionId("leader"), AssetAnimationKey.StackUnitPose.Stop, 7),
                    ShadowKey: null,
                    AuraKey: null,
                    BannerKey: new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
                    Opacity: 1.0),
                Theme.World.Highlight.ExhaustedColor)
        ])));
    }

    [Test]
    public void SelectedStackInCityHighlightTest()
    {
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 5, leadership: 5);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(leader).Build();

        WorldMap map = TestHelpers.SimpleMap(6, 6);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            human.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("s1"),
            human.Id,
            city.Entrance,
            TestHelpers.StackUnitGrid(
                definitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom)]),
            movementPoints: 5);

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithCity(city)
            .WithStack(stack)
            .Build()
            .State;

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: false, cursor: WorldCursor.Empty.SelectStack(stack.Id));

        Assert.That(renderState.Highlights, Is.EqualTo(new HighlightOverlay(
        [
            new Highlight(
                new CityShield(
                    Position: new WorldPosition(3, 3),
                    Depth: 3492,
                    Footprint: city.Footprint,
                    AssetKey: new AssetSpriteKey.CityShield(WorldRace.Human, IsCapital: false)),
                Theme.World.Highlight.SelectedColor)
        ])));
    }

    [Test]
    public void HostileCityHighlightTest()
    {
        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 5, leadership: 5);
        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder().AddUnit(leader).Build();

        WorldMap map = TestHelpers.SimpleMap(8, 6);

        WorldPlayer human = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer undead = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Undead, 1), WorldResourceSet.Empty);
        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            undead.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);
        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("s1"),
            human.Id,
            new WorldPosition(5, 3),
            TestHelpers.StackUnitGrid(
                definitions,
                [(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leader), GridPosition.FrontBottom)]),
            movementPoints: 5);

        WorldState state = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(human.LordId, human.Race, LordCategory.Warrior))
            .WithPlayer(human)
            .WithDefinition(TestHelpers.SimpleLord(undead.LordId, undead.Race, LordCategory.Warrior))
            .WithPlayer(undead)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithCity(city)
            .WithStack(stack)
            .Build()
            .State;

        WorldPath path = new([
            new WorldPathSegment(new WorldPosition(5, 3), new WorldPosition(4, 3), Cost: 2, Turn: 0)
        ]);
        WorldPath remainder = new([
            new WorldPathSegment(new WorldPosition(4, 3), new WorldPosition(3, 3), Cost: 0, Turn: 0)
        ]);

        WorldCursor cursor = WorldCursor.Empty.SelectStack(stack.Id) with
        {
            HoverPath = new WorldPathSelection(city.Entrance, path, remainder, new WorldPathEncounter.Battle(city.Entrance))
        };

        WorldRenderState renderState = Compose(state, definitions, fogEnabled: false, cursor: cursor);

        Assert.That(renderState.Highlights, Is.EqualTo(new HighlightOverlay(
        [
            new Highlight(
                new Stack(
                    Position: new WorldPosition(5, 3),
                    Depth: 4487,
                    AssetKey: new AssetAnimationKey.StackUnit(new UnitDefinitionId("leader"), AssetAnimationKey.StackUnitPose.Stop, 7),
                    ShadowKey: null,
                    AuraKey: null,
                    BannerKey: new AssetSpriteKey.StackBanner(TestHelpers.NeutralSubrace.BannerVariant.Value),
                    Opacity: 1.0),
                Theme.World.Highlight.SelectedColor),
            new Highlight(
                new Fortification(
                    Position: new WorldPosition(3, 3),
                    Depth: 1949,
                    Footprint: city.Footprint,
                    AssetKey: new AssetAnimationKey.City(1)),
                Theme.World.Highlight.HostileColor)
        ])));
    }
}
