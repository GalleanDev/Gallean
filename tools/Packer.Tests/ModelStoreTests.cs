using Gallean.Tests.Common;

namespace Packer.Tests;

public sealed class ModelStoreTests
{
    [Test]
    public void UpscaleRequiredFilesTest()
    {
        Assert.That(
            ModelStore.GetRequiredFiles([EnhancementType.Upscale]),
            Is.EqualTo(["basicvsrpp-reds4.safetensors", "mosr-aomr-2x.safetensors"]));
    }

    [Test]
    public void RequiredFilesTest()
    {
        Assert.That(ModelStore.GetRequiredFiles([EnhancementType.Upscale, EnhancementType.Surface]), Is.EqualTo(
        [
            Path.Combine("vrgbx-inverse", "dit.safetensors"),
            Path.Combine("vrgbx-inverse", "vae.safetensors"),
            Path.Combine("vrgbx-inverse", "context.safetensors"),
            "basicvsrpp-reds4.safetensors",
            "mosr-aomr-2x.safetensors"
        ]));
    }

    [Test]
    public void StagedTest()
    {
        using Sandbox sb = Sandbox.Create();

        string baseDirectory = sb.CreateDirectory("app");
        string modelDirectory = sb.CreateDirectory("models");
        string staged = Path.Combine(baseDirectory, "Models");

        Directory.CreateDirectory(staged);

        ModelStore store = new(baseDirectory, modelDirectory);

        string? directory = store.FindModel([EnhancementType.Upscale, EnhancementType.Surface]);

        Assert.That(directory, Is.EqualTo(staged));
    }

    [Test]
    public void MissingTest()
    {
        using Sandbox sb = Sandbox.Create();

        string modelDirectory = sb.CreateDirectory("models");

        ModelStore store = new(sb.GetPath("app"), modelDirectory);

        Assert.That(store.FindModel([EnhancementType.Upscale]), Is.Null);
    }

    [Test]
    public async Task UpscaleTest()
    {
        using Sandbox sb = Sandbox.Create();

        string modelDirectory = sb.CreateDirectory("models");

        await File.WriteAllTextAsync(Path.Combine(modelDirectory, UmbraVsrUpscaleModel.FileName), "vsr");
        await File.WriteAllTextAsync(Path.Combine(modelDirectory, UmbraMosrUpscaleModel.FileName), "mosr");

        ModelStore store = new(sb.GetPath("app"), modelDirectory);

        string? directory = store.FindModel([EnhancementType.Upscale]);

        Assert.That(directory, Is.EqualTo(modelDirectory));
    }

    [Test]
    public async Task MissingSurfaceTest()
    {
        using Sandbox sb = Sandbox.Create();

        string modelDirectory = sb.CreateDirectory("models");

        await File.WriteAllTextAsync(Path.Combine(modelDirectory, UmbraVsrUpscaleModel.FileName), "vsr");
        await File.WriteAllTextAsync(Path.Combine(modelDirectory, UmbraMosrUpscaleModel.FileName), "mosr");

        ModelStore store = new(sb.GetPath("app"), modelDirectory);

        Assert.That(store.FindModel([EnhancementType.Upscale, EnhancementType.Surface]), Is.Null);
    }

    [Test]
    public void RefineRequiredFilesTest()
    {
        Assert.That(ModelStore.GetRequiredFiles([EnhancementType.Refine]), Is.EqualTo(
        [
            Path.Combine("ltx-2", "transformer.safetensors"),
            Path.Combine("ltx-2", "vae.safetensors"),
            Path.Combine("ltx-2", "upsampler.safetensors"),
            Path.Combine("ltx-2", "upscaler-lora.safetensors"),
            Path.Combine("ltx-2", "context.safetensors")
        ]));
    }
}
