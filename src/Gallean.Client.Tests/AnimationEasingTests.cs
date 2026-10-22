namespace Gallean.Client.Tests;

public sealed class AnimationEasingTests
{
    [Test]
    public void LinearTest()
    {
        Assert.That(AnimationEasing.Linear.Ease(0.0), Is.Zero);
        Assert.That(AnimationEasing.Linear.Ease(0.25), Is.EqualTo(0.25));
        Assert.That(AnimationEasing.Linear.Ease(1.0), Is.EqualTo(1.0));
        Assert.That(AnimationEasing.Linear.Ease(-1.0), Is.Zero);
        Assert.That(AnimationEasing.Linear.Ease(2.0), Is.EqualTo(1.0));
    }

    [Test]
    public void SmoothTest()
    {
        Assert.That(AnimationEasing.Smooth.Ease(0.0), Is.Zero);
        Assert.That(AnimationEasing.Smooth.Ease(0.5), Is.EqualTo(0.5));
        Assert.That(AnimationEasing.Smooth.Ease(1.0), Is.EqualTo(1.0));
        Assert.That(AnimationEasing.Smooth.Ease(0.25), Is.EqualTo(0.15625));
        Assert.That(AnimationEasing.Smooth.Ease(0.75), Is.EqualTo(0.84375));
        Assert.That(AnimationEasing.Smooth.Ease(-1.0), Is.Zero);
        Assert.That(AnimationEasing.Smooth.Ease(2.0), Is.EqualTo(1.0));
    }

    [Test]
    public void DecelerateTest()
    {
        Assert.That(AnimationEasing.Decelerate.Ease(0.0), Is.Zero);
        Assert.That(AnimationEasing.Decelerate.Ease(0.5), Is.EqualTo(0.875));
        Assert.That(AnimationEasing.Decelerate.Ease(1.0), Is.EqualTo(1.0));
        Assert.That(AnimationEasing.Decelerate.Ease(0.75), Is.EqualTo(0.984375));
        Assert.That(AnimationEasing.Decelerate.Ease(-1.0), Is.Zero);
        Assert.That(AnimationEasing.Decelerate.Ease(2.0), Is.EqualTo(1.0));
    }
}
