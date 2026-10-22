namespace Gallean.Client.Tests;

public sealed class AnimationRateTests
{
    [Test]
    public void GetFrameIndexTest()
    {
        AnimationRate rate = new(15);

        Assert.That(rate.GetFrameIndex(TimeSpan.Zero), Is.Zero);
        Assert.That(rate.GetFrameIndex(TimeSpan.FromSeconds(0.5)), Is.EqualTo(7));
        Assert.That(rate.GetFrameIndex(TimeSpan.FromSeconds(1)), Is.EqualTo(15));
    }
}
