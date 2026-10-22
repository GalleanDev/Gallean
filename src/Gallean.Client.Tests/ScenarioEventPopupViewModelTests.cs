namespace Gallean.Client.Tests;

public sealed class ScenarioEventPopupViewModelTests
{
    private static ScenarioEventPopupMessage MakeMessage(string text, bool isPortraitLeft)
    {
        return new ScenarioEventPopupMessage(
            [new ScenarioTextSpan(text, ScenarioTextSpanType.Text)],
            $"Speaker of {text}",
            new AssetSpriteKey.UnitEventPortrait(new ScenarioEventPortraitId(text)),
            isPortraitLeft);
    }

    [Test]
    public void PortraitsTest()
    {
        ScenarioEventPopupMessage first = MakeMessage("g000uu0001", isPortraitLeft: true);
        ScenarioEventPopupMessage second = MakeMessage("g000uu0002", isPortraitLeft: false);
        ScenarioEventPopupMessage third = MakeMessage("g000uu0003", isPortraitLeft: true);

        ScenarioEventPopupViewModel popup = new([first, second, third]);

        Assert.That(popup.Messages, Is.EqualTo([new ScenarioEventPopupMessageViewModel(first)]));
        Assert.That(popup.LeftPortrait.PortraitKey, Is.EqualTo(first.PortraitKey));
        Assert.That(popup.LeftPortrait.IsActive, Is.True);
        Assert.That(popup.RightPortrait.PortraitKey, Is.EqualTo(second.PortraitKey));
        Assert.That(popup.RightPortrait.IsActive, Is.False);

        popup.CloseCommand.Execute(null);

        Assert.That(popup.Messages, Is.EqualTo([new ScenarioEventPopupMessageViewModel(first), new ScenarioEventPopupMessageViewModel(second)]));
        Assert.That(popup.LeftPortrait.IsActive, Is.False);
        Assert.That(popup.RightPortrait.IsActive, Is.True);

        popup.CloseCommand.Execute(null);

        Assert.That(popup.Messages, Is.EqualTo([new ScenarioEventPopupMessageViewModel(first), new ScenarioEventPopupMessageViewModel(second), new ScenarioEventPopupMessageViewModel(third)]));
        Assert.That(popup.LeftPortrait.PortraitKey, Is.EqualTo(third.PortraitKey));
        Assert.That(popup.LeftPortrait.IsActive, Is.True);
        Assert.That(popup.RightPortrait.IsActive, Is.False);
    }

    [Test]
    public void SingleTest()
    {
        ScenarioEventPopupMessage first = MakeMessage("g000uu0001", isPortraitLeft: true);
        ScenarioEventPopupMessage second = MakeMessage("g000uu0002", isPortraitLeft: true);

        ScenarioEventPopupViewModel popup = new([first, second]);

        Assert.That(popup.LeftPortrait.PortraitKey, Is.EqualTo(first.PortraitKey));
        Assert.That(popup.LeftPortrait.IsActive, Is.True);
        Assert.That(popup.RightPortrait.PortraitKey, Is.Null);
        Assert.That(popup.RightPortrait.IsActive, Is.False);
    }

    [Test]
    public void NarrationTest()
    {
        ScenarioEventPopupMessage first = MakeMessage("g000uu0001", isPortraitLeft: true);
        ScenarioEventPopupMessage narration = new([new ScenarioTextSpan("THE SIEGE OF TEMPERANCE", ScenarioTextSpanType.Text)], "", PortraitKey: null, IsPortraitLeft: true);

        ScenarioEventPopupViewModel popup = new([first, narration]);

        Assert.That(popup.LeftPortrait.PortraitKey, Is.EqualTo(first.PortraitKey));
        Assert.That(popup.LeftPortrait.IsActive, Is.True);
        Assert.That(popup.RightPortrait.PortraitKey, Is.Null);

        popup.CloseCommand.Execute(null);

        Assert.That(popup.Messages, Is.EqualTo([new ScenarioEventPopupMessageViewModel(first), new ScenarioEventPopupMessageViewModel(narration)]));
        Assert.That(popup.LeftPortrait.PortraitKey, Is.EqualTo(first.PortraitKey));
        Assert.That(popup.LeftPortrait.IsActive, Is.False);
        Assert.That(popup.RightPortrait.PortraitKey, Is.Null);
        Assert.That(popup.RightPortrait.IsActive, Is.False);
    }

    [Test]
    public void NarrationOnlyTest()
    {
        ScenarioEventPopupMessage narration = new([new ScenarioTextSpan("THE SIEGE OF TEMPERANCE", ScenarioTextSpanType.Text)], "", PortraitKey: null, IsPortraitLeft: true);

        ScenarioEventPopupViewModel popup = new([narration]);

        Assert.That(popup.Messages, Is.EqualTo([new ScenarioEventPopupMessageViewModel(narration)]));
        Assert.That(popup.LeftPortrait.PortraitKey, Is.Null);
        Assert.That(popup.LeftPortrait.IsActive, Is.False);
        Assert.That(popup.RightPortrait.PortraitKey, Is.Null);
        Assert.That(popup.RightPortrait.IsActive, Is.False);
    }
}
