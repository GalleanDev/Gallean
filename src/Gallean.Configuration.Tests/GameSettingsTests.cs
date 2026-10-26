namespace Gallean.Configuration.Tests;

public sealed class GameSettingsTests
{
    [Test]
    public void DefaultTest()
    {
        Assert.That(GameSettings.Default, Is.EqualTo(GameSettings.Create(100, 100, false, 100, 5, 10, BattleSpeed.Normal, null, true, MovementSpeed.Normal)));
    }

    [Test]
    public void ClampSimpleTest()
    {
        Assert.That(
            GameSettings.Create(900, -3, false, 5, 5, 10, BattleSpeed.Normal, null, true, MovementSpeed.Normal),
            Is.EqualTo(GameSettings.Create(100, 0, false, 50, 5, 10, BattleSpeed.Normal, null, true, MovementSpeed.Normal)));
    }

    [Test]
    public void ClampRenderResolutionTest()
    {
        Assert.That(
            GameSettings.Create(40, 25, false, 400, 5, 10, BattleSpeed.Normal, null, true, MovementSpeed.Normal),
            Is.EqualTo(GameSettings.Create(40, 25, false, 100, 5, 10, BattleSpeed.Normal, null, true, MovementSpeed.Normal)));
    }

    [Test]
    public void ClampAutoSaveIntervalTest()
    {
        Assert.That(
            GameSettings.Create(40, 25, false, 100, -4, 10, BattleSpeed.Normal, null, true, MovementSpeed.Normal),
            Is.EqualTo(GameSettings.Create(40, 25, false, 100, 0, 10, BattleSpeed.Normal, null, true, MovementSpeed.Normal)));
    }

    [Test]
    public void ClampAutoSaveLimitTest()
    {
        Assert.That(
            GameSettings.Create(40, 25, false, 100, 5, 0, BattleSpeed.Normal, null, true, MovementSpeed.Normal),
            Is.EqualTo(GameSettings.Create(40, 25, false, 100, 5, 1, BattleSpeed.Normal, null, true, MovementSpeed.Normal)));
    }

    [Test]
    public void ClampUiScaleTest()
    {
        Assert.That(
            GameSettings.Create(40, 25, false, 100, 5, 10, BattleSpeed.Normal, 400, true, MovementSpeed.Normal),
            Is.EqualTo(GameSettings.Create(40, 25, false, 100, 5, 10, BattleSpeed.Normal, 300, true, MovementSpeed.Normal)));
        Assert.That(
            GameSettings.Create(40, 25, false, 100, 5, 10, BattleSpeed.Normal, 10, true, MovementSpeed.Normal),
            Is.EqualTo(GameSettings.Create(40, 25, false, 100, 5, 10, BattleSpeed.Normal, 100, true, MovementSpeed.Normal)));
    }
}
