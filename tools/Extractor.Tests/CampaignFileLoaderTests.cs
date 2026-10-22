namespace Extractor.Tests;

public sealed class CampaignFileLoaderTests
{
    private static byte[] BuildCampaign()
    {
        return TestCampaignArchiveBuilder.Build("The Elven Crusade", "Emanuel Wall", new Db.Id("G000RR0005"), [
            ("TUTORIAL.SG", TestScenarios.TutorialPath),
            ("DORAGONREALM.SG", TestScenarios.DoragonRealmPath)
        ]);
    }

    [Test]
    public void LoadHeaderTest()
    {
        CampaignFile campaign = CampaignFileLoader.Load(BuildCampaign(), TestScenarios.Encoding);

        Assert.That(campaign.Header, Is.EqualTo(new CampaignFileHeader(
            "The Elven Crusade",
            "Emanuel Wall",
            new Db.Id("G000RR0005"),
            false)));
    }

    [Test]
    public void LoadSavedGameTest()
    {
        byte[] data = TestCampaignArchiveBuilder.BuildArchive([
            ("HEADER.CPN", TestCampaignArchiveBuilder.BuildHeader(
                "The Elven Crusade",
                "Emanuel Wall",
                new Db.Id("G000RR0005"),
                hasSavedGame: true)),
            ("ORDER.CPN", TestCampaignArchiveBuilder.BuildOrder([]))
        ]);

        CampaignFile campaign = CampaignFileLoader.Load(data, TestScenarios.Encoding);

        Assert.That(campaign.Header, Is.EqualTo(new CampaignFileHeader(
            "The Elven Crusade",
            "Emanuel Wall",
            new Db.Id("G000RR0005"),
            true)));
    }

    [Test]
    public void LoadScenarioOrderTest()
    {
        CampaignFile campaign = CampaignFileLoader.Load(BuildCampaign(), TestScenarios.Encoding);

        Assert.That(campaign.Scenarios.Select(scenario => scenario.FileName), Is.EqualTo([
            "TUTORIAL.SG",
            "DORAGONREALM.SG"
        ]));
    }

    [Test]
    public void LoadScenariosTest()
    {
        CampaignFile campaign = CampaignFileLoader.Load(BuildCampaign(), TestScenarios.Encoding);

        Assert.That(campaign.Scenarios.Select(scenario => scenario.Scenario.ScenarioId), Is.EqualTo([
            new Db.ScenarioId("S001SC0000"),
            new Db.ScenarioId("S143SC0000")
        ]));
    }

    [Test]
    public void LoadUnorderedScenarioTest()
    {
        byte[] data = TestCampaignArchiveBuilder.BuildArchive([
            ("HEADER.CPN", TestCampaignArchiveBuilder.BuildHeader(
                "The Elven Crusade",
                "Emanuel Wall",
                new Db.Id("G000RR0005"),
                hasSavedGame: false)),
            ("ORDER.CPN", TestCampaignArchiveBuilder.BuildOrder([])),
            ("DORAGONREALM.SG", File.ReadAllBytes(TestScenarios.DoragonRealmPath))
        ]);

        CampaignFile campaign = CampaignFileLoader.Load(data, TestScenarios.Encoding);

        Assert.That(campaign.Scenarios, Is.Empty);
    }

    [Test]
    public void LoadMissingOrderedScenarioTest()
    {
        byte[] data = TestCampaignArchiveBuilder.BuildArchive([
            ("HEADER.CPN", TestCampaignArchiveBuilder.BuildHeader(
                "The Elven Crusade",
                "Emanuel Wall",
                new Db.Id("G000RR0005"),
                hasSavedGame: false)),
            ("ORDER.CPN", TestCampaignArchiveBuilder.BuildOrder(["MISSING.SG"]))
        ]);

        Assert.That(
            () => CampaignFileLoader.Load(data, TestScenarios.Encoding),
            Throws.TypeOf<InvalidDataException>());
    }

    [Test]
    public void LoadShortHeaderTest()
    {
        byte[] data = TestCampaignArchiveBuilder.BuildArchive([
            ("HEADER.CPN", new byte[64]),
            ("ORDER.CPN", TestCampaignArchiveBuilder.BuildOrder([]))
        ]);

        Assert.That(
            () => CampaignFileLoader.Load(data, TestScenarios.Encoding),
            Throws.TypeOf<InvalidDataException>());
    }

    [Test]
    public void LoadUnknownArchiveTest()
    {
        Assert.That(
            () => CampaignFileLoader.Load(new byte[64], TestScenarios.Encoding),
            Throws.TypeOf<InvalidDataException>());
    }
}
