using Gallean.Configuration;

namespace Gallean.Client.Tests;

public sealed class GameSettingsExtensionsTests
{
    [Test]
    public void VolumeRatioTest()
    {
        GameSettings settings = GameSettings.Create(0, 25, false, 100, 5, 10, BattleSpeed.Normal, null, true, MovementSpeed.Normal);

        Assert.That(settings.SoundVolumeRatio, Is.Zero);
        Assert.That(settings.MusicVolumeRatio, Is.EqualTo(0.25f));
    }

    [Test]
    public void FullVolumeRatioTest()
    {
        Assert.That(GameSettings.Default.SoundVolumeRatio, Is.EqualTo(1.0f));
        Assert.That(GameSettings.Default.MusicVolumeRatio, Is.EqualTo(1.0f));
    }

    [Test]
    public void RenderResolutionRatioTest()
    {
        GameSettings settings = GameSettings.Create(100, 100, false, 50, 5, 10, BattleSpeed.Normal, null, true, MovementSpeed.Normal);

        Assert.That(settings.RenderResolutionRatio, Is.EqualTo(0.5f));
    }

    [Test]
    public void FullRenderResolutionRatioTest()
    {
        Assert.That(GameSettings.Default.RenderResolutionRatio, Is.EqualTo(1.0f));
    }

    [Test]
    public void BattleSpeedFactorTest()
    {
        GameSettings normal = GameSettings.Create(100, 100, false, 100, 5, 10, BattleSpeed.Normal, null, true, MovementSpeed.Normal);
        GameSettings fast = GameSettings.Create(100, 100, false, 100, 5, 10, BattleSpeed.Fast, null, true, MovementSpeed.Normal);

        Assert.That(normal.BattleSpeedFactor, Is.EqualTo(1.2));
        Assert.That(fast.BattleSpeedFactor, Is.EqualTo(1.5));
    }

    [Test]
    public void MovementSpeedFactorTest()
    {
        GameSettings normal = GameSettings.Create(100, 100, false, 100, 5, 10, BattleSpeed.Normal, null, true, MovementSpeed.Normal);
        GameSettings fast = GameSettings.Create(100, 100, false, 100, 5, 10, BattleSpeed.Normal, null, true, MovementSpeed.Fast);

        Assert.That(normal.MovementSpeedFactor, Is.EqualTo(1.0));
        Assert.That(fast.MovementSpeedFactor, Is.EqualTo(1.3));
    }

    [Test]
    public void UiScaleRatioTest()
    {
        GameSettings settings = GameSettings.Create(100, 100, false, 100, 5, 10, BattleSpeed.Normal, 150, true, MovementSpeed.Normal);

        Assert.That(settings.UiScaleRatio, Is.EqualTo(1.5));
    }

    [Test]
    public void AutomaticUiScaleRatioTest()
    {
        Assert.That(GameSettings.Default.UiScaleRatio, Is.Null);
    }
}
