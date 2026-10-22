namespace Gallean.Client.Tests;

public sealed class AnimationPropertyTests
{
    private sealed record TestTarget(string Name) : AnimationTarget;

    private static AnimationProperty Property(AnimationEasing easing)
    {
        return new AnimationProperty(
            new TestTarget("wound"),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            From: 10,
            To: 20,
            easing);
    }

    [Test]
    public void LinearTest()
    {
        AnimationProperty property = Property(AnimationEasing.Linear);

        Assert.That(property.ValueAt(TimeSpan.Zero), Is.EqualTo(10));
        Assert.That(property.ValueAt(TimeSpan.FromSeconds(1)), Is.EqualTo(10));
        Assert.That(property.ValueAt(TimeSpan.FromSeconds(2)), Is.EqualTo(15));
        Assert.That(property.ValueAt(TimeSpan.FromSeconds(3)), Is.EqualTo(20));
        Assert.That(property.ValueAt(TimeSpan.FromSeconds(9)), Is.EqualTo(20));
    }

    [Test]
    public void SmoothTest()
    {
        AnimationProperty property = Property(AnimationEasing.Smooth);

        Assert.That(property.ValueAt(TimeSpan.FromSeconds(1)), Is.EqualTo(10));
        Assert.That(property.ValueAt(TimeSpan.FromSeconds(1.5)), Is.EqualTo(11.5625));
        Assert.That(property.ValueAt(TimeSpan.FromSeconds(2)), Is.EqualTo(15));
        Assert.That(property.ValueAt(TimeSpan.FromSeconds(3)), Is.EqualTo(20));
    }

    [Test]
    public void ZeroDurationTest()
    {
        AnimationProperty property = new(
            new TestTarget("wound"),
            TimeSpan.FromSeconds(1),
            TimeSpan.Zero,
            From: 10,
            To: 20,
            AnimationEasing.Linear);

        Assert.That(property.ValueAt(TimeSpan.FromSeconds(0.5)), Is.EqualTo(10));
        Assert.That(property.ValueAt(TimeSpan.FromSeconds(1)), Is.EqualTo(20));
        Assert.That(property.ValueAt(TimeSpan.FromSeconds(2)), Is.EqualTo(20));
    }
}
