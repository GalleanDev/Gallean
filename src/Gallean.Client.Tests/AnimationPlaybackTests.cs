namespace Gallean.Client.Tests;

public sealed class AnimationPlaybackTests
{
    private sealed record TestAnimation(TimeSpan Duration) : IAnimationObject;

    [Test]
    public void RegisterAndCompleteTest()
    {
        AnimationPlayback<TestAnimation> playback = new();
        TestAnimation animation = new(TimeSpan.FromSeconds(1));

        Assert.That(playback.IsPlaying, Is.False);
        Assert.That(playback.Current, Is.Null);
        Assert.That(playback.TryComplete(TimeSpan.FromSeconds(10)), Is.False);

        Assert.That(playback.Register(animation, TimeSpan.FromSeconds(2)), Is.True);
        Assert.That(playback.IsPlaying, Is.True);
        Assert.That(playback.Current, Is.SameAs(animation));
        Assert.That(playback.GetElapsed(TimeSpan.FromSeconds(2.5)), Is.EqualTo(TimeSpan.FromSeconds(0.5)));

        Assert.That(playback.TryComplete(TimeSpan.FromSeconds(2.5)), Is.False);
        Assert.That(playback.IsPlaying, Is.True);

        Assert.That(playback.TryComplete(TimeSpan.FromSeconds(3)), Is.True);
        Assert.That(playback.IsPlaying, Is.False);

        Assert.That(playback.TryComplete(TimeSpan.FromSeconds(4)), Is.False);
    }

    [Test]
    public void RegisterSameInstanceTest()
    {
        AnimationPlayback<TestAnimation> playback = new();
        TestAnimation animation = new(TimeSpan.FromSeconds(1));

        Assert.That(playback.Register(animation, TimeSpan.FromSeconds(1)), Is.True);
        Assert.That(playback.Register(animation, TimeSpan.FromSeconds(1.5)), Is.False);

        Assert.That(playback.GetElapsed(TimeSpan.FromSeconds(1.5)), Is.EqualTo(TimeSpan.FromSeconds(0.5)));
    }

    [Test]
    public void RegisterNewInstanceTest()
    {
        AnimationPlayback<TestAnimation> playback = new();

        Assert.That(playback.Register(new TestAnimation(TimeSpan.FromSeconds(1)), TimeSpan.FromSeconds(1)), Is.True);
        Assert.That(playback.TryComplete(TimeSpan.FromSeconds(2)), Is.True);

        Assert.That(playback.Register(new TestAnimation(TimeSpan.FromSeconds(1)), TimeSpan.FromSeconds(3)), Is.True);
        Assert.That(playback.IsPlaying, Is.True);
        Assert.That(playback.GetElapsed(TimeSpan.FromSeconds(3.25)), Is.EqualTo(TimeSpan.FromSeconds(0.25)));
        Assert.That(playback.TryComplete(TimeSpan.FromSeconds(4)), Is.True);
    }

    [Test]
    public void RegisterNullTest()
    {
        AnimationPlayback<TestAnimation> playback = new();

        Assert.That(playback.Register(new TestAnimation(TimeSpan.FromSeconds(1)), TimeSpan.FromSeconds(1)), Is.True);
        Assert.That(playback.Register(null, TimeSpan.FromSeconds(1.5)), Is.True);

        Assert.That(playback.IsPlaying, Is.False);
        Assert.That(playback.Current, Is.Null);
        Assert.That(playback.GetElapsed(TimeSpan.FromSeconds(2)), Is.EqualTo(TimeSpan.Zero));
        Assert.That(playback.TryComplete(TimeSpan.FromSeconds(10)), Is.False);
    }
}
