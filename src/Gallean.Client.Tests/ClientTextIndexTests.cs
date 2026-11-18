namespace Gallean.Client.Tests;

public sealed class ClientTextIndexTests
{
    [Test]
    public void SettingsTest()
    {
        IReadOnlyList<string> settings = ClientTextIndex.All
            .Where(entry => entry.File == "Screens/Settings/SettingsScreenView.axaml")
            .Select(entry => entry.Text)
            .ToList();

        Assert.That(settings, Is.EquivalentTo([
            "Autosave limit",
            "Autosave interval",
            "Battle position",
            "Battle speed",
            "Close",
            "Display",
            "Sound effects",
            "Game",
            "Interface size",
            "Language",
            "Lock cursor to window",
            "Movement speed",
            "Music",
            "Render resolution",
            "Settings",
            "Sound and music",
            "The side of the battlefield your units stand on.",
            "Windowed mode (F11)"
        ]));
    }

    [Test]
    public void EntriesTest()
    {
        foreach (ClientTextEntry entry in ClientTextIndex.All)
        {
            Assert.That(entry.Text, Is.Not.Empty);
            Assert.That(entry.File, Is.Not.Empty);
            Assert.That(entry.Line, Is.GreaterThan(0));
        }
    }
}
