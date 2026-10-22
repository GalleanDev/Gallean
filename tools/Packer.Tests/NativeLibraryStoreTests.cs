using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Gallean.Tests.Common;

namespace Packer.Tests;

public sealed class NativeLibraryStoreTests
{
    private const string NativePrefix = "runtimes";
    private const string FragmentPrefix = "fragments";

    private static string GetLocation(NuGetPackage package)
    {
        string id = package.Id.ToLowerInvariant();
        string version = package.Version.ToLowerInvariant();

        return $"https://api.nuget.org/v3-flatcontainer/{id}/{version}/{NuGetClient.GetFileName(package)}";
    }

    private static byte[] BuildPackage(NuGetPackage package, IReadOnlyDictionary<string, byte[]> entries)
    {
        using MemoryStream buffer = new();

        using (ZipArchive archive = new(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, $"{package.Id}.nuspec", Encoding.UTF8.GetBytes(BuildNuspec(package, [])));

            foreach (KeyValuePair<string, byte[]> entry in entries)
            {
                WriteEntry(archive, entry.Key, entry.Value);
            }
        }

        return buffer.ToArray();
    }

    private static byte[] BuildMetaPackage(NuGetPackage package, IReadOnlyList<NuGetPackage> dependencies)
    {
        using MemoryStream buffer = new();

        using (ZipArchive archive = new(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, $"{package.Id}.nuspec", Encoding.UTF8.GetBytes(BuildNuspec(package, dependencies)));
        }

        return buffer.ToArray();
    }

    private static string BuildNuspec(NuGetPackage package, IReadOnlyList<NuGetPackage> dependencies)
    {
        StringBuilder builder = new();

        builder.Append("<?xml version=\"1.0\"?>");
        builder.Append("<package xmlns=\"http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd\"><metadata>");
        builder.Append($"<id>{package.Id}</id><version>{package.Version}</version><dependencies><group>");

        foreach (NuGetPackage dependency in dependencies)
        {
            builder.Append($"<dependency id=\"{dependency.Id}\" version=\"{dependency.Version}\" />");
        }

        builder.Append("</group></dependencies></metadata></package>");

        return builder.ToString();
    }

    private static void WriteEntry(ZipArchive archive, string name, byte[] contents)
    {
        ZipArchiveEntry entry = archive.CreateEntry(name, CompressionLevel.NoCompression);

        using Stream stream = entry.Open();

        stream.Write(contents);
    }

    private static string BuildNativePath(string fileName)
    {
        return $"{NativePrefix}/{NativeRuntime.Identifier}/native/{fileName}";
    }

    private static string BuildFragmentPath(string fileName)
    {
        return $"{FragmentPrefix}/{NativeRuntime.Identifier}/native/{fileName}";
    }

    private static byte[] HexSha256(byte[] contents)
    {
        byte[] hash = SHA256.HashData(contents);

        return Encoding.ASCII.GetBytes(Convert.ToHexStringLower(hash));
    }

    private static NativeLibraryStore CreateStore(TestHttpMessageHandler handler, Sandbox sb)
    {
        HttpClient httpClient = new(handler);
        DownloadStore downloadStore = new(httpClient, new TestDownloadProgress(), sb.GetPath("downloads"));

        return new NativeLibraryStore(new NuGetClient(httpClient), downloadStore, sb.GetPath("app"), sb.GetPath("native"));
    }

    [Test]
    public async Task InstallTest()
    {
        using Sandbox sb = Sandbox.Create();

        NuGetPackage package = new("Sample.Native", "1.2.3");
        byte[] library = "the shared library"u8.ToArray();
        TestHttpMessageHandler handler = new(
            new Dictionary<string, TestHttpResource>
            {
                [GetLocation(package)] = new(
                    BuildPackage(package, new Dictionary<string, byte[]> { [BuildNativePath("libsample.so")] = library }),
                    new Dictionary<string, string>(),
                    null)
            });

        NativeLibraryStore store = CreateStore(handler, sb);
        NativeLibrarySet set = new("sample-1.2.3", [package], ["libsample.so"]);

        string libraryDirectory = await store.EnsureLibraryAsync(set, CancellationToken.None);

        Assert.That(await File.ReadAllBytesAsync(Path.Combine(libraryDirectory, "libsample.so")), Is.EqualTo(library));
        Assert.That(libraryDirectory, Is.EqualTo(Path.Combine(sb.GetPath("native"), NativeRuntime.Identifier, "sample-1.2.3")));
        Assert.That(Directory.GetFiles(sb.GetPath("downloads")), Is.EqualTo(Array.Empty<string>()));
    }

