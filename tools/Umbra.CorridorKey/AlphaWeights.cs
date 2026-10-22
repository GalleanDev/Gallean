using TorchSharp;

namespace Umbra.CorridorKey;

internal static class AlphaWeights
{
    public const string BlueFileName = "corridorkey-blue.safetensors";
    public const string GreenFileName = "corridorkey-green.safetensors";

    private const string CompiledPrefix = "_orig_mod.";
    private const string EncoderPrefix = "encoder.model.";

    public static Dictionary<string, torch.Tensor> Map(string path, CorridorKeyOptions options, torch.ScalarType dtype)
    {
        Dictionary<string, torch.Tensor> mapped = SafeTensors.MapStateDict(path);
        Dictionary<string, torch.Tensor> result = [];

        foreach ((string name, torch.Tensor tensor) in mapped)
        {
            string localName = Rename(name);

            if (localName.EndsWith(".num_batches_tracked", StringComparison.Ordinal))
            {
                tensor.Dispose();
            }
            else
            {
                torch.Tensor parameter = Reshape(localName, tensor, options.TokenGrid);

                result[localName] = Replace(parameter, parameter.to(dtype));
            }
        }

        return result;
    }

    private static string Rename(string name)
    {
        string stripped = name;

        if (stripped.StartsWith(CompiledPrefix, StringComparison.Ordinal))
        {
            stripped = stripped[CompiledPrefix.Length..];
        }

        if (stripped.StartsWith(EncoderPrefix, StringComparison.Ordinal))
        {
            stripped = $"encoder.{stripped[EncoderPrefix.Length..]}";
        }

        return stripped;
    }

    private static torch.Tensor Reshape(string name, torch.Tensor tensor, long grid)
    {
        if (name == "encoder.pos_embed")
        {
            return Replace(tensor, Resample(tensor, grid));
        }
        else
        {
            return tensor;
        }
    }

    private static torch.Tensor Resample(torch.Tensor tensor, long grid)
    {
        long tokens = tensor.shape[1];
        long source = (long)Math.Sqrt(tokens);

        if (source * source != tokens)
        {
            throw new InvalidDataException("Invalid position embedding.");
        }

        if (source == grid)
        {
            return tensor;
        }
        else
        {
            using IDisposable scope = torch.NewDisposeScope();

            long channels = tensor.shape[2];

            using torch.Tensor planes = tensor.permute(0, 2, 1).reshape(1, channels, source, source);
            using torch.Tensor resizedPlanes = torch.nn.functional.interpolate(
                planes, size: [grid, grid], mode: torch.InterpolationMode.Bicubic, align_corners: false);

            return resizedPlanes.flatten(2).transpose(1, 2).contiguous().MoveToOuterDisposeScope();
        }
    }

    private static torch.Tensor Replace(torch.Tensor source, torch.Tensor replacement)
    {
        if (!ReferenceEquals(source, replacement))
        {
            source.Dispose();
        }

        return replacement;
    }
}
