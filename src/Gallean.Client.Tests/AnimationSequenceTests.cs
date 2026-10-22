namespace Gallean.Client.Tests;

public sealed class AnimationSequenceTests
{
    private static AnimationSequence Sequence(IReadOnlyList<AnimationPause> pauses)
    {
        return new AnimationSequence(TimeSpan.FromSeconds(1), Clips: [], Sounds: [], Properties: [], pauses);
    }

    [Test]
    public void SinglePauseTest()
    {
        AnimationSequence sequence = Sequence([new AnimationPause(TimeSpan.FromSeconds(0.4), TimeSpan.FromSeconds(0.2))]);

        Assert.That(sequence.GetTime(TimeSpan.FromSeconds(0.3)), Is.EqualTo(TimeSpan.FromSeconds(0.3)));
        Assert.That(sequence.GetTime(TimeSpan.FromSeconds(0.4)), Is.EqualTo(TimeSpan.FromSeconds(0.4)));
        Assert.That(sequence.GetTime(TimeSpan.FromSeconds(0.5)), Is.EqualTo(TimeSpan.FromSeconds(0.4)));
        Assert.That(sequence.GetTime(TimeSpan.FromSeconds(0.6)), Is.EqualTo(TimeSpan.FromSeconds(0.4)));
        Assert.That(sequence.GetTime(TimeSpan.FromSeconds(0.9)), Is.EqualTo(TimeSpan.FromSeconds(0.7)));
    }

    [Test]
    public void ConsecutivePausesTest()
    {
        AnimationSequence sequence = Sequence(
        [
            new AnimationPause(TimeSpan.FromSeconds(0.2), TimeSpan.FromSeconds(0.1)),
            new AnimationPause(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(0.3))
        ]);

        Assert.That(sequence.GetTime(TimeSpan.FromSeconds(0.25)), Is.EqualTo(TimeSpan.FromSeconds(0.2)));
        Assert.That(sequence.GetTime(TimeSpan.FromSeconds(0.4)), Is.EqualTo(TimeSpan.FromSeconds(0.3)));
        Assert.That(sequence.GetTime(TimeSpan.FromSeconds(0.7)), Is.EqualTo(TimeSpan.FromSeconds(0.5)));
        Assert.That(sequence.GetTime(TimeSpan.FromSeconds(1.0)), Is.EqualTo(TimeSpan.FromSeconds(0.6)));
        Assert.That(sequence.GetTime(TimeSpan.FromSeconds(1.4)), Is.EqualTo(TimeSpan.FromSeconds(1.0)));
    }

    [Test]
    public void NoPausesTest()
    {
        AnimationSequence sequence = Sequence([]);

        Assert.That(sequence.GetTime(TimeSpan.FromSeconds(0.9)), Is.EqualTo(TimeSpan.FromSeconds(0.9)));
    }
}
