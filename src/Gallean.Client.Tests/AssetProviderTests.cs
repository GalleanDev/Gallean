using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client.Tests;

public sealed class AssetProviderTests
{
    private IGameDefinitionProvider definitions = null!;
    private IAssetProvider provider = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        provider = TestAssets.LoadOrIgnore();

        GameLoader loader = new(new RandomProvider(0));
        (GameSession session, IGameTextProvider texts) = loader.LoadScenario("S107SC0000", worldState: null);
        using GameModel gameModel = new(session, texts, NullLog.Instance, SynchronousTaskScheduler.Instance);
        definitions = gameModel.Definitions;
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        if (provider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private void CheckSprite(AssetSpriteKey key)
    {
        using AssetCache<Sprite>.Lease lease = provider.GetRequiredSprite(key);
        Sprite sprite = lease.Value;

        Assert.That(sprite.Image, Is.Not.Null);
        Assert.That(sprite.Metadata.Width, Is.GreaterThan(0));
        Assert.That(sprite.Metadata.Height, Is.GreaterThan(0));
    }

    private static void CheckSpriteMetadata(AssetSpriteMetadata metadata, int width, int height)
    {
        Assert.That(metadata.Width, Is.EqualTo(width));
        Assert.That(metadata.Height, Is.EqualTo(height));
        Assert.That(metadata.OffsetX, Is.Zero);
        Assert.That(metadata.OffsetY, Is.Zero);

        Assert.That(metadata.PixelWidth % width, Is.Zero);
        Assert.That(metadata.PixelHeight % height, Is.Zero);
        Assert.That(metadata.PixelWidth / width, Is.EqualTo(metadata.PixelHeight / height));
        Assert.That(metadata.PixelWidth / width, Is.GreaterThanOrEqualTo(1));
    }

    private void CheckAnimation(AssetAnimationKey key)
    {
        using AssetCache<Animation>.Lease lease = provider.GetRequiredAnimation(key);
        Animation animation = lease.Value;

        Assert.That(animation.Metadata.FrameCount, Is.GreaterThan(0));
        Assert.That(animation[0].Metadata.Width, Is.GreaterThan(0));
        Assert.That(animation[0].Metadata.Height, Is.GreaterThan(0));
    }

    private void CheckUnitAnimationStates(UnitDefinitionId unitId)
    {
        AssetAnimationKey.BattleUnit attack = new(unitId, AssetAnimationKey.BattleUnitPose.Attack, BattleSide.Attacker);

        CheckAnimation(attack);
        CheckAnimation(new AssetAnimationKey.BattleUnit(unitId, AssetAnimationKey.BattleUnitPose.Hit, BattleSide.Defender));

        AssetAnimationMetadata attackMetadata = provider.GetRequiredAnimationMetadata(attack);

        Assert.That(attackMetadata.TriggerFrame, Is.Not.Null);
        Assert.That(attackMetadata.SecondTriggerFrame, Is.Not.Null);
    }

    private bool CheckSound(AssetSoundKey key)
    {
        using AssetCache<Sound>.Lease? lease = provider.GetOptionalSound(key);

        if (lease is not null)
        {
            Sound sound = lease.Value;

            Assert.That(sound.Samples, Is.Not.Empty);

            return true;
        }
        else
        {
            return false;
        }
    }

    private int CheckSoundVariants(int variantCount, Func<int, AssetSoundKey> createKey)
    {
        int found = 0;

        for (int variant = 0; variant < variantCount; variant++)
        {
            if (CheckSound(createKey(variant)))
            {
                found++;
            }
        }

        return found;
    }

    private void CheckBorderVariants(AssetSpriteKey.GroundBorderSet set, AssetSpriteKey.GroundBorderShape shape)
    {
        for (int variant = 0; variant < AssetSpriteKey.GroundBorder.GetVariantCount(set, shape); variant++)
        {
            CheckSprite(new AssetSpriteKey.GroundBorder(set, shape, variant));
        }
    }

    [Test]
    public void GroundSpriteTest([Values] WorldMapTerrain terrain)
    {
        CheckSprite(new AssetSpriteKey.Ground(terrain));
    }

    [Test]
    public void GroundBorderSpriteTest([Values] AssetSpriteKey.GroundBorderSet set)
    {
        for (int edges = 1; edges <= (int)AssetSpriteKey.GroundBorderEdges.All; edges++)
        {
            CheckBorderVariants(set, new AssetSpriteKey.GroundBorderShape.Edges((AssetSpriteKey.GroundBorderEdges)edges));
        }

        for (int corners = 1; corners <= (int)AssetSpriteKey.GroundBorderCorners.All; corners++)
        {
            CheckBorderVariants(set, new AssetSpriteKey.GroundBorderShape.Corners((AssetSpriteKey.GroundBorderCorners)corners));
        }
    }

    [Test]
    public void TreeSpriteTest(
        [Values(WorldMapTerrain.Human, WorldMapTerrain.Undead, WorldMapTerrain.Heretic, WorldMapTerrain.Dwarf, WorldMapTerrain.Neutral, WorldMapTerrain.Elf)] WorldMapTerrain terrain)
    {
        CheckSprite(new AssetSpriteKey.Tree(terrain, Variant: 0));
    }

    [Test]
    public void MountainSpriteTest([Values] WorldRace race)
    {
        foreach (int size in AssetSpriteKey.Mountain.Sizes)
        {
            for (int image = 0; image < AssetSpriteKey.Mountain.GetVariantCount(size); image++)
            {
                CheckSprite(new AssetSpriteKey.Mountain(race, size, image));
            }
        }
    }

    [Test]
    public void CrystalAnimationTest([Values] WorldResourceType resourceType)
    {
        CheckAnimation(new AssetAnimationKey.Crystal(resourceType));
    }

    [Test]
    public void CityAnimationTest([Values(1, 2, 3, 4, 5)] int level)
    {
        CheckAnimation(new AssetAnimationKey.City(level));
    }

    [Test]
    public void CapitalAnimationTest([Values] WorldRace race)
    {
        CheckAnimation(new AssetAnimationKey.Capital(race, IsCaptured: false));
    }

    [TestCase(WorldRace.Human, 0)]
    [TestCase(WorldRace.Human, 1)]
    [TestCase(WorldRace.Human, 2)]
    [TestCase(WorldRace.Undead, 0)]
    [TestCase(WorldRace.Heretic, 0)]
    [TestCase(WorldRace.Dwarf, 0)]
    [TestCase(WorldRace.Elf, 0)]
    public void CapitalBackgroundSpriteTest(WorldRace race, int variant)
    {
        using AssetCache<Sprite>.Lease lease = provider.GetRequiredSprite(new AssetSpriteKey.CapitalBackground(race, variant));

        CheckSpriteMetadata(lease.Value.Metadata, width: 640, height: 600);
    }

    [TestCase(WorldRace.Human, 0)]
    [TestCase(WorldRace.Human, 1)]
    [TestCase(WorldRace.Undead, 0)]
    [TestCase(WorldRace.Heretic, 0)]
    [TestCase(WorldRace.Dwarf, 0)]
    public void CapitalBackgroundOverlayTest(WorldRace race, int variant)
    {
        CheckAnimation(new AssetAnimationKey.CapitalBackgroundOverlay(race, variant));
    }

    [TestCase("G000BB0001")]
    [TestCase("G000BB0026")]
    [TestCase("G000BB0050")]
    [TestCase("G000BB0075")]
    [TestCase("G000BB0100")]
    public void CapitalBuildingAnimationTest(string buildingId)
    {
        CheckAnimation(new AssetAnimationKey.CapitalBuilding(new WorldPlayerBuildingDefinitionId(buildingId)));
    }

    [Test]
    public void CityShieldSpriteTest([Values] WorldRace race)
    {
        CheckSprite(new AssetSpriteKey.CityShield(race, IsCapital: false));
    }

    [Test]
    public void CityImageSpriteTest([Values(1, 2, 3, 4, 5)] int level)
    {
        CheckSprite(new AssetSpriteKey.CityImage(level));
    }

    [Test]
    public void CapitalImageSpriteTest([Values(WorldRace.Human, WorldRace.Undead, WorldRace.Heretic, WorldRace.Dwarf, WorldRace.Elf)] WorldRace race)
    {
        CheckSprite(new AssetSpriteKey.CapitalImage(race));
    }

    [Test]
    public void CapitalShieldSpriteTest([Values(WorldRace.Human, WorldRace.Undead, WorldRace.Heretic, WorldRace.Dwarf, WorldRace.Elf)] WorldRace race)
    {
        CheckSprite(new AssetSpriteKey.CityShield(race, IsCapital: true));
    }

    [Test]
    public void RodSpriteTest([Values(WorldRace.Human, WorldRace.Undead, WorldRace.Heretic, WorldRace.Dwarf, WorldRace.Elf)] WorldRace race)
    {
        CheckSprite(new AssetSpriteKey.Rod(race));
    }

    [Test]
    public void RaceSelectionBackgroundSpriteTest([Values(WorldRace.Human, WorldRace.Undead, WorldRace.Heretic, WorldRace.Dwarf, WorldRace.Elf)] WorldRace race)
    {
        CheckSprite(new AssetSpriteKey.RaceSelectionBackground(race));
    }

    [Test]
    public void RaceSelectionBackgroundOverlayTest()
    {
        CheckAnimation(new AssetAnimationKey.RaceSelectionBackgroundOverlay(WorldRace.Human, 0));
        CheckAnimation(new AssetAnimationKey.RaceSelectionBackgroundOverlay(WorldRace.Human, 1));
        CheckAnimation(new AssetAnimationKey.RaceSelectionBackgroundOverlay(WorldRace.Undead, 1));
        CheckAnimation(new AssetAnimationKey.RaceSelectionBackgroundOverlay(WorldRace.Heretic, 1));
    }

    [Test]
    public void RuinAnimationTest([Range(0, 10)] int variant, [Values] bool isLooted)
    {
        CheckAnimation(new AssetAnimationKey.Ruin(isLooted, variant));
    }

    [Test]
    public void RuinBannerSpriteTest([Values] bool isLooted)
    {
        CheckSprite(new AssetSpriteKey.RuinBanner(isLooted));
    }

    [Test]
    public void SiteBannerAnimationTest([Values] AssetAnimationKey.SiteType type)
    {
        CheckAnimation(new AssetAnimationKey.SiteBanner(type));
    }

    [Test]
    public void BagLandSpriteTest([Range(0, 7)] int variant)
    {
        CheckSprite(new AssetSpriteKey.Bag(IsWater: false, variant));
    }

    [Test]
    public void BagWaterSpriteTest([Range(0, 3)] int variant)
    {
        CheckSprite(new AssetSpriteKey.Bag(IsWater: true, variant));
    }

    [Test]
    public void BattleBackgroundSpriteTest([Values] WorldMapTerrain terrain, [Values(0, 1, 2)] int variant)
    {
        CheckSprite(new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(terrain), variant));
    }

