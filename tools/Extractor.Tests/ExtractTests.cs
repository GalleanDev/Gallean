using Gallean.Tests.Common;
using GameContent;

namespace Extractor.Tests;

[NonParallelizable]
public sealed class ExtractTests
{
    private static string GamePath => GameFiles.Original.Root;

    [Test]
    [Explicit]
    public async Task ExtractDatabaseTest()
    {
        using Sandbox sb = Sandbox.Create();

        await TestContext.Progress.WriteLineAsync($"Extracting database from '{GamePath}'.");
        await TestContext.Progress.WriteLineAsync($"Outputting files to '{sb.RootPath}'.");

        await ExtractDatabaseCommand.ExecuteAsync(GamePath, sb.RootPath);
    }

    [Test]
    [Explicit]
    public async Task ExtractImagesTest()
    {
        using Sandbox sb = Sandbox.Create();

        await TestContext.Progress.WriteLineAsync($"Extracting images from '{GamePath}'.");
        await TestContext.Progress.WriteLineAsync($"Outputting files to '{sb.RootPath}'.");

        await ExtractImagesCommand.ExecuteAsync(GamePath, sb.RootPath);
    }

    [Test]
    [Explicit]
    public async Task ExtractInterfaceTest()
    {
        using Sandbox sb = Sandbox.Create();

        await TestContext.Progress.WriteLineAsync($"Extracting interface from '{GamePath}'.");
        await TestContext.Progress.WriteLineAsync($"Outputting files to '{sb.RootPath}'.");

        await ExtractInterfaceCommand.ExecuteAsync(GamePath, sb.RootPath);
    }

    [Test]
    [Explicit]
    public async Task ExtractSoundsTest()
    {
        using Sandbox sb = Sandbox.Create();

        await TestContext.Progress.WriteLineAsync($"Extracting sounds from '{GamePath}'.");
        await TestContext.Progress.WriteLineAsync($"Outputting files to '{sb.RootPath}'.");

        await ExtractSoundsCommand.ExecuteAsync(GamePath, sb.RootPath);
    }
}
