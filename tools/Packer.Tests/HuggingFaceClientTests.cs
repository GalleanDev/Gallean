using System.Net.Http.Headers;
using System.Security.Cryptography;

namespace Packer.Tests;

public sealed class HuggingFaceClientTests
{
    private const string Location =
        "https://huggingface.co/aleafy/V-RGBX/resolve/3776946b/vrgbx_inverse_renderer.safetensors";

    private const string Signed = "https://cdn.hf.test/xet/blob";

    private static readonly HuggingFaceFile Source = new(
        "aleafy/V-RGBX", "3776946b", "vrgbx_inverse_renderer.safetensors");

    private static HuggingFaceClient CreateClient(TestHttpResource resource)
    {
        TestHttpMessageHandler handler = new(
            new Dictionary<string, TestHttpResource> { [Location] = resource });

        return new HuggingFaceClient(new HttpClient(handler), null);
    }

    [Test]
    public async Task StoredTest()
    {
        HuggingFaceClient client = CreateClient(new TestHttpResource(
            [],
            new Dictionary<string, string>
            {
                ["x-linked-size"] = "2838273936",
                ["x-linked-etag"] = "\"8EC059C834AD3A0ECB62E3D831C36550F5189D18400286F67EAC6B2AB88F0F3D\""
            },
            Signed));

        RemoteFile file = await client.GetRemoteFileAsync(Source, CancellationToken.None);

        Assert.That(file, Is.EqualTo(new RemoteFile(
            "vrgbx_inverse_renderer.safetensors",
            new Uri(Location),
            2838273936,
            new DownloadDigest(
                HashAlgorithmName.SHA256,
                "8ec059c834ad3a0ecb62e3d831c36550f5189d18400286f67eac6b2ab88f0f3d"),
            null)));
    }

    [Test]
    public async Task PlainTest()
    {
        HuggingFaceClient client = CreateClient(
            new TestHttpResource(new byte[32], new Dictionary<string, string>(), null));

        RemoteFile file = await client.GetRemoteFileAsync(Source, CancellationToken.None);

        Assert.That(file, Is.EqualTo(new RemoteFile(
            "vrgbx_inverse_renderer.safetensors", new Uri(Location), 32, null, null)));
    }

    [Test]
    public async Task ShortEtagTest()
    {
        HuggingFaceClient client = CreateClient(new TestHttpResource(
            new byte[32],
            new Dictionary<string, string> { ["x-linked-etag"] = "\"abc123\"" },
            null));

        RemoteFile file = await client.GetRemoteFileAsync(Source, CancellationToken.None);

        Assert.That(file, Is.EqualTo(new RemoteFile(
            "vrgbx_inverse_renderer.safetensors", new Uri(Location), 32, null, null)));
    }

    [Test]
    public async Task TokenTest()
    {
        TestHttpMessageHandler handler = new(
            new Dictionary<string, TestHttpResource>
            {
                [Location] = new(new byte[32], new Dictionary<string, string>(), null)
            });
        HuggingFaceClient client = new(new HttpClient(handler), "secret");

        RemoteFile file = await client.GetRemoteFileAsync(Source, CancellationToken.None);

        Assert.That(handler.Requests, Is.EqualTo([$"HEAD {Location} Bearer secret"]));
        Assert.That(file, Is.EqualTo(new RemoteFile(
            "vrgbx_inverse_renderer.safetensors",
            new Uri(Location),
            32,
            null,
            new AuthenticationHeaderValue("Bearer", "secret"))));
    }

    [Test]
    public async Task StoredWithoutSizeTest()
    {
        HuggingFaceClient client = CreateClient(
            new TestHttpResource([], new Dictionary<string, string>(), Signed));

        await Assert.ThrowsAsync<InvalidDataException>(() => client.GetRemoteFileAsync(Source, CancellationToken.None));
    }
}
