namespace Gallean.Client.Tests;

public sealed class ScenarioFormatterTests
{
    [Test]
    public void ParagraphBreaksTest()
    {
        string actual = ScenarioFormatter.FormatBriefing("First.\n\n\nSecond.\n\n\n\nThird.\n\nFourth.");

        Assert.That(actual, Is.EqualTo("First.\n\nSecond.\n\nThird.\n\nFourth."));
    }

    [Test]
    public void WrappedLineBreakTest()
    {
        string actual = ScenarioFormatter.FormatBriefing("From this audacious decadence,\nnumerous sects were born.");

        Assert.That(actual, Is.EqualTo("From this audacious decadence, numerous sects were born."));
    }

    [Test]
    public void LineBreakTest()
    {
        string actual = ScenarioFormatter.FormatBriefing("Empire: Own Willisboard.\nLegions of the Damned: Own Hymnbeth.");

        Assert.That(actual, Is.EqualTo("Empire: Own Willisboard.\nLegions of the Damned: Own Hymnbeth."));
    }

    [Test]
    public void EllipsisTest()
    {
        string actual = ScenarioFormatter.FormatBriefing("Justice was no longer a luxury all could afford...\n\n\n\nHubert led the Empire to ruin... indulging in feasts.");

        Assert.That(actual, Is.EqualTo("Justice was no longer a luxury all could afford…\n\nHubert led the Empire to ruin… indulging in feasts."));
    }

    [Test]
    public void ObjectiveLabelTest()
    {
        string actual = ScenarioFormatter.FormatObjective("Scenario objective: Transform 60% of the terrain. ");

        Assert.That(actual, Is.EqualTo("Transform 60% of the terrain."));
    }

    [Test]
    public void ObjectiveLabelWithoutColonTest()
    {
        string actual = ScenarioFormatter.FormatObjective("Scenario objective Capture and hold the four Greenskin cities.");

        Assert.That(actual, Is.EqualTo("Capture and hold the four Greenskin cities."));
    }

    [Test]
    public void DefinedObjectiveLabelTest()
    {
        string actual = ScenarioFormatter.FormatObjective("Scenario objective defined: Capture the following cities.\n(1) Asmara City");

        Assert.That(actual, Is.EqualTo("Capture the following cities.\n(1) Asmara City"));
    }

    [Test]
    public void NewObjectiveLabelTest()
    {
        string actual = ScenarioFormatter.FormatObjective("NEW OBJECTIVE: \nDefend the fountain against invaders.");

        Assert.That(actual, Is.EqualTo("Defend the fountain against invaders."));
    }

    [Test]
    public void RepeatedObjectiveLabelTest()
    {
        string actual = ScenarioFormatter.FormatObjective("Objective: Destroy Tairenn Gull.\n\nObjective: Destroy all Empire Generals.");

        Assert.That(actual, Is.EqualTo("Destroy Tairenn Gull.\n\nDestroy all Empire Generals."));
    }

    [Test]
    public void MissingObjectiveTest()
    {
        string actual = ScenarioFormatter.FormatObjective("No scenario objective defined");

        Assert.That(actual, Is.EqualTo("No scenario objective defined"));
    }

    [Test]
    public void UnlabeledObjectiveTest()
    {
        string actual = ScenarioFormatter.FormatObjective("Destroy Hubert de Lalye");

        Assert.That(actual, Is.EqualTo("Destroy Hubert de Lalye"));
    }
}