    [Test]
    public void CityBattleBackgroundSpriteTest([Values(0, 1, 2)] int variant)
    {
        CheckSprite(new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.City(), variant));
    }

    [Test]
    public void RuinBattleBackgroundSpriteTest([Values(0, 1, 2)] int variant)
    {
        CheckSprite(new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Ruin(), variant));
    }

    [Test]
    public void CapitalBattleBackgroundSpriteTest(
        [Values(WorldRace.Human, WorldRace.Undead, WorldRace.Heretic, WorldRace.Dwarf, WorldRace.Elf)] WorldRace race)
    {
        CheckSprite(new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Capital(race), Variant: 0));
    }

    [Test]
    public void WaterAnimationTest()
    {
        CheckAnimation(new AssetAnimationKey.Water());
    }

    [Test]
    public void BoatAnimationTest([Values] WorldRace race, [Values] AssetAnimationKey.StackUnitPose pose, [Range(0, 7)] int direction)
    {
        CheckAnimation(new AssetAnimationKey.StackBoat(race, pose, direction));
    }

    [Test]
    public void StackUnitAnimationTest([Values] WorldRace race)
    {
        WorldRaceDefinition definition = definitions.GetRace(race);

        foreach (UnitDefinitionId unitId in definition.Leaders)
        {
            foreach (AssetAnimationKey.StackUnitPose pose in Enum.GetValues<AssetAnimationKey.StackUnitPose>())
            {
                CheckAnimation(new AssetAnimationKey.StackUnit(unitId, pose, 1));
            }
        }
    }

    [Test]
    public void UnitTest([Values] WorldRace race)
    {
        WorldRaceDefinition definition = definitions.GetRace(race);

        foreach (UnitDefinitionId unitId in definition.Leaders)
        {
            CheckAnimation(new AssetAnimationKey.BattleUnit(unitId, AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker));
            CheckSprite(new AssetSpriteKey.UnitFace(unitId));
            CheckSprite(new AssetSpriteKey.UnitPortrait(unitId));
        }

        foreach (UnitDefinitionId unitId in definition.Units)
        {
            CheckAnimation(new AssetAnimationKey.BattleUnit(unitId, AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker));
            CheckSprite(new AssetSpriteKey.UnitFace(unitId));
            CheckSprite(new AssetSpriteKey.UnitPortrait(unitId));
        }
    }

    [Test]
    public void UnitAnimationStateTest([Values] WorldRace race)
    {
        WorldRaceDefinition definition = definitions.GetRace(race);

        foreach (UnitDefinitionId unitId in definition.Leaders)
        {
            CheckUnitAnimationStates(unitId);
        }

        foreach (UnitDefinitionId unitId in definition.Units)
        {
            CheckUnitAnimationStates(unitId);
        }
    }

    [Test]
    public void DeathEffectTest([Values] UnitDeathAnimation type)
    {
        CheckAnimation(new AssetAnimationKey.BattleEffect.Death(type, IsLarge: false));

        if (type is UnitDeathAnimation.Elf)
        {
            CheckAnimation(new AssetAnimationKey.BattleEffect.Death(type, IsLarge: true));
        }
    }

    [Test]
    public void LevelUpEffectTest([Values] WorldRace race)
    {
        CheckAnimation(new AssetAnimationKey.BattleEffect.LevelUp(race, IsLarge: false));
        CheckAnimation(new AssetAnimationKey.BattleEffect.LevelUp(race, IsLarge: true));
    }

    [Test]
    public void StatusEffectTest([Values] AssetAnimationKey.BattleEffect.StatusType type)
    {
        CheckAnimation(new AssetAnimationKey.BattleEffect.Status(type, IsLarge: false));
        CheckAnimation(new AssetAnimationKey.BattleEffect.Status(type, IsLarge: true));
    }

    [Test]
    public void HealEffectTest()
    {
        CheckAnimation(new AssetAnimationKey.BattleEffect.Heal());
    }

    [Test]
    public void BlisterEffectTest()
    {
        CheckAnimation(new AssetAnimationKey.BattleEffect.Blister());
    }

    [Test]
    public void BattleEffectHealSoundTest()
    {
        int found = CheckSoundVariants(1, variant => new AssetSoundKey.BattleEffect.Heal());

        Assert.That(found, Is.EqualTo(1));
    }

    [Test]
    public void BattleEffectDeathSoundTest()
    {
        int found = CheckSoundVariants(1, variant => new AssetSoundKey.BattleEffect.Death());

        Assert.That(found, Is.EqualTo(1));
    }

    [Test]
    public void BattleEffectStatusSoundTest([Values] AssetSoundKey.BattleEffect.StatusType type)
    {
        int found = CheckSoundVariants(1, variant => new AssetSoundKey.BattleEffect.Status(type));

        Assert.That(found, Is.EqualTo(1));
    }

    [Test]
    public void UnitSoundTest([Values] WorldRace race)
    {
        WorldRaceDefinition definition = definitions.GetRace(race);
        int found = 0;

        foreach (UnitDefinitionId unitId in definition.Leaders.Concat(definition.Units))
        {
            found += CheckSoundVariants(AssetSoundKey.BattleUnitAttack.VariantCount, variant => new AssetSoundKey.BattleUnitAttack(unitId, variant));
            found += CheckSoundVariants(AssetSoundKey.BattleUnitHit.VariantCount, variant => new AssetSoundKey.BattleUnitHit(unitId, variant));
            found += CheckSoundVariants(AssetSoundKey.BattleUnitTouch.VariantCount, variant => new AssetSoundKey.BattleUnitTouch(unitId, variant));
            found += CheckSoundVariants(AssetSoundKey.BattleUnitDamaged.VariantCount, variant => new AssetSoundKey.BattleUnitDamaged(unitId, variant));
            found += CheckSoundVariants(AssetSoundKey.StackUnitMove.VariantCount, variant => new AssetSoundKey.StackUnitMove(unitId, variant));
        }

        if (definition.Leaders.Length + definition.Units.Length > 0)
        {
            Assert.That(found, Is.GreaterThan(0));
        }
    }

    [Test]
    public void UnitSoundDecodeTest([Values] WorldRace race)
    {
        WorldRaceDefinition definition = definitions.GetRace(race);

        foreach (UnitDefinitionId unitId in definition.Leaders.Concat(definition.Units))
        {
            CheckSoundVariants(AssetSoundKey.BattleUnitAttack.VariantCount, variant => new AssetSoundKey.BattleUnitAttack(unitId, variant));
        }
    }

    [Test]
    public void BuildingIconTest()
    {
        GameLoader loader = new(new RandomProvider(0));
        (GameSession session, IGameTextProvider texts) = loader.LoadScenario("S107SC0000", worldState: null);
        using GameModel gameModel = new(session, texts, NullLog.Instance, SynchronousTaskScheduler.Instance);

        IEnumerable<WorldPlayerBuildingDefinitionId> buildingIds = gameModel.Snapshot.WorldState.Players.Values
            .SelectMany(player => player.Buildings.Available)
            .Distinct();

        foreach (WorldPlayerBuildingDefinitionId buildingId in buildingIds)
        {
            CheckSprite(new AssetSpriteKey.BuildingIcon(buildingId));
        }
    }
}