    [Test]
    public async Task FragmentTest()
    {
        using Sandbox sb = Sandbox.Create();

        NuGetPackage root = new("Sample.Split", "1.0.0");
        NuGetPackage primary = new("Sample.Split-part1-primary", "1.0.0");
        NuGetPackage fragment = new("Sample.Split-part1-fragment1", "1.0.0");

        byte[] head = "the first half of "u8.ToArray();
        byte[] tail = "the shared library"u8.ToArray();
        byte[] full = [.. head, .. tail];

        TestHttpMessageHandler handler = new(
            new Dictionary<string, TestHttpResource>
            {
                [GetLocation(root)] = new(
                    BuildMetaPackage(root, [primary, fragment]), new Dictionary<string, string>(), null),
                [GetLocation(primary)] = new(
                    BuildPackage(primary, new Dictionary<string, byte[]>
                    {
                        [BuildNativePath("libsplit.so")] = head,
                        [BuildNativePath("libsplit.so.sha")] = HexSha256(full)
                    }),
                    new Dictionary<string, string>(),
                    null),
                [GetLocation(fragment)] = new(
                    BuildPackage(fragment, new Dictionary<string, byte[]>
                    {
                        [BuildFragmentPath("libsplit.so.fragment1")] = tail
                    }),
                    new Dictionary<string, string>(),
                    null)
            });

        NativeLibraryStore store = CreateStore(handler, sb);
        NativeLibrarySet set = new("split-1.0.0", [root], ["libsplit.so"]);

        string libraryDirectory = await store.EnsureLibraryAsync(set, CancellationToken.None);

        Assert.That(await File.ReadAllBytesAsync(Path.Combine(libraryDirectory, "libsplit.so")), Is.EqualTo(full));
        Assert.That(Directory.GetFiles(libraryDirectory).Select(Path.GetFileName), Is.EqualTo(["libsplit.so"]));
    }

    [Test]
    public async Task CorruptedFragmentTest()
    {
        using Sandbox sb = Sandbox.Create();

        NuGetPackage package = new("Sample.Broken", "1.0.0");
        byte[] library = "the shared library"u8.ToArray();

        TestHttpMessageHandler handler = new(
            new Dictionary<string, TestHttpResource>
            {
                [GetLocation(package)] = new(
                    BuildPackage(package, new Dictionary<string, byte[]>
                    {
                        [BuildNativePath("libbroken.so")] = library,
                        [BuildNativePath("libbroken.so.sha")] = HexSha256("something else"u8.ToArray())
                    }),
                    new Dictionary<string, string>(),
                    null)
            });

        NativeLibraryStore store = CreateStore(handler, sb);
        NativeLibrarySet set = new("broken-1.0.0", [package], ["libbroken.so"]);

        await Assert.ThrowsAsync<InvalidDataException>(() => store.EnsureLibraryAsync(set, CancellationToken.None));

        Assert.That(
            Directory.GetDirectories(Path.Combine(sb.GetPath("native"), NativeRuntime.Identifier)),
            Is.EqualTo(Array.Empty<string>()));
    }

    [Test]
    public async Task MissingLibraryTest()
    {
        using Sandbox sb = Sandbox.Create();

        NuGetPackage package = new("Sample.Empty", "1.0.0");

        TestHttpMessageHandler handler = new(
            new Dictionary<string, TestHttpResource>
            {
                [GetLocation(package)] = new(
                    BuildPackage(package, new Dictionary<string, byte[]>()), new Dictionary<string, string>(), null)
            });

        NativeLibraryStore store = CreateStore(handler, sb);
        NativeLibrarySet set = new("empty-1.0.0", [package], ["libempty.so"]);

        await Assert.ThrowsAsync<InvalidDataException>(() => store.EnsureLibraryAsync(set, CancellationToken.None));

        Assert.That(
            Directory.GetDirectories(Path.Combine(sb.GetPath("native"), NativeRuntime.Identifier)),
            Is.EqualTo(Array.Empty<string>()));
    }

