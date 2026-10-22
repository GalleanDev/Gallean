using Gallean.Common;
using Gallean.Engine;
using Gallean.Tests.Common;

namespace Gallean.Client.Tests;

[Explicit]
[NonParallelizable]
public sealed class BattleAssetPerformanceTests
{
    private List<UnitDefinitionId> unitIds = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        GameLoader loader = new(new RandomProvider(0));
        (GameSession session, IGameTextProvider texts) = loader.LoadScenario("S107SC0000", worldState: null);
        using GameModel gameModel = new(session, texts, NullLog.Instance, SynchronousTaskScheduler.Instance);
        IGameDefinitionProvider definitions = gameModel.Definitions;

        List<UnitDefinitionId> ids = [];
        HashSet<UnitDefinitionId> listedIds = [];

        foreach (WorldRace race in Enum.GetValues<WorldRace>())
        {
            WorldRaceDefinition definition = definitions.GetRace(race);

            foreach (UnitDefinitionId unitId in definition.Leaders.Concat(definition.Units))
            {
                if (listedIds.Add(unitId))
                {
                    ids.Add(unitId);
                }
            }
        }

        unitIds = ids;
    }

    private static (PerformanceMeasurement Measurement, int ResolvedCount, long FrameCount, long DecodedBytes) MeasureCold(
        AssetProvider provider,
        IReadOnlyCollection<AssetAnimationKey> keys,
        AssetSpriteKey background)
    {
        PerformanceTimer timer = new();

        List<AssetCache<Animation>.Lease> leases = new(keys.Count);
        foreach (AssetAnimationKey key in keys)
        {
            if (provider.GetOptionalAnimation(key) is { } lease)
            {
                leases.Add(lease);
            }
        }

        AssetCache<Sprite>.Lease? backgroundLease = provider.GetOptionalSprite(background);

        PerformanceMeasurement measurement = timer.Stop(iterations: 1);

        long frameCount = 0;
        long decodedBytes = 0;

        foreach (AssetCache<Animation>.Lease lease in leases)
        {
            Animation animation = lease.Value;

            for (int index = 0; index < animation.Metadata.FrameCount; index++)
            {
                Sprite frame = animation[index];
                frameCount++;
                decodedBytes += DecodedBytes(frame);
            }
        }

        if (backgroundLease is not null)
        {
            decodedBytes += DecodedBytes(backgroundLease.Value);
        }

        int resolvedCount = leases.Count;

        foreach (AssetCache<Animation>.Lease lease in leases)
        {
            lease.Dispose();
        }

        backgroundLease?.Dispose();

        return (measurement, resolvedCount, frameCount, decodedBytes);
    }

    private static long DecodedBytes(Sprite sprite)
    {
        return (long)sprite.Image.Width * sprite.Image.Height * 4;
    }

    private static HashSet<AssetAnimationKey> BuildBattleKeys(IReadOnlyList<(UnitDefinitionId Id, BattleSide Side)> roster)
    {
        HashSet<AssetAnimationKey> keys = [];

        foreach ((UnitDefinitionId id, BattleSide side) in roster)
        {
            foreach (AssetAnimationKey.BattleUnitPose state in Enum.GetValues<AssetAnimationKey.BattleUnitPose>())
            {
                keys.Add(new AssetAnimationKey.BattleUnitShadow(id, state, side));
                keys.Add(new AssetAnimationKey.BattleUnit(id, state, side));
                keys.Add(new AssetAnimationKey.BattleUnitAura(id, state, side));
            }

            foreach (AssetAnimationKey.BattleEffect.AttackType effectType in Enum.GetValues<AssetAnimationKey.BattleEffect.AttackType>())
            {
                foreach (AssetAnimationKey.BattleLayer effectLayer in Enum.GetValues<AssetAnimationKey.BattleLayer>())
                {
                    foreach (AssetAnimationKey.BattleEffect.AttackSide effectSide in Enum.GetValues<AssetAnimationKey.BattleEffect.AttackSide>())
                    {
                        for (int variant = 0; variant < AssetAnimationKey.BattleEffect.Attack.VariantCount; variant++)
                        {
                            keys.Add(new AssetAnimationKey.BattleEffect.Attack(id, effectType, effectLayer, effectSide, variant));
                        }
                    }
                }
            }
        }

        foreach (UnitDeathAnimation type in Enum.GetValues<UnitDeathAnimation>())
        {
            keys.Add(new AssetAnimationKey.BattleEffect.Death(type, IsLarge: false));
            keys.Add(new AssetAnimationKey.BattleEffect.Death(type, IsLarge: true));
        }

        return keys;
    }

    [TestCase(6)]
    public void InitializeBattleTest(int unitsPerSide)
    {
        int rosterSize = unitsPerSide * 2;
        if (unitIds.Count < rosterSize)
        {
            Assert.Ignore();
        }

        using (AssetProvider provider = TestAssets.LoadOrIgnore())
        {
            List<(UnitDefinitionId Id, BattleSide Side)> roster = [];
            for (int index = 0; index < unitsPerSide; index++)
            {
                roster.Add((unitIds[index], BattleSide.Attacker));
            }
            for (int index = 0; index < unitsPerSide; index++)
            {
                roster.Add((unitIds[unitsPerSide + index], BattleSide.Defender));
            }

            HashSet<AssetAnimationKey> keys = BuildBattleKeys(roster);
            AssetSpriteKey background = new AssetSpriteKey.BattleBackground(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 0);

            (PerformanceMeasurement cold, int resolvedCount, long frameCount, long decodedBytes) = MeasureCold(provider, keys, background);

            TestContext.Out.WriteLine($"Battle: {unitsPerSide}x{unitsPerSide}, Units = {roster.Count}");
            TestContext.Out.WriteLine($"Animations: {resolvedCount:N0} / {keys.Count:N0}, Frames = {frameCount:N0}");
            TestContext.Out.WriteLine(
                $"Cold: Time = {Math.Round(cold.ElapsedTime.TotalMilliseconds):N0} ms, CPU = {Math.Round(cold.CpuTime.TotalMilliseconds):N0} ms, Decoded = {decodedBytes / (1024 * 1024)} MB");
        }
    }
}
