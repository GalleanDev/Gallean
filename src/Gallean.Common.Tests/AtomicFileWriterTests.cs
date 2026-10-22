using Gallean.Tests.Common;

namespace Gallean.Common.Tests;

public sealed class AtomicFileWriterTests
{
    [Test]
    public void CommitTest()
    {
        using Sandbox sb = Sandbox.Create();

        string targetPath = sb.GetPath("data.txt");

        using (AtomicFileWriter writer = new(targetPath))
        {
            writer.Stream.Write("committed"u8);

            Assert.That(sb.GetTree(), Is.EquivalentTo(["data.txt.tmp"]));

            writer.Commit();

            Assert.That(sb.GetTree(), Is.EquivalentTo(["data.txt"]));
        }

        Assert.That(sb.GetTree(), Is.EquivalentTo(["data.txt"]));
        Assert.That(sb.ReadTextFile("data.txt"), Is.EqualTo("committed"));
    }

    [Test]
    public async Task CommitAsyncTest()
    {
        using Sandbox sb = Sandbox.Create();

        string targetPath = sb.GetPath("data.txt");

        using (AtomicFileWriter writer = new(targetPath, FileOptions.Asynchronous, 4096))
        {
            await writer.Stream.WriteAsync("committed"u8.ToArray());

            Assert.That(sb.GetTree(), Is.EquivalentTo(["data.txt.tmp"]));

            await writer.CommitAsync();
        }

        Assert.That(sb.GetTree(), Is.EquivalentTo(["data.txt"]));
        Assert.That(sb.ReadTextFile("data.txt"), Is.EqualTo("committed"));
    }

    [Test]
    public void FlushToDiskTest()
    {
        using Sandbox sb = Sandbox.Create();

        string targetPath = sb.GetPath("data.txt");

        using (AtomicFileWriter writer = new(targetPath, flushToDisk: true))
        {
            writer.Stream.Write("committed"u8);

            Assert.That(sb.GetTree(), Is.EquivalentTo(["data.txt.tmp"]));

            writer.Commit();
        }

        Assert.That(sb.GetTree(), Is.EquivalentTo(["data.txt"]));
        Assert.That(sb.ReadTextFile("data.txt"), Is.EqualTo("committed"));
    }

    [Test]
    public async Task FlushToDiskAsyncTest()
    {
        using Sandbox sb = Sandbox.Create();

        string targetPath = sb.GetPath("data.txt");

        using (AtomicFileWriter writer = new(targetPath, FileOptions.Asynchronous, flushToDisk: true))
        {
            await writer.Stream.WriteAsync("committed"u8.ToArray());

            Assert.That(sb.GetTree(), Is.EquivalentTo(["data.txt.tmp"]));

            await writer.CommitAsync();
        }

        Assert.That(sb.GetTree(), Is.EquivalentTo(["data.txt"]));
        Assert.That(sb.ReadTextFile("data.txt"), Is.EqualTo("committed"));
    }

    [Test]
    public void AbandonTest()
    {
        using Sandbox sb = Sandbox.Create();

        string targetPath = sb.GetPath("data.txt");

        using (AtomicFileWriter writer = new(targetPath))
        {
            writer.Stream.Write("abandoned"u8);

            Assert.That(sb.GetTree(), Is.EquivalentTo(["data.txt.tmp"]));
        }

        Assert.That(sb.GetTree(), Is.Empty);
    }

    [Test]
    public void CommitExistingTest()
    {
        using Sandbox sb = Sandbox.Create();

        string targetPath = sb.WriteTextFile("data.txt", "original");

        using (AtomicFileWriter writer = new(targetPath))
        {
            writer.Stream.Write("replaced"u8);

            Assert.That(sb.GetTree(), Is.EquivalentTo(["data.txt", "data.txt.tmp"]));
            Assert.That(sb.ReadTextFile("data.txt"), Is.EqualTo("original"));

            writer.Commit();
        }

        Assert.That(sb.GetTree(), Is.EquivalentTo(["data.txt"]));
        Assert.That(sb.ReadTextFile("data.txt"), Is.EqualTo("replaced"));
    }

    [Test]
    public void AbandonExistingTest()
    {
        using Sandbox sb = Sandbox.Create();

        string targetPath = sb.WriteTextFile("data.txt", "original");

        using (AtomicFileWriter writer = new(targetPath))
        {
            writer.Stream.Write("abandoned"u8);

            Assert.That(sb.GetTree(), Is.EquivalentTo(["data.txt", "data.txt.tmp"]));
        }

        Assert.That(sb.GetTree(), Is.EquivalentTo(["data.txt"]));
        Assert.That(sb.ReadTextFile("data.txt"), Is.EqualTo("original"));
    }

    [Test]
    public void NestedDirectoryTest()
    {
        using Sandbox sb = Sandbox.Create();

        sb.CreateDirectory("saves");

        using (AtomicFileWriter writer = new(sb.GetPath(Path.Combine("saves", "data.txt"))))
        {
            writer.Stream.Write("committed"u8);
            writer.Commit();
        }

        Assert.That(sb.GetTree(), Is.EquivalentTo(["saves/", "saves/data.txt"]));
        Assert.That(sb.ReadTextFile(Path.Combine("saves", "data.txt")), Is.EqualTo("committed"));
    }

    [Test]
    public void MissingDirectoryTest()
    {
        using Sandbox sb = Sandbox.Create();

        string targetPath = sb.GetPath(Path.Combine("saves", "data.txt"));

        Assert.That(() => new AtomicFileWriter(targetPath), Throws.InstanceOf<DirectoryNotFoundException>());
        Assert.That(sb.GetTree(), Is.Empty);
    }

    [Test]
    public async Task RepeatedCommitTest()
    {
        using Sandbox sb = Sandbox.Create();

        using AtomicFileWriter writer = new(sb.GetPath("data.txt"));
        writer.Stream.Write("committed"u8);

        writer.Commit();
        writer.Commit();
        await writer.CommitAsync();

        Assert.That(sb.GetTree(), Is.EquivalentTo(["data.txt"]));
        Assert.That(sb.ReadTextFile("data.txt"), Is.EqualTo("committed"));
    }

    [Test]
    public void DisposeAfterCommitTest()
    {
        using Sandbox sb = Sandbox.Create();

        AtomicFileWriter writer = new(sb.GetPath("data.txt"));
        writer.Stream.Write("committed"u8);
        writer.Commit();

        writer.Dispose();
        writer.Dispose();

        Assert.That(sb.GetTree(), Is.EquivalentTo(["data.txt"]));
        Assert.That(sb.ReadTextFile("data.txt"), Is.EqualTo("committed"));
    }

    [Test]
    public void DoubleDisposeTest()
    {
        using Sandbox sb = Sandbox.Create();

        AtomicFileWriter writer = new(sb.GetPath("data.txt"));
        writer.Stream.Write("abandoned"u8);

        writer.Dispose();
        writer.Dispose();

        Assert.That(sb.GetTree(), Is.Empty);
    }
}
