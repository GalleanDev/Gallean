using System.IO.Compression;
using Gallean.Tests.Common;

namespace Gallean.Client.Tests;

public sealed class UpdateInstallerTests
{
    [Test]
    public void InstallTest()
    {
        using Sandbox sb = Sandbox.Create();

        sb.CreateDirectory("game/RiseOfTheElves");
        sb.WriteTextFile("game/Gallean", "client 1");
        sb.WriteTextFile("game/RiseOfTheElves/GAI.json", "ai 1");
        sb.WriteTextFile("game/RiseOfTheElves/Gone.json", "gone 1");
        sb.WriteTextFile("game/base.galleanpack", "pack");

        sb.CreateDirectory("package/Gallean-2-test/RiseOfTheElves");
        sb.WriteTextFile("package/Gallean-2-test/Gallean", "client 2");
        sb.WriteTextFile("package/Gallean-2-test/RiseOfTheElves/GAI.json", "ai 2");
        ZipFile.CreateFromDirectory(sb.GetPath("package"), sb.GetPath("Gallean-2-test.zip"));

        UpdateInstaller installer = new(sb.GetPath("game"));
        installer.Install(sb.GetPath("Gallean-2-test.zip"));
        installer.Cleanup();

        Assert.That(sb.GetTree(), Is.EquivalentTo(
        [
            "Gallean-2-test.zip",
            "game/",
            "game/Gallean",
            "game/RiseOfTheElves/",
            "game/RiseOfTheElves/GAI.json",
            "game/base.galleanpack",
            "package/",
            "package/Gallean-2-test/",
            "package/Gallean-2-test/Gallean",
            "package/Gallean-2-test/RiseOfTheElves/",
            "package/Gallean-2-test/RiseOfTheElves/GAI.json"
        ]));
        Assert.That(sb.ReadTextFile("game/Gallean"), Is.EqualTo("client 2"));
        Assert.That(sb.ReadTextFile("game/RiseOfTheElves/GAI.json"), Is.EqualTo("ai 2"));
        Assert.That(sb.ReadTextFile("game/base.galleanpack"), Is.EqualTo("pack"));
    }

    [Test]
    public void InstallInterrupTest()
    {
        using Sandbox sb = Sandbox.Create();

        sb.CreateDirectory("game/.tmp/pending/RiseOfTheElves");
        sb.CreateDirectory("game/.tmp/previous/RiseOfTheElves");
        sb.WriteTextFile("game/Gallean", "client 2");
        sb.WriteTextFile("game/libSkiaSharp.so", "skia 1");
        sb.WriteTextFile("game/.tmp/pending/libSkiaSharp.so", "skia 2");
        sb.WriteTextFile("game/.tmp/pending/RiseOfTheElves/GAI.json", "ai 2");
        sb.WriteTextFile("game/.tmp/previous/Gallean", "client 1");
        sb.WriteTextFile("game/.tmp/previous/RiseOfTheElves/GAI.json", "ai 1");

        UpdateInstaller installer = new(sb.GetPath("game"));
        installer.Cleanup();

        Assert.That(sb.GetTree(), Is.EquivalentTo(
        [
            "game/",
            "game/Gallean",
            "game/RiseOfTheElves/",
            "game/RiseOfTheElves/GAI.json",
            "game/libSkiaSharp.so"
        ]));
        Assert.That(sb.ReadTextFile("game/Gallean"), Is.EqualTo("client 2"));
        Assert.That(sb.ReadTextFile("game/RiseOfTheElves/GAI.json"), Is.EqualTo("ai 2"));
        Assert.That(sb.ReadTextFile("game/libSkiaSharp.so"), Is.EqualTo("skia 2"));
    }
}
