namespace Gallean.Engine.Scenarios.Tests;

public sealed class CampaignFileLoaderTests
{
    private static byte[] BuildCampaign()
    {
        return TestCampaignArchiveBuilder.Build("The Greywater Chronicle", "Gallean Contributors", new Db.Id("G000RR0003"), [
            ("CROSSING.SG", TestScenarios.GreywaterCrossingPath),
            ("SIEGE.SG", TestScenarios.GreywaterSiegePath)
        ]);
    }

    [Test]
    public void LoadHeaderTest()
    {
        CampaignFile campaign = CampaignFileLoader.Load(BuildCampaign(), TestScenarios.Encoding);

        Assert.That(campaign.Header, Is.EqualTo(new CampaignFileHeader(
            "The Greywater Chronicle",
            "Gallean Contributors",
            new Db.Id("G000RR0003"),
            false)));
    }

    [Test]
    public void LoadSavedGameTest()
    {
        byte[] data = TestCampaignArchiveBuilder.BuildArchive([
            ("HEADER.CPN", TestCampaignArchiveBuilder.BuildHeader(
                "The Greywater Chronicle",
                "Gallean Contributors",
                new Db.Id("G000RR0003"),
                hasSavedGame: true)),
            ("ORDER.CPN", TestCampaignArchiveBuilder.BuildOrder([]))
        ]);

        CampaignFile campaign = CampaignFileLoader.Load(data, TestScenarios.Encoding);

        Assert.That(campaign.Header, Is.EqualTo(new CampaignFileHeader(
            "The Greywater Chronicle",
            "Gallean Contributors",
            new Db.Id("G000RR0003"),
            true)));
    }

    [Test]
    public void LoadScenarioOrderTest()
    {
        CampaignFile campaign = CampaignFileLoader.Load(BuildCampaign(), TestScenarios.Encoding);

        Assert.That(campaign.Scenarios.Select(scenario => scenario.FileName), Is.EqualTo([
            "CROSSING.SG",
            "SIEGE.SG"
        ]));
    }

    [Test]
    public void LoadScenariosTest()
    {
        CampaignFile campaign = CampaignFileLoader.Load(BuildCampaign(), TestScenarios.Encoding);

        Assert.That(campaign.Scenarios.Select(scenario => scenario.Scenario.ScenarioId), Is.EqualTo([
            new Db.ScenarioId("S417SC0000"),
            new Db.ScenarioId("S418SC0000")
        ]));
    }

    [Test]
    public void LoadUnorderedScenarioTest()
    {
        byte[] data = TestCampaignArchiveBuilder.BuildArchive([
            ("HEADER.CPN", TestCampaignArchiveBuilder.BuildHeader(
                "The Greywater Chronicle",
                "Gallean Contributors",
                new Db.Id("G000RR0003"),
                hasSavedGame: false)),
            ("ORDER.CPN", TestCampaignArchiveBuilder.BuildOrder([])),
            ("CROSSING.SG", File.ReadAllBytes(TestScenarios.GreywaterCrossingPath))
        ]);

        CampaignFile campaign = CampaignFileLoader.Load(data, TestScenarios.Encoding);

        Assert.That(campaign.Scenarios, Is.Empty);
    }

    [Test]
    public void LoadMissingOrderedScenarioTest()
    {
        byte[] data = TestCampaignArchiveBuilder.BuildArchive([
            ("HEADER.CPN", TestCampaignArchiveBuilder.BuildHeader(
                "The Greywater Chronicle",
                "Gallean Contributors",
                new Db.Id("G000RR0003"),
                hasSavedGame: false)),
            ("ORDER.CPN", TestCampaignArchiveBuilder.BuildOrder(["MISSING.SG"]))
        ]);

        Assert.That(
            () => CampaignFileLoader.Load(data, TestScenarios.Encoding),
            Throws.TypeOf<InvalidDataException>());
    }

    [Test]
    public void DetectEncodingTest()
    {
        byte[] data = TestCampaignArchiveBuilder.Build("\u00d5\u00f0\u00ee\u00ed\u00e8\u00ea\u00e8", "Gallean Contributors", new Db.Id("G000RR0003"), [
            ("CROSSING.SG", TestScenarios.GreywaterCrossingPath)
        ]);

        ScenarioTextEncoding encoding = CampaignFileLoader.DetectEncoding(data);
        CampaignFileContent content = CampaignFileLoader.Read(data, encoding);

        Assert.That(content.Header, Is.EqualTo(new CampaignFileHeader(
            "Хроники",
            "Gallean Contributors",
            new Db.Id("G000RR0003"),
            false)));
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
