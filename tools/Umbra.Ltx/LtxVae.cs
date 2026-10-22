using TorchSharp;
using TorchSharp.Utils;
using static TorchSharp.torch.nn;

namespace Umbra.Ltx;

internal sealed class LtxVae : Module
{
    [ComponentName(Name = "per_channel_statistics")]
    private readonly LtxVaeStatistics statistics;

    [ComponentName(Name = "encoder")]
    private readonly LtxVaeEncoder encoder;

    [ComponentName(Name = "decoder")]
    private readonly LtxVaeDecoder decoder;

    private readonly long tileSize;
    private readonly long tileOverlap;
    private readonly long residentBytes;
    private readonly int baseChannels;
    private readonly int patchSize;

    public LtxVae(LtxVaeOptions options)
        : base(nameof(LtxVae))
    {
        statistics = new LtxVaeStatistics(options.LatentChannels);
        encoder = new LtxVaeEncoder(options);
        decoder = new LtxVaeDecoder(options, causal: false);
        LatentChannels = options.LatentChannels;
        SpatialFactor = options.SpatialFactor;
        TemporalFactor = options.TemporalFactor;
        tileSize = options.TileSize;
        tileOverlap = options.TileOverlap;
        residentBytes = options.ResidentBytes;
        baseChannels = options.BaseChannels;
        patchSize = options.PatchSize;

        RegisterComponents();
    }

    public int LatentChannels { get; }
    public int SpatialFactor { get; }
    public int TemporalFactor { get; }

    public torch.Tensor Encode(torch.Tensor video)
    {
        using IDisposable scope = torch.NewDisposeScope();

        long size = GetTileSize(video.shape[2], video.element_size());

        using torch.Tensor means = LtxVaeTiling.Apply(encoder.forward, video, SpatialFactor, 1, size, tileOverlap);

        return statistics.Normalize(means).MoveToOuterDisposeScope();
    }

    public torch.Tensor Decode(torch.Tensor latent, torch.Tensor frames)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor unnormalizedLatent = statistics.Unnormalize(latent);

        long outputFrames = ((latent.shape[2] - 1) * TemporalFactor) + 1;
        long size = GetTileSize(outputFrames, latent.element_size());

        return LtxVaeTiling
            .Apply(slice => SelectFrames(decoder.forward(slice), frames), unnormalizedLatent, 1, SpatialFactor, size, tileOverlap)
            .MoveToOuterDisposeScope();
    }

    public torch.Tensor Unnormalize(torch.Tensor latent)
    {
        return statistics.Unnormalize(latent);
    }

    public torch.Tensor Normalize(torch.Tensor latent)
    {
        return statistics.Normalize(latent);
    }

    private static torch.Tensor SelectFrames(torch.Tensor video, torch.Tensor frames)
    {
        using torch.Tensor indices = frames.to(video.device);
        using torch.Tensor selectedVideo = video.index_select(2, indices);

        return selectedVideo.cpu();
    }

    private long GetTileSize(long frames, long elementSize)
    {
        double widestScale = (double)SpatialFactor / patchSize;
        double fitting = Math.Sqrt((double)residentBytes / (baseChannels * frames * elementSize)) / widestScale;

        return Math.Clamp((long)fitting, (2 * tileOverlap) + 1, tileSize);
    }
}
