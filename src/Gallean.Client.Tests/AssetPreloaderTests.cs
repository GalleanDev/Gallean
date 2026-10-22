using Gallean.Tests.Common;
using SkiaSharp;

namespace Gallean.Client.Tests;

public sealed class AssetPreloaderTests
{
    private sealed class RequestLatch
    {
        private readonly ManualResetEventSlim release = new();
        private readonly TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int blocked;

        public int FirstThreadId { get; private set; }

        public Task WaitUntilStartedAsync()
        {
            return started.Task;
        }

        public void Release()
        {
            release.Set();
        }

        public void BlockOnFirstRequest(AssetKey key)
        {
            if (Interlocked.Exchange(ref blocked, 1) == 0)
            {
                FirstThreadId = Environment.CurrentManagedThreadId;
                started.SetResult();
                release.Wait();
            }
        }
    }

    [Test]
    public async Task PreloadInitialTest()
    {
        UnitDefinitionId definitionId = new("ATK");
        AssetSpriteKey.BattleBackground background = new(new AssetSpriteKey.BattleLocation.Terrain(WorldMapTerrain.Human), 1);
        AssetAnimationKey.BattleUnitAura aura = new(definitionId, AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker);
        AssetPreloadPlan plan = new(
            [
                new AssetPreloadItem.Sprite(background),
                new AssetPreloadItem.Animation(new AssetAnimationKey.BattleUnitShadow(definitionId, AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker)),
                new AssetPreloadItem.Animation(new AssetAnimationKey.BattleUnit(definitionId, AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker)),
                new AssetPreloadItem.Animation(aura)
            ],
            [],
            []);

        TestAssetProviderBuilder builder = new();
        Sprite sprite = builder.CreateSprite(SKColors.White, 1, 1);
        Animation animation = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 1, Width: 1, Height: 1, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null),
            [sprite]);
        RequestLatch latch = new();
        using TestAssetProvider assets = builder
            .ResolveSprite(_ => sprite)
            .ResolveAnimation(_ => animation)
            .ResolveSound(_ => null)
            .OnRequest(latch.BlockOnFirstRequest)
            .Build();
        AssetPreloader preloader = new(assets);

        Task<IDisposable> preloadTask = preloader.PreloadAsync(plan.InitialRequired);
        await latch.WaitUntilStartedAsync();

        Assert.That(preloadTask.IsCompleted, Is.False);
        Assert.That(latch.FirstThreadId, Is.Not.EqualTo(Environment.CurrentManagedThreadId));

        latch.Release();
        using IDisposable leases = await preloadTask;

        AssetKey[] preloadedAssets =
        [
            background,
            new AssetAnimationKey.BattleUnitShadow(definitionId, AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker),
            new AssetAnimationKey.BattleUnit(definitionId, AssetAnimationKey.BattleUnitPose.Idle, BattleSide.Attacker),
            aura
        ];

        Assert.That(assets.OptionalAssets, Is.EquivalentTo(preloadedAssets));
        Assert.That(assets.RequiredAssets, Is.Empty);
    }

    [Test]
    public async Task PreloadSoundTest()
    {
        AssetSoundKey key = new AssetSoundKey.BattleEffect.Heal();
        TestAssetProviderBuilder builder = new();
        AssetStream data = builder.CreateSound(TestSounds.Create(SoundDecoder.MixerSampleRate, SoundDecoder.MixerChannels, 64));
        int resolvedCount = 0;

        using TestAssetProvider assets = builder
            .ResolveSprite(_ => throw new KeyNotFoundException())
            .ResolveAnimation(_ => throw new KeyNotFoundException())
            .ResolveSound(_ =>
            {
                resolvedCount++;

                return data;
            })
            .Build();
        AssetPreloader preloader = new(assets);

        using IDisposable leases = await preloader.PreloadAsync([new AssetPreloadItem.Sound(key)]);

        using AssetCache<Sound>.Lease lease = Assert.NotNull(assets.GetOptionalSound(key));
        Sound sound = lease.Value;

        Assert.That(assets.OptionalAssets, Is.EqualTo([key, key]));
        Assert.That(resolvedCount, Is.EqualTo(1));
        Assert.That(sound.Samples, Has.Length.EqualTo(128));
    }
}
