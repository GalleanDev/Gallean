using SkiaSharp;

namespace Gallean.Client.Tests;

public sealed class AnimationExtensionsTests
{
    [Test]
    public void ToAnimationProgressTest()
    {
        TimeSpan start = TimeSpan.FromSeconds(1);
        TimeSpan duration = TimeSpan.FromSeconds(2);

        Assert.That(TimeSpan.Zero.ToAnimationProgress(start, duration), Is.Zero);
        Assert.That(TimeSpan.FromSeconds(1).ToAnimationProgress(start, duration), Is.Zero);
        Assert.That(TimeSpan.FromSeconds(2).ToAnimationProgress(start, duration), Is.EqualTo(0.5));
        Assert.That(TimeSpan.FromSeconds(3).ToAnimationProgress(start, duration), Is.EqualTo(1.0));
        Assert.That(TimeSpan.FromSeconds(10).ToAnimationProgress(start, duration), Is.EqualTo(1.0));
    }

    [Test]
    public void ToAnimationProgressZeroTest()
    {
        TimeSpan start = TimeSpan.FromSeconds(1);

        Assert.That(TimeSpan.FromSeconds(0.5).ToAnimationProgress(start, TimeSpan.Zero), Is.Zero);
        Assert.That(TimeSpan.FromSeconds(1).ToAnimationProgress(start, TimeSpan.Zero), Is.EqualTo(1.0));
    }

    [Test]
    public void FrameByIndexTest()
    {
        TestAssetProviderBuilder builder = new();
        Sprite first = builder.CreateSprite(SKColors.White, 1, 1);
        Sprite second = builder.CreateSprite(SKColors.Black, 1, 1);
        Sprite third = builder.CreateSprite(SKColors.Gray, 1, 1);
        Animation animation = builder.CreateAnimation(
            new AssetAnimationMetadata(FrameCount: 3, Width: 1, Height: 1, OffsetX: 0, OffsetY: 0, TriggerFrame: null, SecondTriggerFrame: null),
            [first, second, third]);

        Assert.That(animation.FrameByIndex(-1, looping: true), Is.SameAs(third));
        Assert.That(animation.FrameByIndex(-3, looping: true), Is.SameAs(first));
        Assert.That(animation.FrameByIndex(-85, looping: true), Is.SameAs(third));
        Assert.That(animation.FrameByIndex(4, looping: true), Is.SameAs(second));
    }
}
