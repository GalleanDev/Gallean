using System.Security.Cryptography;

namespace Packer.Tests;

public sealed class NuGetClientTests
{
    private const string Location =
        "https://api.nuget.org/v3-flatcontainer/sample.native/1.2.3/sample.native.1.2.3.nupkg";

    private static readonly NuGetPackage Package = new("Sample.Native", "1.2.3");

    private static NuGetClient CreateClient(IReadOnlyDictionary<string, string> headers)
    {
        TestHttpMessageHandler handler = new(
            new Dictionary<string, TestHttpResource>
            {
                [Location] = new(new byte[16], headers, null)
            });

        return new NuGetClient(new HttpClient(handler));
    }

    [Test]
    public void FileNameTest()
    {
        Assert.That(NuGetClient.GetFileName(Package), Is.EqualTo("sample.native.1.2.3.nupkg"));
    }

    [Test]
    public async Task RemoteFileTest()
    {
        NuGetClient client = CreateClient(new Dictionary<string, string>());

        RemoteFile file = await client.GetRemoteFileAsync(Package, CancellationToken.None);

        Assert.That(file, Is.EqualTo(new RemoteFile("sample.native.1.2.3.nupkg", new Uri(Location), 16, null, null)));
    }

    [Test]
    public async Task DigestTest()
    {
        NuGetClient client = CreateClient(
            new Dictionary<string, string>
            {
                ["x-ms-meta-sha512"] =
                    "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=="
            });

        RemoteFile file = await client.GetRemoteFileAsync(Package, CancellationToken.None);

        Assert.That(file, Is.EqualTo(new RemoteFile(
            "sample.native.1.2.3.nupkg",
            new Uri(Location),
            16,
            new DownloadDigest(
                HashAlgorithmName.SHA512,
                "0b6cbac838dfe7f47ea1bd0df00ec282fdf45510c92161072ccfb84035390c4d" +
                "a743d9c3b954eaa1b0f86fc9861b23cc6c8667ab232c11c686432ebb5c8c3f27"),
            null)));
    }

    [Test]
    public async Task MissingTest()
    {
        TestHttpMessageHandler handler = new(new Dictionary<string, TestHttpResource>());
        NuGetClient client = new(new HttpClient(handler));

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetRemoteFileAsync(Package, CancellationToken.None));
    }
}
