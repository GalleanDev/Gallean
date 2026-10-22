using System.Text.Json;
using Gallean.Tests.Common;

namespace Gallean.Entities.Tests;

public sealed class DbFileReaderTests
{
    [Test]
    public void UnsupportedFormatVersionTest()
    {
        using Sandbox sb = Sandbox.Create();

        string path = sb.WriteTextFile("CCamp.json", """{"formatVersion":0,"rows":[]}""");

        Assert.That(
            () => CampaignDatabaseLoader.Load(sb.RootPath),
            Throws.TypeOf<InvalidDataException>()
                .With.Message.EqualTo($"The data file '{path}' has unsupported format version '0'."));
    }

    [Test]
    public void InvalidTest()
    {
        using Sandbox sb = Sandbox.Create();

        string path = sb.WriteTextFile("CCamp.json", "[]");

        Assert.That(
            () => CampaignDatabaseLoader.Load(sb.RootPath),
            Throws.TypeOf<JsonException>()
                .With.Message.EqualTo($"The data file '{path}' is invalid."));
    }

    [Test]
    public void NoFormatVersionTest()
    {
        using Sandbox sb = Sandbox.Create();

        string path = sb.WriteTextFile("CCamp.json", """{"rows":[]}""");

        Assert.That(
            () => CampaignDatabaseLoader.Load(sb.RootPath),
            Throws.TypeOf<JsonException>()
                .With.Message.EqualTo($"The data file '{path}' does not specify a format version."));
    }

    [Test]
    public void InvalidFormatVersionTest()
    {
        using Sandbox sb = Sandbox.Create();

        string path = sb.WriteTextFile("CCamp.json", """{"formatVersion":"1","rows":[]}""");

        Assert.That(
            () => CampaignDatabaseLoader.Load(sb.RootPath),
            Throws.TypeOf<JsonException>()
                .With.Message.EqualTo($"The data file '{path}' has an invalid format version."));
    }

    [Test]
    public void NoDataTest()
    {
        using Sandbox sb = Sandbox.Create();

        string path = sb.WriteTextFile("CCamp.json", """{"formatVersion":1}""");

        Assert.That(
            () => CampaignDatabaseLoader.Load(sb.RootPath),
            Throws.TypeOf<JsonException>()
                .With.Message.EqualTo($"The data file '{path}' has no data."));
    }
}
