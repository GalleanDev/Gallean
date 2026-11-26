using Gallean.Tests.Common;

namespace Gallean.Configuration.Tests;

public sealed class CustomScenarioStoreTests
{
    private static IReadOnlyList<string> FormatFiles(IReadOnlyList<CustomScenarioFile> files)
    {
        List<string> rows = [];

        foreach (CustomScenarioFile file in files)
        {
            rows.Add($"{file.Id} {file.Type} {Convert.ToHexStringLower(file.Data)}");
        }

        return rows;
    }

    [Test]
    public async Task SaveEnumerateTest()
    {
        using Sandbox sb = Sandbox.Create();

        CustomScenarioStore store = new(sb.GetPath("scenarios"));

        Assert.That(await store.EnumerateAsync(CustomScenarioType.Scenario), Is.Empty);
        Assert.That(await store.EnumerateAsync(CustomScenarioType.Campaign), Is.Empty);

        await store.SaveAsync(CustomScenarioFile.Create(CustomScenarioType.Campaign, [4, 5, 6]));
        await store.SaveAsync(CustomScenarioFile.Create(CustomScenarioType.Scenario, [7, 8, 9]));
        await store.SaveAsync(CustomScenarioFile.Create(CustomScenarioType.Scenario, [1, 2, 3]));

        Assert.That(FormatFiles(await store.EnumerateAsync(CustomScenarioType.Scenario)), Is.EqualTo([
            "039058c6f2c0cb492c533b0a4d14ef77cc0f78abccced5287d84a1a2011cfb81 Scenario 010203",
            "66a6757151f8ee55db127716c7e3dce0be8074b64e20eda542e5c1e46ca9c41e Scenario 070809"
        ]));
        Assert.That(FormatFiles(await store.EnumerateAsync(CustomScenarioType.Campaign)), Is.EqualTo([
            "787c798e39a5bc1910355bae6d0cd87a36b2e10fd0202a83e3bb6b005da83472 Campaign 040506"
        ]));
        Assert.That(sb.GetTree(), Is.EquivalentTo([
            "scenarios/",
            "scenarios/039058c6f2c0cb492c533b0a4d14ef77cc0f78abccced5287d84a1a2011cfb81.sg",
            "scenarios/66a6757151f8ee55db127716c7e3dce0be8074b64e20eda542e5c1e46ca9c41e.sg",
            "scenarios/787c798e39a5bc1910355bae6d0cd87a36b2e10fd0202a83e3bb6b005da83472.csg"
        ]));
    }

    [Test]
    public async Task SaveTwiceTest()
    {
        using Sandbox sb = Sandbox.Create();

        CustomScenarioStore store = new(sb.RootPath);

        await store.SaveAsync(CustomScenarioFile.Create(CustomScenarioType.Scenario, [1, 2, 3]));
        await store.SaveAsync(CustomScenarioFile.Create(CustomScenarioType.Scenario, [1, 2, 3]));

        Assert.That(sb.GetTree(), Is.EquivalentTo([
            "039058c6f2c0cb492c533b0a4d14ef77cc0f78abccced5287d84a1a2011cfb81.sg"
        ]));
    }

    [Test]
    public async Task LoadTest()
    {
        using Sandbox sb = Sandbox.Create();

        CustomScenarioStore store = new(sb.RootPath);

        await store.SaveAsync(CustomScenarioFile.Create(CustomScenarioType.Campaign, [4, 5, 6]));

        CustomScenarioFile file = Assert.NotNull(await store.TryLoadAsync(
            new CustomScenarioId("787c798e39a5bc1910355bae6d0cd87a36b2e10fd0202a83e3bb6b005da83472")));

        Assert.That(FormatFiles([file]), Is.EqualTo([
            "787c798e39a5bc1910355bae6d0cd87a36b2e10fd0202a83e3bb6b005da83472 Campaign 040506"
        ]));
    }

    [Test]
    public async Task LoadMissingTest()
    {
        using Sandbox sb = Sandbox.Create();

        CustomScenarioStore store = new(sb.GetPath("scenarios"));

        Assert.That(
            await store.TryLoadAsync(new CustomScenarioId("787c798e39a5bc1910355bae6d0cd87a36b2e10fd0202a83e3bb6b005da83472")),
            Is.Null);
    }

    [Test]
    public async Task DeleteTest()
    {
        using Sandbox sb = Sandbox.Create();

        CustomScenarioStore store = new(sb.RootPath);

        await store.SaveAsync(CustomScenarioFile.Create(CustomScenarioType.Campaign, [4, 5, 6]));
        await store.SaveAsync(CustomScenarioFile.Create(CustomScenarioType.Scenario, [1, 2, 3]));

        await store.DeleteAsync(new CustomScenarioId("039058c6f2c0cb492c533b0a4d14ef77cc0f78abccced5287d84a1a2011cfb81"));

        Assert.That(sb.GetTree(), Is.EquivalentTo([
            "787c798e39a5bc1910355bae6d0cd87a36b2e10fd0202a83e3bb6b005da83472.csg"
        ]));
    }

    [Test]
    public async Task EnumerateForeignFilesTest()
    {
        using Sandbox sb = Sandbox.Create();

        sb.WriteTextFile("Tutorial.sg", "");
        sb.WriteTextFile("787c798e39a5bc1910355bae6d0cd87a36b2e10fd0202a83e3bb6b005da83472.txt", "");
        sb.WriteTextFile("787c798e39a5bc1910355bae6d0cd87a36b2e10fd0202a83e3bb6b005da83472.csg.tmp", "");

        CustomScenarioStore store = new(sb.RootPath);

        Assert.That(await store.EnumerateAsync(CustomScenarioType.Scenario), Is.Empty);
        Assert.That(await store.EnumerateAsync(CustomScenarioType.Campaign), Is.Empty);
    }
}
