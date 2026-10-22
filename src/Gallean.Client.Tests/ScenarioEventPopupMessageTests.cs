using Gallean.Common;
using Gallean.Engine;
using Gallean.Engine.Tests;

namespace Gallean.Client.Tests;

public sealed class ScenarioEventPopupMessageTests
{
    private static ScenarioEventPopupMessage Create(
        string text,
        ScenarioEventPortraitId? portraitId,
        string? portraitName,
        IReadOnlyList<ScenarioName> names)
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleUnitDefinition("speaker", maxHitpoints: 100, initiative: 50, damage: 10))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        WorldScenarioPopupEvent popup = new(
            new ScenarioEventId("Event1"),
            new ScenarioTextId(text),
            portraitId,
            portraitName,
            IsPortraitLeft: true,
            player.Id,
            TriggererPosition: null,
            [player.Id]);

        ScenarioTextFormatter formatter = new(names);

        return ScenarioEventPopupMessage.Create(TestGameTextProvider.Instance, session.EngineContext.Definitions, formatter, popup);
    }

    [Test]
    public void SpeakerTest()
    {
        Assert.That(
            Create("Tora'ach \n\"Hah! Mindless beast. You want food, do you?\"", portraitId: null, portraitName: null, names: []),
            Is.EqualTo(new ScenarioEventPopupMessage([new ScenarioTextSpan("Hah! Mindless beast. You want food, do you?", ScenarioTextSpanType.Text)], "Tora'ach", PortraitKey: null, IsPortraitLeft: true)));
    }

    [Test]
    public void CarriageReturnTest()
    {
        Assert.That(
            Create("Loremaster\r\"As you wish.\"", portraitId: null, portraitName: null, names: []),
            Is.EqualTo(new ScenarioEventPopupMessage([new ScenarioTextSpan("As you wish.", ScenarioTextSpanType.Text)], "Loremaster", PortraitKey: null, IsPortraitLeft: true)));
    }

    [Test]
    public void InnerQuotesTest()
    {
        Assert.That(
            Create("Scout \n\"They call themselves the \"Occultists\". They worship the Undead!\"", portraitId: null, portraitName: null, names: []),
            Is.EqualTo(new ScenarioEventPopupMessage([new ScenarioTextSpan("They call themselves the \"Occultists\". They worship the Undead!", ScenarioTextSpanType.Text)], "Scout", PortraitKey: null, IsPortraitLeft: true)));
    }

    [Test]
    public void UnclosedQuoteTest()
    {
        Assert.That(
            Create("Ronon \n\"The information you seek is in this library.", portraitId: null, portraitName: null, names: []),
            Is.EqualTo(new ScenarioEventPopupMessage([new ScenarioTextSpan("The information you seek is in this library.", ScenarioTextSpanType.Text)], "Ronon", PortraitKey: null, IsPortraitLeft: true)));
    }

    [Test]
    public void QuoteOnlyTest()
    {
        Assert.That(
            Create("\"The Orcs slaughtered the Humans who used to live here.\"", portraitId: null, portraitName: null, names: []),
            Is.EqualTo(new ScenarioEventPopupMessage([new ScenarioTextSpan("The Orcs slaughtered the Humans who used to live here.", ScenarioTextSpanType.Text)], "", PortraitKey: null, IsPortraitLeft: true)));
    }

    [Test]
    public void NarrationTest()
    {
        Assert.That(
            Create("A Thug advances from the shadows. ", portraitId: null, portraitName: null, names: []),
            Is.EqualTo(new ScenarioEventPopupMessage([new ScenarioTextSpan("A Thug advances from the shadows.", ScenarioTextSpanType.Text)], "", PortraitKey: null, IsPortraitLeft: true)));
    }

    [Test]
    public void NarrationBeforeSpeakerTest()
    {
        Assert.That(
            Create("A phantom appears before you... \n \nSpecter \n \"All who enter shall feel her power.\"", portraitId: null, portraitName: null, names: []),
            Is.EqualTo(new ScenarioEventPopupMessage([new ScenarioTextSpan("A phantom appears before you... \n \nSpecter \n \"All who enter shall feel her power.\"", ScenarioTextSpanType.Text)], "", PortraitKey: null, IsPortraitLeft: true)));
    }

    [Test]
    public void ImagePortraitTest()
    {
        Assert.That(
            Create("\"Well met.\"", new ScenarioEventPortraitId("speaker"), portraitName: null, names: []),
            Is.EqualTo(new ScenarioEventPopupMessage([new ScenarioTextSpan("Well met.", ScenarioTextSpanType.Text)], "name", new AssetSpriteKey.UnitEventPortrait(new ScenarioEventPortraitId("speaker")), IsPortraitLeft: true)));
    }

    [Test]
    public void NamedImagePortraitTest()
    {
        Assert.That(
            Create("\"Well met.\"", new ScenarioEventPortraitId("FYRWENN"), portraitName: null, names: []),
            Is.EqualTo(new ScenarioEventPopupMessage([new ScenarioTextSpan("Well met.", ScenarioTextSpanType.Text)], "", new AssetSpriteKey.UnitEventPortrait(new ScenarioEventPortraitId("FYRWENN")), IsPortraitLeft: true)));
    }

    [Test]
    public void LeaderNameTest()
    {
        Assert.That(
            Create("\"Well met.\"", new ScenarioEventPortraitId("speaker"), "Konrad", names: []),
            Is.EqualTo(new ScenarioEventPopupMessage([new ScenarioTextSpan("Well met.", ScenarioTextSpanType.Text)], "Konrad", new AssetSpriteKey.UnitEventPortrait(new ScenarioEventPortraitId("speaker")), IsPortraitLeft: true)));
    }

    [Test]
    public void TextSpeakerOverLeaderNameTest()
    {
        Assert.That(
            Create("Scout \n\"Well met.\"", new ScenarioEventPortraitId("speaker"), "Konrad", names: []),
            Is.EqualTo(new ScenarioEventPopupMessage([new ScenarioTextSpan("Well met.", ScenarioTextSpanType.Text)], "Scout", new AssetSpriteKey.UnitEventPortrait(new ScenarioEventPortraitId("speaker")), IsPortraitLeft: true)));
    }

    [Test]
    public void NamesTest()
    {
        Assert.That(
            Create(
                "Illumielle \n\"Tora'ach, return to Hearth's Grove before the Empire hears of it.\"",
                portraitId: null,
                portraitName: null,
                [
                    new ScenarioName("Tora'ach", ScenarioNameType.Character),
                    new ScenarioName("Hearth's Grove", ScenarioNameType.Place),
                    new ScenarioName("Empire", ScenarioNameType.Faction),
                    new ScenarioName("Illumielle", ScenarioNameType.Character)
                ]),
            Is.EqualTo(new ScenarioEventPopupMessage(
                [
                    new ScenarioTextSpan("Tora'ach", ScenarioTextSpanType.Character),
                    new ScenarioTextSpan(", return to ", ScenarioTextSpanType.Text),
                    new ScenarioTextSpan("Hearth's Grove", ScenarioTextSpanType.Place),
                    new ScenarioTextSpan(" before the ", ScenarioTextSpanType.Text),
                    new ScenarioTextSpan("Empire", ScenarioTextSpanType.Faction),
                    new ScenarioTextSpan(" hears of it.", ScenarioTextSpanType.Text)
                ],
                "Illumielle",
                PortraitKey: null,
                IsPortraitLeft: true)));
    }

    [Test]
    public void LongestNameTest()
    {
        Assert.That(
            Create(
                "\"Lachla'an rests in Lachla'an's Tomb.\"",
                portraitId: null,
                portraitName: null,
                [
                    new ScenarioName("Lachla'an", ScenarioNameType.Character),
                    new ScenarioName("Lachla'an's Tomb", ScenarioNameType.Place)
                ]),
            Is.EqualTo(new ScenarioEventPopupMessage(
                [
                    new ScenarioTextSpan("Lachla'an", ScenarioTextSpanType.Character),
                    new ScenarioTextSpan(" rests in ", ScenarioTextSpanType.Text),
                    new ScenarioTextSpan("Lachla'an's Tomb", ScenarioTextSpanType.Place),
                    new ScenarioTextSpan(".", ScenarioTextSpanType.Text)
                ],
                "",
                PortraitKey: null,
                IsPortraitLeft: true)));
    }

    [Test]
    public void WholeWordTest()
    {
        Assert.That(
            Create(
                "\"Orcs fear the Orc, not the Orcish horde, nor Gallean's wrath.\"",
                portraitId: null,
                portraitName: null,
                [
                    new ScenarioName("Orc", ScenarioNameType.Character),
                    new ScenarioName("Gallean", ScenarioNameType.Character)
                ]),
            Is.EqualTo(new ScenarioEventPopupMessage(
                [
                    new ScenarioTextSpan("Orcs fear the ", ScenarioTextSpanType.Text),
                    new ScenarioTextSpan("Orc", ScenarioTextSpanType.Character),
                    new ScenarioTextSpan(", not the Orcish horde, nor ", ScenarioTextSpanType.Text),
                    new ScenarioTextSpan("Gallean", ScenarioTextSpanType.Character),
                    new ScenarioTextSpan("'s wrath.", ScenarioTextSpanType.Text)
                ],
                "",
                PortraitKey: null,
                IsPortraitLeft: true)));
    }

    [Test]
    public void CaseTest()
    {
        Assert.That(
            Create(
                "\"The Legions Of The Damned guard Lachla'an's tomb, and the legions of the damned never sleep.\"",
                portraitId: null,
                portraitName: null,
                [
                    new ScenarioName("Legions of the Damned", ScenarioNameType.Faction),
                    new ScenarioName("Lachla'an's Tomb", ScenarioNameType.Place)
                ]),
            Is.EqualTo(new ScenarioEventPopupMessage(
                [
                    new ScenarioTextSpan("The ", ScenarioTextSpanType.Text),
                    new ScenarioTextSpan("Legions Of The Damned", ScenarioTextSpanType.Faction),
                    new ScenarioTextSpan(" guard ", ScenarioTextSpanType.Text),
                    new ScenarioTextSpan("Lachla'an's tomb", ScenarioTextSpanType.Place),
                    new ScenarioTextSpan(", and the legions of the damned never sleep.", ScenarioTextSpanType.Text)
                ],
                "",
                PortraitKey: null,
                IsPortraitLeft: true)));
    }
}
