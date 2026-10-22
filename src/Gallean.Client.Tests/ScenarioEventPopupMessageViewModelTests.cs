namespace Gallean.Client.Tests;

public sealed class ScenarioEventPopupMessageViewModelTests
{
    [Test]
    public void ShortNarrationTest()
    {
        ScenarioEventPopupMessageViewModel message = new(new ScenarioEventPopupMessage([new ScenarioTextSpan("We shall no longer be bothered by these shadows. Their lair has been destroyed.", ScenarioTextSpanType.Text)], "", PortraitKey: null, IsPortraitLeft: true));

        Assert.That(message.IsNarration, Is.True);
        Assert.That(message.IsShortNarration, Is.True);
    }

    [Test]
    public void LongNarrationTest()
    {
        ScenarioEventPopupMessageViewModel message = new(new ScenarioEventPopupMessage([new ScenarioTextSpan("This is the Dwarven frontline. It is too dangerous at this point to enter the area.", ScenarioTextSpanType.Text)], "", PortraitKey: null, IsPortraitLeft: true));

        Assert.That(message.IsNarration, Is.True);
        Assert.That(message.IsShortNarration, Is.False);
    }

    [Test]
    public void SpeakerNarrationTest()
    {
        ScenarioEventPopupMessageViewModel message = new(new ScenarioEventPopupMessage([new ScenarioTextSpan("The Eye!", ScenarioTextSpanType.Text)], "Advisor", PortraitKey: null, IsPortraitLeft: true));

        Assert.That(message.IsNarration, Is.True);
        Assert.That(message.IsShortNarration, Is.True);
    }

    [Test]
    public void PortraitTest()
    {
        ScenarioEventPopupMessageViewModel message = new(new ScenarioEventPopupMessage([new ScenarioTextSpan("Well met.", ScenarioTextSpanType.Text)], "Scout", new AssetSpriteKey.UnitEventPortrait(new ScenarioEventPortraitId("speaker")), IsPortraitLeft: true));

        Assert.That(message.IsNarration, Is.False);
        Assert.That(message.IsShortNarration, Is.False);
    }
}
