using System.Security.Cryptography;
using Gallean.Tests.Common;

namespace Packer.Tests;

public sealed class ModelDownloaderTests
{
    private static string HexSha256(string path)
    {
        using FileStream stream = File.OpenRead(path);

        byte[] hash = SHA256.HashData(stream);

        return Convert.ToHexStringLower(hash);
    }

    private static List<string> FormatModels(string directory)
    {
        List<string> rows = [];

        foreach (string path in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            FileInfo info = new(path);
            string name = Path.GetRelativePath(directory, path).Replace(Path.DirectorySeparatorChar, '/');

            rows.Add($"{name} {info.Length} {HexSha256(path)}");
        }

        return rows;
    }

    [Test]
    public async Task SimpleTest()
    {
        using Sandbox sb = Sandbox.Create();

        string modelDirectory = sb.CreateDirectory("models");

        Directory.CreateDirectory(Path.Combine(modelDirectory, UmbraSurfaceModel.DirectoryName));
        await File.WriteAllTextAsync(Path.Combine(modelDirectory, UmbraSurfaceModel.DirectoryName, UmbraSurfaceModel.DitFileName), "downloaded");
        await File.WriteAllTextAsync(Path.Combine(modelDirectory, UmbraSurfaceModel.DirectoryName, UmbraSurfaceModel.VaeFileName), "downloaded");

        TestHttpMessageHandler handler = new(new Dictionary<string, TestHttpResource>());
        HttpClient client = new(handler);
        ModelDownloader downloader = new(
            new ModelStore(sb.GetPath("app"), modelDirectory),
            new HuggingFaceClient(client, null),
            new DownloadStore(client, new TestDownloadProgress(), sb.GetPath("downloads")));

        string resolvedDirectory = await downloader.EnsureModelAsync([EnhancementType.Upscale, EnhancementType.Surface], CancellationToken.None);

        Assert.That(resolvedDirectory, Is.EqualTo(modelDirectory));
        Assert.That(handler.Requests, Is.EqualTo(Array.Empty<string>()));
        Assert.That(FormatModels(resolvedDirectory), Is.EqualTo(
        [
            "basicvsrpp-reds4.safetensors 29324368 04c554d2547aed2e3521de2af9134f34c97a56722cbf4c8becafa3cdca6f424f",
            "mosr-aomr-2x.safetensors 17077764 a7bb1c5d8805ad8f07063faed41d1c4291ebf17b2ad53bd5af5bbeef41d11205",
            "vrgbx-inverse/context.safetensors 41943456 68798038aebae47f5330dab59cf6cb780f155aa32a56e24cb99744bf51106085",
            "vrgbx-inverse/dit.safetensors 10 b7a8a844a613be796bc1892dc480f9d92c50d32a5713a87758e5c5addc4ec814",
            "vrgbx-inverse/vae.safetensors 10 b7a8a844a613be796bc1892dc480f9d92c50d32a5713a87758e5c5addc4ec814"
        ]));
    }

    [Test]
    public async Task RefineTest()
    {
        using Sandbox sb = Sandbox.Create();

        string modelDirectory = sb.CreateDirectory("models");
        string refineDirectory = Path.Combine(modelDirectory, UmbraRefineModel.DirectoryName);

        Directory.CreateDirectory(refineDirectory);
        await File.WriteAllTextAsync(Path.Combine(refineDirectory, UmbraRefineModel.TransformerFileName), "downloaded");
        await File.WriteAllTextAsync(Path.Combine(refineDirectory, UmbraRefineModel.VaeFileName), "downloaded");
        await File.WriteAllTextAsync(Path.Combine(refineDirectory, UmbraRefineModel.UpsamplerFileName), "downloaded");
        await File.WriteAllTextAsync(Path.Combine(refineDirectory, UmbraRefineModel.LoraFileName), "downloaded");
        await File.WriteAllTextAsync(Path.Combine(refineDirectory, UmbraRefineModel.ContextFileName), "packed");

        TestHttpMessageHandler handler = new(new Dictionary<string, TestHttpResource>());
        HttpClient client = new(handler);
        ModelDownloader downloader = new(
            new ModelStore(sb.GetPath("app"), modelDirectory),
            new HuggingFaceClient(client, null),
            new DownloadStore(client, new TestDownloadProgress(), sb.GetPath("downloads")));

        string resolvedDirectory = await downloader.EnsureModelAsync([EnhancementType.Refine], CancellationToken.None);

        Assert.That(resolvedDirectory, Is.EqualTo(modelDirectory));
        Assert.That(handler.Requests, Is.EqualTo(Array.Empty<string>()));
        Assert.That(FormatModels(resolvedDirectory), Is.EqualTo(
        [
            "ltx-2/context.safetensors 6 88cb8a087b6e8cebfc9ae5602f5a2159a6bcf923e7f2c56809bcda6cad1727a7",
            "ltx-2/transformer.safetensors 10 b7a8a844a613be796bc1892dc480f9d92c50d32a5713a87758e5c5addc4ec814",
            "ltx-2/upsampler.safetensors 10 b7a8a844a613be796bc1892dc480f9d92c50d32a5713a87758e5c5addc4ec814",
            "ltx-2/upscaler-lora.safetensors 10 b7a8a844a613be796bc1892dc480f9d92c50d32a5713a87758e5c5addc4ec814",
            "ltx-2/vae.safetensors 10 b7a8a844a613be796bc1892dc480f9d92c50d32a5713a87758e5c5addc4ec814"
        ]));
    }
}
