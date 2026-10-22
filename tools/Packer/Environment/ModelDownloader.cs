using System.IO.Compression;
using System.Reflection;
using Gallean.Common;

namespace Packer;

internal sealed class ModelDownloader(ModelStore models, HuggingFaceClient huggingFaceClient, DownloadStore downloads)
{
    private const string ContextResourceName = "vrgbx-inverse-context.safetensors.gz";
    private const string UpscaleResourceName = "basicvsrpp-reds4.safetensors.gz";
    private const string MosrUpscaleResourceName = "mosr-aomr-2x.safetensors.gz";

    private static readonly HuggingFaceFile DitSource = new(
        "aleafy/V-RGBX", "3776946b084d5e89dda0416889bb216f85e350f5", "vrgbx_inverse_renderer.safetensors");

    private static readonly HuggingFaceFile VaeSource = new(
        "Kijai/WanVideo_comfy", "8260d429d19fd7a72304cad059160b95d843913f", "Wan2_1_VAE_fp32.safetensors");

    private static readonly HuggingFaceFile RefineTransformerSource = new(
        "Lightricks/LTX-2.5",
        "5e6e71018ee1756ed329b697a7b4aedc934dfce9",
        "diffusion_models/ltx-2.5-22b-distilled-transformer-bf16.safetensors");

    private static readonly HuggingFaceFile RefineVaeSource = new(
        "Lightricks/LTX-2.5",
        "5e6e71018ee1756ed329b697a7b4aedc934dfce9",
        "vae/ltx-2.5-video-vae-conv-bf16.safetensors");

    private static readonly HuggingFaceFile RefineUpsamplerSource = new(
        "Lightricks/LTX-2.5",
        "5e6e71018ee1756ed329b697a7b4aedc934dfce9",
        "latent_upscale_models/ltx-2.5-latent-spatial-upscaler-x2-bf16-1.0.safetensors");

    private static readonly HuggingFaceFile RefineLoraSource = new(
        "Lightricks/LTX-2.5-22b-IC-LoRA-Pixel-Spatial-Upscaler",
        "5863fdef3eaa8b2d69fa22e259a1d75fede215dd",
        "ltx-2.5-22b-ic-lora-pixel-spatial-upscaler-x2-1.0.safetensors");

    private static readonly HuggingFaceFile AlphaBlueSource = new(
        "nikopueringer/CorridorKeyBlue_1.0",
        "51e6ccaa4b703f54be20a72ac2c37784fb9ba1cd",
        "CorridorKeyBlue_1.0.safetensors");

    private static readonly HuggingFaceFile AlphaGreenSource = new(
        "nikopueringer/CorridorKey_v1.0",
        "f6386ddf042d8e92aeb5fd16cb9b101cff508195",
        "CorridorKey_v1.0.safetensors");

    public async Task<string> EnsureModelAsync(IReadOnlyList<EnhancementType> enhancements, CancellationToken cancellationToken)
    {
        string? existingDirectory = models.FindModel(enhancements);

        if (existingDirectory is not null)
        {
            return existingDirectory;
        }

        foreach (string file in ModelStore.GetRequiredFiles(enhancements))
        {
            await EnsureFileAsync(file, cancellationToken);
        }

        return models.ModelDirectory;
    }

    private static async Task WriteResourceAsync(
        string resourceName,
        string targetPath,
        CancellationToken cancellationToken)
    {
        Assembly assembly = typeof(ModelDownloader).Assembly;

        await using Stream? resource = assembly.GetManifestResourceStream(resourceName);

        if (resource is null)
        {
            throw new InvalidOperationException($"The embedded resource '{resourceName}' is missing.");
        }

        await using GZipStream contents = new(resource, CompressionMode.Decompress);
        using AtomicFileWriter writer = new(targetPath, FileOptions.Asynchronous);

        await contents.CopyToAsync(writer.Stream, cancellationToken);
        await writer.CommitAsync();
    }

    private async Task EnsureFileAsync(string file, CancellationToken cancellationToken)
    {
        string targetPath = Path.Combine(models.ModelDirectory, file);

        if (File.Exists(targetPath))
        {
            return;
        }

        string? parent = Path.GetDirectoryName(targetPath);

        if (parent is not null)
        {
            Directory.CreateDirectory(parent);
        }

        switch (Path.GetDirectoryName(file), Path.GetFileName(file))
        {
            case (UmbraSurfaceModel.DirectoryName, UmbraSurfaceModel.DitFileName):
                await FetchAsync(DitSource, targetPath, cancellationToken);
                break;

            case (UmbraSurfaceModel.DirectoryName, UmbraSurfaceModel.VaeFileName):
                await FetchAsync(VaeSource, targetPath, cancellationToken);
                break;

            case (UmbraSurfaceModel.DirectoryName, UmbraSurfaceModel.ContextFileName):
                await WriteResourceAsync(ContextResourceName, targetPath, cancellationToken);
                break;

            case ("", UmbraVsrUpscaleModel.FileName):
                await WriteResourceAsync(UpscaleResourceName, targetPath, cancellationToken);
                break;

            case ("", UmbraMosrUpscaleModel.FileName):
                await WriteResourceAsync(MosrUpscaleResourceName, targetPath, cancellationToken);
                break;

            case (UmbraRefineModel.DirectoryName, UmbraRefineModel.TransformerFileName):
                await FetchAsync(RefineTransformerSource, targetPath, cancellationToken);
                break;

            case (UmbraRefineModel.DirectoryName, UmbraRefineModel.VaeFileName):
                await FetchAsync(RefineVaeSource, targetPath, cancellationToken);
                break;

            case (UmbraRefineModel.DirectoryName, UmbraRefineModel.UpsamplerFileName):
                await FetchAsync(RefineUpsamplerSource, targetPath, cancellationToken);
                break;

            case (UmbraRefineModel.DirectoryName, UmbraRefineModel.LoraFileName):
                await FetchAsync(RefineLoraSource, targetPath, cancellationToken);
                break;

            case (UmbraAlphaModel.DirectoryName, UmbraAlphaModel.BlueFileName):
                await FetchAsync(AlphaBlueSource, targetPath, cancellationToken);
                break;

            case (UmbraAlphaModel.DirectoryName, UmbraAlphaModel.GreenFileName):
                await FetchAsync(AlphaGreenSource, targetPath, cancellationToken);
                break;

            case (UmbraRefineModel.DirectoryName, UmbraRefineModel.ContextFileName):
                throw new KeyNotFoundException($"Cannot find prompt context at '{targetPath}'.");

            default:
                throw new NotSupportedException($"The model file '{file}' has no source.");
        }
    }

    private async Task FetchAsync(HuggingFaceFile source, string targetPath, CancellationToken cancellationToken)
    {
        RemoteFile file = await huggingFaceClient.GetRemoteFileAsync(source, cancellationToken);
        string downloadPath = await downloads.EnsureAsync(file, cancellationToken);

        File.Move(downloadPath, targetPath, overwrite: true);
    }
}