    [Test]
    public async Task BesideExecutableTest()
    {
        using Sandbox sb = Sandbox.Create();

        TestHttpMessageHandler handler = new(new Dictionary<string, TestHttpResource>());
        NativeLibraryStore store = CreateStore(handler, sb);
        NativeLibrarySet set = new("sample-1.2.3", [new NuGetPackage("Sample.Native", "1.2.3")], ["libsample.so"]);
        string application = sb.CreateDirectory("app");

        await File.WriteAllTextAsync(Path.Combine(application, "libsample.so"), "already beside the executable");

        string libraryDirectory = await store.EnsureLibraryAsync(set, CancellationToken.None);

        Assert.That(libraryDirectory, Is.EqualTo(application));
        Assert.That(handler.Requests, Is.EqualTo(Array.Empty<string>()));
    }

    [Test]
    public async Task UnderRuntimesTest()
    {
        using Sandbox sb = Sandbox.Create();

        TestHttpMessageHandler handler = new(new Dictionary<string, TestHttpResource>());
        NativeLibraryStore store = CreateStore(handler, sb);
        NativeLibrarySet set = new("sample-1.2.3", [new NuGetPackage("Sample.Native", "1.2.3")], ["libsample.so"]);
        string native = Path.Combine(
            sb.CreateDirectory("app"), "runtimes", NativeRuntime.Identifier, "native");

        Directory.CreateDirectory(native);
        await File.WriteAllTextAsync(Path.Combine(native, "libsample.so"), "left by a plain build");

        string libraryDirectory = await store.EnsureLibraryAsync(set, CancellationToken.None);

        Assert.That(libraryDirectory, Is.EqualTo(native));
        Assert.That(handler.Requests, Is.EqualTo(Array.Empty<string>()));
    }

    [Test]
    public async Task CachedTest()
    {
        using Sandbox sb = Sandbox.Create();

        NuGetPackage package = new("Sample.Native", "1.2.3");
        byte[] library = "the shared library"u8.ToArray();
        TestHttpMessageHandler handler = new(
            new Dictionary<string, TestHttpResource>
            {
                [GetLocation(package)] = new(
                    BuildPackage(package, new Dictionary<string, byte[]> { [BuildNativePath("libsample.so")] = library }),
                    new Dictionary<string, string>(),
                    null)
            });

        NativeLibraryStore store = CreateStore(handler, sb);
        NativeLibrarySet set = new("sample-1.2.3", [package], ["libsample.so"]);

        await store.EnsureLibraryAsync(set, CancellationToken.None);
        await store.EnsureLibraryAsync(set, CancellationToken.None);

        Assert.That(handler.Requests, Is.EqualTo([
            "HEAD https://api.nuget.org/v3-flatcontainer/sample.native/1.2.3/sample.native.1.2.3.nupkg",
            "GET https://api.nuget.org/v3-flatcontainer/sample.native/1.2.3/sample.native.1.2.3.nupkg"
        ]));
    }

    [Test]
    public async Task FindTest()
    {
        using Sandbox sb = Sandbox.Create();

        NuGetPackage package = new("Sample.Native", "1.2.3");
        byte[] library = "the shared library"u8.ToArray();
        TestHttpMessageHandler handler = new(
            new Dictionary<string, TestHttpResource>
            {
                [GetLocation(package)] = new(
                    BuildPackage(package, new Dictionary<string, byte[]> { [BuildNativePath("libsample.so")] = library }),
                    new Dictionary<string, string>(),
                    null)
            });

        NativeLibraryStore store = CreateStore(handler, sb);
        NativeLibrarySet set = new("sample-1.2.3", [package], ["libsample.so"]);

        await store.EnsureLibraryAsync(set, CancellationToken.None);

        string? libraryDirectory = store.FindLibrary(set);

        Assert.That(libraryDirectory, Is.EqualTo(Path.Combine(sb.GetPath("native"), NativeRuntime.Identifier, "sample-1.2.3")));
    }

    [Test]
    public void NotSetUpTest()
    {
        using Sandbox sb = Sandbox.Create();

        TestHttpMessageHandler handler = new(new Dictionary<string, TestHttpResource>());
        NativeLibraryStore store = CreateStore(handler, sb);
        NativeLibrarySet set = new("sample-1.2.3", [new NuGetPackage("Sample.Native", "1.2.3")], ["libsample.so"]);

        Assert.That(store.FindLibrary(set), Is.Null);
        Assert.That(handler.Requests, Is.EqualTo(Array.Empty<string>()));
    }
}
