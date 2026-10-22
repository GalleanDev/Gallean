using Gallean.Engine.Tests;

namespace Gallean.Client.Tests;

public sealed class WorldAssetPreloadPlanTests
{
    [Test]
    public void WorldPlanTest()
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);

        AssetSpriteKey.Road road = new(AssetSpriteKey.RoadConnections.East | AssetSpriteKey.RoadConnections.West);
        AssetSpriteKey.GroundBorder blendMask = new(
            AssetSpriteKey.GroundBorderSet.Mask,
            new AssetSpriteKey.GroundBorderShape.Edges(AssetSpriteKey.GroundBorderEdges.East),
            0);
        AssetSpriteKey.GroundBorder coast = new(
            AssetSpriteKey.GroundBorderSet.Coast,
            new AssetSpriteKey.GroundBorderShape.Edges(AssetSpriteKey.GroundBorderEdges.West),
            0);
        AssetSpriteKey.GroundBorder coastMask = new(
            AssetSpriteKey.GroundBorderSet.Mask,
            new AssetSpriteKey.GroundBorderShape.Edges(AssetSpriteKey.GroundBorderEdges.West),
            0);
        AssetSpriteKey.Tree tree = new(WorldMapTerrain.Human, 0);
        AssetAnimationKey.Crystal crystal = new(WorldResourceType.Gold);
        AssetSpriteKey.Rod rod = new(WorldRace.Elf);
        AssetAnimationKey.City city = new(1);
        AssetAnimationKey.CityShadow cityShadow = new(2);
        AssetAnimationKey.StackBoat boat = new(WorldRace.Neutral, AssetAnimationKey.StackUnitPose.Stop, 1);
        AssetAnimationKey.StackBoatShadow boatShadowKey = new(WorldRace.Neutral, AssetAnimationKey.StackUnitPose.Stop, 1);
        AssetSpriteKey.StackBanner banner = new(4);
        AssetSpriteKey.CapitalBackground capitalBackground = new(WorldRace.Human, 0);
        AssetAnimationKey.CapitalBackgroundOverlay capitalOverlay = new(WorldRace.Human, 0);
        AssetAnimationKey.CapitalBuilding capitalBuilding = new(new WorldPlayerBuildingDefinitionId("G000BB0012"));

        WorldRenderState state = new(
            Terrain: new WorldRenderState.MapTerrain(map,
                [
                    new WorldRenderState.MapTile(new WorldPosition(0, 0), new WorldRenderState.MapTileSurface.Ground(WorldMapTerrain.Human), RoadKey: null),
                    new WorldRenderState.MapTile(new WorldPosition(1, 0), new WorldRenderState.MapTileSurface.Water(), RoadKey: null),
                    new WorldRenderState.MapTile(new WorldPosition(2, 0), new WorldRenderState.MapTileSurface.Ground(WorldMapTerrain.Human), RoadKey: road)
                ],
                [WorldMapTerrain.Human],
                [
                    new WorldRenderState.MapTileBorder.Blend(new WorldPosition(0, 0), blendMask, WorldMapTerrain.Neutral),
                    new WorldRenderState.MapTileBorder.Coast(new WorldPosition(1, 0), coast, WorldMapTerrain.Human, coastMask)
                ]),
            FogTiles: [],
            MapObjects:
            [
                new WorldRenderState.MapTileFeature(new WorldPosition(0, 0), Depth: 0, tree),
                new WorldRenderState.ResourceNode(new WorldPosition(1, 0), Depth: 0, crystal),
                new WorldRenderState.Rod(new WorldPosition(2, 0), Depth: 0, rod),
                new WorldRenderState.Fortification(new WorldPosition(2, 0), Depth: 0, new WorldFootprint(new WorldPosition(2, 0), 1, 1), city),
                new WorldRenderState.ObjectShadow(new WorldPosition(2, 0), Depth: 0, new WorldFootprint(new WorldPosition(2, 0), 1, 1), cityShadow)
            ],
            Stacks:
            [
                new WorldRenderState.Stack(new WorldPosition(1, 0), Depth: 0, boat, boatShadowKey, AuraKey: null, banner, Opacity: 1.0)
            ],
            Minimap: new MinimapState(1, 1, [], []),
            Cursors: [],
            Reach: null,
            Highlights: new WorldRenderState.HighlightOverlay([]),
            SelectedPath: null,
            Animation: null,
            ObjectiveMarkers: [], AmbientSounds: [],
            Clouds: []);

        CapitalSceneViewModel capitalScene = new(
            capitalBackground,
            [
                new CapitalSceneLayerViewModel(capitalBackground, AnimationKey: null, IsConstructed: false),
                new CapitalSceneLayerViewModel(SpriteKey: null, capitalOverlay, IsConstructed: false),
                new CapitalSceneLayerViewModel(SpriteKey: null, capitalBuilding, IsConstructed: false)
            ]);

        AssetPreloadPlan plan = WorldAssetPreloadPlan.BuildWorld(state, capitalScene);

        Assert.That(plan, Is.EqualTo(new AssetPreloadPlan(
            [
                new AssetPreloadItem.Sprite(new AssetSpriteKey.Ground(WorldMapTerrain.Human)),
                new AssetPreloadItem.Animation(new AssetAnimationKey.Water()),
                new AssetPreloadItem.Sprite(road),
                new AssetPreloadItem.Sprite(blendMask),
                new AssetPreloadItem.Sprite(coast),
                new AssetPreloadItem.Sprite(coastMask),
                new AssetPreloadItem.Sprite(tree),
                new AssetPreloadItem.Animation(crystal),
                new AssetPreloadItem.Sprite(rod),
                new AssetPreloadItem.Animation(city),
                new AssetPreloadItem.Animation(cityShadow),
                new AssetPreloadItem.Animation(boat),
                new AssetPreloadItem.Sprite(banner),
                new AssetPreloadItem.Animation(boatShadowKey),
                new AssetPreloadItem.Sprite(capitalBackground),
                new AssetPreloadItem.Animation(capitalOverlay),
                new AssetPreloadItem.Animation(capitalBuilding)
            ],
            [],
            [])));
    }

    [Test]
    public void StackMovePlanTest()
    {
        AssetAnimationKey.StackUnit unit = new(new UnitDefinitionId("G000UU0001"), AssetAnimationKey.StackUnitPose.Move, Direction: 2);
        AssetAnimationKey.StackUnitShadow shadow = new(new UnitDefinitionId("G000UU0001"), AssetAnimationKey.StackUnitPose.Move, Direction: 2);
        AssetSpriteKey.StackBanner banner = new(4);

        WorldRenderState.StackMove stackMove = new(
            new WorldStackId("Stack"),
            new AssetSoundKey.StackUnitMove(new UnitDefinitionId("G000UU0001"), Variant: 2),
            banner,
            [
                new WorldRenderState.StackMoveStep(new WorldPosition(0, 0), Depth: 0, unit, shadow, AuraKey: null, TimeSpan.Zero),
                new WorldRenderState.StackMoveStep(new WorldPosition(1, 0), Depth: 0, unit, shadow, AuraKey: null, TimeSpan.FromMilliseconds(200))
            ],
            Opacity: 1.0,
            IsObjective: false,
            Highlight: null);

        AssetPreloadPlan plan = WorldAssetPreloadPlan.BuildAnimation(stackMove);

        Assert.That(plan, Is.EqualTo(new AssetPreloadPlan(
            [
                new AssetPreloadItem.Sprite(banner),
                new AssetPreloadItem.Animation(unit),
                new AssetPreloadItem.Animation(shadow)
            ],
            [new AssetPreloadItem.Sound(new AssetSoundKey.StackUnitMove(new UnitDefinitionId("G000UU0001"), Variant: 2))],
            [])));
    }

    [Test]
    public void SpellCastPlanTest()
    {
        AssetAnimationKey.WorldSpellEffect layer = new(
            new WorldSpellDefinitionId("G000SS0008"),
            AssetAnimationKey.WorldSpellEffectLayer.Ground,
            Sublayer: 0);

        WorldRenderState.SpellCast spellCast = new(
            new AssetSoundKey.WorldSpellCast(new WorldSpellDefinitionId("G000SS0008")),
            new WorldPosition(0, 0),
            [new WorldRenderState.SpellCastLayer.Effect(layer, Depth: 1, FrameCount: 46)],
            SummonedStackId: null,
            FrameCount: 46);

        AssetPreloadPlan plan = WorldAssetPreloadPlan.BuildAnimation(spellCast);

        Assert.That(plan, Is.EqualTo(new AssetPreloadPlan(
            [new AssetPreloadItem.Animation(layer)],
            [new AssetPreloadItem.Sound(new AssetSoundKey.WorldSpellCast(new WorldSpellDefinitionId("G000SS0008")))],
            [])));
    }

    [Test]
    public void StackDestroyPlanTest()
    {
        AssetAnimationKey.StackDestroyEffect key = new();

        WorldRenderState.Effect stackDestroy = new(
            new AssetSoundKey.StackDestroy(),
            new WorldPosition(0, 0),
            Depth: 1,
            key,
            FrameCount: 16);

        AssetPreloadPlan plan = WorldAssetPreloadPlan.BuildAnimation(stackDestroy);

        Assert.That(plan, Is.EqualTo(new AssetPreloadPlan(
            [new AssetPreloadItem.Animation(key)],
            [new AssetPreloadItem.Sound(new AssetSoundKey.StackDestroy())],
            [])));
    }

    [Test]
    public void StackCreatePlanTest()
    {
        AssetAnimationKey.StackCreateEffect key = new();

        WorldRenderState.Effect stackCreate = new(
            new AssetSoundKey.StackCreate(),
            new WorldPosition(0, 0),
            Depth: 1,
            key,
            FrameCount: 16);

        AssetPreloadPlan plan = WorldAssetPreloadPlan.BuildAnimation(stackCreate);

        Assert.That(plan, Is.EqualTo(new AssetPreloadPlan(
            [new AssetPreloadItem.Animation(key)],
            [new AssetPreloadItem.Sound(new AssetSoundKey.StackCreate())],
            [])));
    }

    [Test]
    public void StackOwnerChangePlanTest()
    {
        AssetAnimationKey.StackOwnerChangeEffect key = new();

        WorldRenderState.Effect stackOwnerChange = new(
            SoundKey: null,
            new WorldPosition(0, 0),
            Depth: 1,
            key,
            FrameCount: 26);

        AssetPreloadPlan plan = WorldAssetPreloadPlan.BuildAnimation(stackOwnerChange);

        Assert.That(plan, Is.EqualTo(new AssetPreloadPlan([new AssetPreloadItem.Animation(key)], [], [])));
    }

    [Test]
    public void MountainRemovePlanTest()
    {
        AssetAnimationKey.MountainRemoveEffect key = new(5);

        WorldRenderState.Effect mountainRemove = new(
            SoundKey: null,
            new WorldPosition(0, 0),
            Depth: 1,
            key,
            FrameCount: 16);

        AssetPreloadPlan plan = WorldAssetPreloadPlan.BuildAnimation(mountainRemove);

        Assert.That(plan, Is.EqualTo(new AssetPreloadPlan([new AssetPreloadItem.Animation(key)], [], [])));
    }
}
