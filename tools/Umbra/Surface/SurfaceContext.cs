using TorchSharp;

namespace Umbra;

internal sealed class SurfaceContext(Dictionary<string, torch.Tensor> embeddings) : IDisposable
{
    public const string EmbeddingsFileName = "context.safetensors";

    public static SurfaceContext Load(string directory)
    {
        string path = Path.Combine(directory, EmbeddingsFileName);

        return new SurfaceContext(SafeTensors.MapStateDict(path));
    }

    public torch.Tensor BuildPositive(SurfaceChannelType channel, torch.ScalarType dtype, torch.Device device)
    {
        return Take(channel.ToString().ToLowerInvariant(), dtype, device);
    }

    public torch.Tensor BuildNegative(torch.ScalarType dtype, torch.Device device)
    {
        return Take("negative", dtype, device);
    }

    public void Dispose()
    {
        foreach (torch.Tensor embedding in embeddings.Values)
        {
            embedding.Dispose();
        }

        embeddings.Clear();
    }

    private torch.Tensor Take(string name, torch.ScalarType dtype, torch.Device device)
    {
        using IDisposable scope = torch.NewDisposeScope();

        if (!embeddings.TryGetValue(name, out torch.Tensor? embedding))
        {
            throw new InvalidDataException($"The prompt embedding '{name}' is missing.");
        }

        torch.Tensor result = embedding.to(dtype, device).alias();

        return result.MoveToOuterDisposeScope();
    }
}
