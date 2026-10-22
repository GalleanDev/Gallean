using System.Security.Cryptography;
using Gallean.Tests.Common;

namespace Packer.Tests;

public sealed class DownloadDigestTests
{
    [Test]
    public void FromBase64Test()
    {
        DownloadDigest digest = DownloadDigest.FromBase64(
            HashAlgorithmName.SHA512,
            "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==");

        Assert.That(digest, Is.EqualTo(new DownloadDigest(
            HashAlgorithmName.SHA512,
            "0b6cbac838dfe7f47ea1bd0df00ec282fdf45510c92161072ccfb84035390c4da743d9c3b954eaa1b0f86fc9861b23cc6c8667ab232c11c686432ebb5c8c3f27")));
    }

    [Test]
    public void FromHexTest()
    {
        DownloadDigest digest = DownloadDigest.FromHex(
            HashAlgorithmName.SHA256, "131DB0B57A618771D4D791B8E065C3286FF3B0FD92AFB2DCDD6119256688F94E");

        Assert.That(digest, Is.EqualTo(new DownloadDigest(
            HashAlgorithmName.SHA256, "131db0b57a618771d4d791b8e065c3286ff3b0fd92afb2dcdd6119256688f94e")));
    }

    [Test]
    public void FromBytesTest()
    {
        DownloadDigest digest = DownloadDigest.FromBytes(
            HashAlgorithmName.MD5, Convert.FromBase64String("Cw8TfxesEJRHFgILAY+BJg=="));

        Assert.That(digest, Is.EqualTo(new DownloadDigest(HashAlgorithmName.MD5, "0b0f137f17ac10944716020b018f8126")));
    }

    [Test]
    public void WrongLengthTest()
    {
        Assert.Throws<FormatException>(() => DownloadDigest.FromHex(
            HashAlgorithmName.SHA512, "131db0b57a618771d4d791b8e065c3286ff3b0fd92afb2dcdd6119256688f94e"));
    }

    [Test]
    public void NotHexTest()
    {
        Assert.Throws<FormatException>(() => DownloadDigest.FromHex(HashAlgorithmName.SHA256, new string('z', 64)));
    }

    [Test]
    public async Task MatchesTest()
    {
        using Sandbox sb = Sandbox.Create();

        string path = sb.GetPath("payload.bin");

        await File.WriteAllBytesAsync(path, "packer"u8.ToArray());

        DownloadDigest digest = new(
            HashAlgorithmName.SHA256, "131db0b57a618771d4d791b8e065c3286ff3b0fd92afb2dcdd6119256688f94e");
        DownloadDigest other = new(
            HashAlgorithmName.SHA256, "0000000000000000000000000000000000000000000000000000000000000000");

        Assert.That(await digest.MatchesAsync(path, CancellationToken.None), Is.True);
        Assert.That(await other.MatchesAsync(path, CancellationToken.None), Is.False);
    }
}
