namespace Gallean.Client.Tests;

public sealed class BattleRenderStateExtensionsTests
{
    private static BattleRenderState.Unit Unit(int unitId, TimeSpan idleOffset)
    {
        return new BattleRenderState.Unit(
            Id: new BattleUnitId(unitId),
            DefinitionId: new UnitDefinitionId("DEF"),
            Race: WorldRace.Human,
            DeathAnimation: UnitDeathAnimation.Human,
            Footprint: new BattleGridFootprint(BattleSide.Defender, GridPosition.FrontCenter, IsLarge: false),
            Facing: BattleSide.Defender,
            IdleOffset: idleOffset,
            CorpseVariant: 0,
            Hitpoints: 40,
            Presence: BattleUnitState.Presence.Alive,
            Marker: null,
            Highlight: null,
            Auras: BattleUnitState.Auras.None,
            Frozen: false);
    }

    private static BattleAnimationState Animation(int unitId, double idleAt, double duration)
    {
        return new BattleAnimationState(
            new AnimationSequence(TimeSpan.FromSeconds(duration), Clips: [], Sounds: [], Properties: [], Pauses: []),
            [
                new BattleAnimationState.Unit(
                    new BattleUnitId(unitId),
                    [
                        new BattleAnimationState.BodyEntry(
                            TimeSpan.Zero,
                            TimeSpan.FromSeconds(idleAt),
                            AssetAnimationKey.BattleUnitPose.Hit,
                            BattleUnitState.Presence.Alive,
                            Opacity: null),
                        new BattleAnimationState.BodyEntry(
                            TimeSpan.FromSeconds(idleAt),
                            TimeSpan.Zero,
                            AssetAnimationKey.BattleUnitPose.Idle,
                            BattleUnitState.Presence.Alive,
                            Opacity: null)
                    ],
                    Form: [],
                    Feedback: [],
                    Motion: [],
                    Status: [])
            ],
            Effects: [],
            Animations: [],
            Shakes: [],
            Rate: new AnimationRate(15));
    }

    [Test]
    public void WithFinishedAnimationTest()
    {
        BattleRenderState.Unit[] units = [Unit(1, TimeSpan.FromTicks(7676067)), Unit(2, TimeSpan.FromTicks(666057))];
        BattleRenderState state = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 1),
            units,
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        BattleRenderState complete = state.WithFinishedAnimation(Animation(unitId: 2, idleAt: 1.2, duration: 2.0), TimeSpan.FromSeconds(100));

        BattleRenderState.Unit[] units1 = [Unit(1, TimeSpan.FromTicks(7676067)), Unit(2, TimeSpan.FromTicks(-992000000))];
        Assert.That(complete, Is.EqualTo(new BattleRenderState(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 1),
            units1,
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites)));
    }

    [Test]
    public void WithFinishedAnimationAbsentUnitTest()
    {
        BattleRenderState.Unit[] units = [Unit(1, TimeSpan.FromTicks(7676067))];
        BattleRenderState state = new(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 1),
            units,
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites);

        BattleRenderState complete = state.WithFinishedAnimation(Animation(unitId: 9, idleAt: 1.2, duration: 2.0), TimeSpan.FromSeconds(100));

        BattleRenderState.Unit[] units1 = [Unit(1, TimeSpan.FromTicks(7676067))];
        Assert.That(complete, Is.EqualTo(new BattleRenderState(
            new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 1),
            units1,
            Placeholders: [],
            HitTargets: [],
            BattleLightConfiguration.MakeMidday([]),
            FluidConfigurations: [],
            RainConfiguration: null,
            AnimationRate.Sprites)));
    }
}
