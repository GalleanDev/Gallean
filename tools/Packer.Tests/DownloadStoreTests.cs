using System.Net.Http.Headers;
using System.Security.Cryptography;
using Gallean.Tests.Common;

namespace Packer.Tests;

public sealed class DownloadStoreTests
{
    private const string Location = "https://example.test/payload.bin";
    private const string Signed = "https://cdn.example.test/signed";

    private static byte[] BuildContents(int length)
    {
        byte[] contents = new byte[length];

        for (int index = 0; index < length; index++)
        {
            contents[index] = (byte)(index % 251);
        }

        return contents;
    }

    private static DownloadDigest Sha256Of(byte[] contents)
    {
        byte[] hash = SHA256.HashData(contents);

        return new DownloadDigest(HashAlgorithmName.SHA256, Convert.ToHexStringLower(hash));
    }

    private static TestHttpMessageHandler CreateHandler(byte[] contents)
    {
        return new TestHttpMessageHandler(
            new Dictionary<string, TestHttpResource>
            {
                [Location] = new(contents, new Dictionary<string, string>(), null)
            });
    }

    private static DownloadStore CreateStore(TestHttpMessageHandler handler, Sandbox sb)
    {
        return new DownloadStore(new HttpClient(handler), new TestDownloadProgress(), sb.GetPath("downloads"));
    }

    private static RemoteFile CreateFile(byte[] contents, DownloadDigest? digest)
    {
        return new RemoteFile("payload.bin", new Uri(Location), contents.Length, digest, null);
    }

    private static void WritePartial(Sandbox sb, byte[] contents)
    {
        string directory = sb.CreateDirectory("downloads");

        File.WriteAllBytes(Path.Combine(directory, "payload.bin.partial"), contents);
    }

    [Test]
    public async Task DownloadTest()
    {
        using Sandbox sb = Sandbox.Create();

        byte[] contents = BuildContents(4096);
        TestHttpMessageHandler handler = CreateHandler(contents);

        string path = await CreateStore(handler, sb).EnsureAsync(
            CreateFile(contents, Sha256Of(contents)), CancellationToken.None);

        Assert.That(await File.ReadAllBytesAsync(path), Is.EqualTo(contents));
        Assert.That(sb.GetTree(), Is.EqualTo(["downloads/", "downloads/payload.bin"]));
        Assert.That(handler.Requests, Is.EqualTo([$"GET {Location}"]));
    }

    [Test]
    public async Task ResumeTest()
    {
        using Sandbox sb = Sandbox.Create();

        byte[] contents = BuildContents(4096);
        TestHttpMessageHandler handler = CreateHandler(contents);

        WritePartial(sb, contents[..1000]);

        string path = await CreateStore(handler, sb).EnsureAsync(
            CreateFile(contents, Sha256Of(contents)), CancellationToken.None);

        Assert.That(await File.ReadAllBytesAsync(path), Is.EqualTo(contents));
        Assert.That(sb.GetTree(), Is.EqualTo(["downloads/", "downloads/payload.bin"]));
        Assert.That(handler.Requests, Is.EqualTo([$"GET {Location}"]));
    }

    [Test]
    public async Task RedirectTest()
    {
        using Sandbox sb = Sandbox.Create();

        byte[] contents = BuildContents(4096);
        TestHttpMessageHandler handler = new(
            new Dictionary<string, TestHttpResource>
            {
                [Location] = new([], new Dictionary<string, string>(), Signed),
                [Signed] = new(contents, new Dictionary<string, string>(), null)
            });

        string path = await CreateStore(handler, sb).EnsureAsync(
            CreateFile(contents, Sha256Of(contents)), CancellationToken.None);

        Assert.That(await File.ReadAllBytesAsync(path), Is.EqualTo(contents));
        Assert.That(handler.Requests, Is.EqualTo([$"GET {Location}", $"GET {Signed}"]));
    }

    [Test]
    public async Task AuthorizedRedirectTest()
    {
        using Sandbox sb = Sandbox.Create();

        byte[] contents = BuildContents(4096);
        TestHttpMessageHandler handler = new(
            new Dictionary<string, TestHttpResource>
            {
                [Location] = new([], new Dictionary<string, string>(), Signed),
                [Signed] = new(contents, new Dictionary<string, string>(), null)
            });
        RemoteFile file = new(
            "payload.bin",
            new Uri(Location),
            contents.Length,
            Sha256Of(contents),
            new AuthenticationHeaderValue("Bearer", "secret"));

        string path = await CreateStore(handler, sb).EnsureAsync(file, CancellationToken.None);

        Assert.That(await File.ReadAllBytesAsync(path), Is.EqualTo(contents));
        Assert.That(handler.Requests, Is.EqualTo([$"GET {Location} Bearer secret", $"GET {Signed}"]));
    }

    [Test]
    public async Task DigestMismatchTest()
    {
        using Sandbox sb = Sandbox.Create();

        byte[] contents = BuildContents(4096);
        byte[] other = BuildContents(2048);
        TestHttpMessageHandler handler = CreateHandler(contents);
        DownloadStore store = CreateStore(handler, sb);
        RemoteFile file = CreateFile(contents, Sha256Of(other));

        await Assert.ThrowsAsync<InvalidDataException>(() => store.EnsureAsync(file, CancellationToken.None));

        Assert.That(sb.GetTree(), Is.EqualTo(["downloads/"]));
    }

    [Test]
    public async Task TruncatedTest()
    {
        using Sandbox sb = Sandbox.Create();

        byte[] contents = BuildContents(4096);
        TestHttpMessageHandler handler = CreateHandler(contents);
        DownloadStore store = CreateStore(handler, sb);
        RemoteFile file = new("payload.bin", new Uri(Location), contents.Length + 1, null, null);

        await Assert.ThrowsAsync<InvalidDataException>(() => store.EnsureAsync(file, CancellationToken.None));

        Assert.That(sb.GetTree(), Is.EqualTo(["downloads/"]));
    }

    [Test]
    public async Task ExistingFileTest()
    {
        using Sandbox sb = Sandbox.Create();

        byte[] contents = BuildContents(4096);
        TestHttpMessageHandler handler = CreateHandler(contents);
        string directory = sb.CreateDirectory("downloads");

        await File.WriteAllTextAsync(Path.Combine(directory, "payload.bin"), "already here");

        string path = await CreateStore(handler, sb).EnsureAsync(
            CreateFile(contents, null), CancellationToken.None);

        Assert.That(await File.ReadAllTextAsync(path), Is.EqualTo("already here"));
        Assert.That(handler.Requests, Is.EqualTo(Array.Empty<string>()));
    }

    [Test]
    public async Task CompletePartialTest()
    {
        using Sandbox sb = Sandbox.Create();

        byte[] contents = "the whole payload"u8.ToArray();
        TestHttpMessageHandler handler = CreateHandler(contents);

        WritePartial(sb, contents);

        string path = await CreateStore(handler, sb).EnsureAsync(
            CreateFile(contents, Sha256Of(contents)), CancellationToken.None);

        Assert.That(await File.ReadAllBytesAsync(path), Is.EqualTo(contents));
        Assert.That(handler.Requests, Is.EqualTo(Array.Empty<string>()));
    }
}
