namespace Gallean.Client.Tests;

public sealed class WorldAnimationStateTests
{
    [Test]
    public void NoFocusPositionTest()
    {
        Assert.That(new WorldAnimationState.Pause(TimeSpan.FromSeconds(0.75)).GetFocusInfo(TimeSpan.Zero), Is.Null);
        Assert.That(new WorldAnimationState.StackCreate(new WorldPosition(2, 2)).GetFocusInfo(TimeSpan.Zero), Is.Null);
        Assert.That(new WorldAnimationState.SpellCast(new WorldSpellDefinitionId("G000SS0008"), new WorldPosition(2, 2), SummonedStackId: null).GetFocusInfo(TimeSpan.Zero), Is.Null);
        Assert.That(new WorldAnimationState.BattleStart(new WorldPosition(2, 2)).GetFocusInfo(TimeSpan.Zero), Is.Null);
        Assert.That(new WorldAnimationState.BattleEnd(new WorldPosition(2, 2)).GetFocusInfo(TimeSpan.Zero), Is.Null);
    }

    [Test]
    public void StackMoveFocusInfoTest()
    {
        WorldAnimationState.StackMove move = new(
            new WorldStackId("Stack1"),
            [
                new WorldAnimationState.StackMoveStep(new WorldPosition(0, 0), WorldStackDirection.SouthEast, TimeSpan.Zero),
                new WorldAnimationState.StackMoveStep(new WorldPosition(1, 0), WorldStackDirection.South, TimeSpan.FromMilliseconds(200)),
                new WorldAnimationState.StackMoveStep(new WorldPosition(1, 1), WorldStackDirection.South, TimeSpan.FromMilliseconds(400))
            ]);

        Assert.That(move.GetFocusInfo(TimeSpan.Zero), Is.EqualTo(new WorldAnimationState.FocusInfo(new WorldPosition(1, 0), TimeSpan.FromMilliseconds(200))));
        Assert.That(move.GetFocusInfo(TimeSpan.FromMilliseconds(100)), Is.EqualTo(new WorldAnimationState.FocusInfo(new WorldPosition(1, 0), TimeSpan.FromMilliseconds(100))));
        Assert.That(move.GetFocusInfo(TimeSpan.FromMilliseconds(200)), Is.EqualTo(new WorldAnimationState.FocusInfo(new WorldPosition(1, 1), TimeSpan.FromMilliseconds(200))));
        Assert.That(move.GetFocusInfo(TimeSpan.FromMilliseconds(300)), Is.EqualTo(new WorldAnimationState.FocusInfo(new WorldPosition(1, 1), TimeSpan.FromMilliseconds(100))));
        Assert.That(move.GetFocusInfo(TimeSpan.FromMilliseconds(400)), Is.EqualTo(new WorldAnimationState.FocusInfo(new WorldPosition(1, 1), TimeSpan.Zero)));
        Assert.That(move.GetFocusInfo(TimeSpan.FromSeconds(5)), Is.EqualTo(new WorldAnimationState.FocusInfo(new WorldPosition(1, 1), TimeSpan.Zero)));
    }
}
