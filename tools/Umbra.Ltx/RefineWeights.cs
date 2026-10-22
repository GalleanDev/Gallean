using System.Diagnostics.CodeAnalysis;
using TorchSharp;

namespace Umbra.Ltx;

internal static class RefineWeights
{
    public const string TransformerFileName = "transformer.safetensors";
    public const string VaeFileName = "vae.safetensors";
    public const string UpsamplerFileName = "upsampler.safetensors";
    public const string LoraFileName = "upscaler-lora.safetensors";

    private const string TransformerPrefix = "model.diffusion_model.";
    private const string LoraPrefix = "diffusion_model.";
    private const string LoraDownSuffix = ".lora_A.weight";
    private const string LoraUpSuffix = ".lora_B.weight";

    public static Dictionary<string, torch.Tensor> Map(string directory, string fileName, torch.ScalarType dtype)
    {
        return ConvertType(SafeTensors.MapStateDict(Path.Combine(directory, fileName)), dtype);
    }

    public static Dictionary<string, torch.Tensor> MapTransformer(
        string directory,
        IReadOnlySet<string> parameterNames)
    {
        Dictionary<string, torch.Tensor> mapped = SafeTensors.MapStateDict(Path.Combine(directory, TransformerFileName));
        Dictionary<string, torch.Tensor> result = [];

        foreach ((string name, torch.Tensor tensor) in mapped)
        {
            if (!name.StartsWith(TransformerPrefix, StringComparison.Ordinal))
            {
                throw new InvalidDataException($"Unexpected transformer entry '{name}'.");
            }

            string localName = name[TransformerPrefix.Length..];

            if (parameterNames.Contains(localName))
            {
                result[localName] = tensor;
            }
            else
            {
                tensor.Dispose();
            }
        }

        return result;
    }

    public static LtxLora MapLora(string directory, double strength, IReadOnlySet<string> parameterNames)
    {
        Dictionary<string, torch.Tensor> mapped = SafeTensors.MapStateDict(Path.Combine(directory, LoraFileName));
        Dictionary<string, torch.Tensor> down = [];
        Dictionary<string, torch.Tensor> up = [];

        foreach ((string name, torch.Tensor tensor) in mapped)
        {
            if (TryParseLoraTarget(name, LoraDownSuffix, out string? downTarget))
            {
                if (!parameterNames.Contains(downTarget))
                {
                    throw new InvalidDataException($"LoRa entry '{name}' uses an unknown parameter.");
                }

                down[downTarget] = tensor;
            }
            else if (TryParseLoraTarget(name, LoraUpSuffix, out string? upTarget))
            {
                if (!parameterNames.Contains(upTarget))
                {
                    throw new InvalidDataException($"LoRa entry '{name}' uses an unknown parameter.");
                }

                up[upTarget] = tensor;
            }
            else
            {
                tensor.Dispose();
            }
        }

        return new LtxLora(Pair(down, up), strength);
    }

    private static bool TryParseLoraTarget(string name, string suffix, [NotNullWhen(true)] out string? target)
    {
        if (name.Length > LoraPrefix.Length + suffix.Length &&
            name.StartsWith(LoraPrefix, StringComparison.Ordinal) &&
            name.EndsWith(suffix, StringComparison.Ordinal))
        {
            target = $"{name[LoraPrefix.Length..^suffix.Length]}.weight";
        }
        else
        {
            target = null;
        }

        return target is not null;
    }

    private static Dictionary<string, LtxLoraEntry> Pair(
        Dictionary<string, torch.Tensor> down,
        Dictionary<string, torch.Tensor> up)
    {
        Dictionary<string, LtxLoraEntry> entries = [];

        foreach ((string target, torch.Tensor downProjection) in down)
        {
            if (!up.TryGetValue(target, out torch.Tensor? upProjection))
            {
                throw new InvalidDataException($"Unexpected LoRa entry '{target}'.");
            }

            entries[target] = new LtxLoraEntry(downProjection, upProjection);
        }

        foreach (string target in up.Keys)
        {
            if (!down.ContainsKey(target))
            {
                throw new InvalidDataException($"Unexpected LoRa entry '{target}'.");
            }
        }

        return entries;
    }

    private static Dictionary<string, torch.Tensor> ConvertType(Dictionary<string, torch.Tensor> mapped, torch.ScalarType dtype)
    {
        Dictionary<string, torch.Tensor> result = [];

        foreach ((string name, torch.Tensor tensor) in mapped)
        {
            torch.Tensor parameter = tensor.to(dtype);

            if (!ReferenceEquals(parameter, tensor))
            {
                tensor.Dispose();
            }

            result[name] = parameter;
        }

        return result;
    }
}
