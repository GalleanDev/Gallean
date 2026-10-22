namespace Umbra.Ltx;

internal sealed record LtxVaeOptions(
    int LatentChannels,
    int BaseChannels,
    int PatchSize,
    IReadOnlyList<LtxVaeBlock> Blocks,
    IReadOnlyList<LtxVaeBlock> DecoderBlocks,
    long ChunkBytes)
{
    public long StreamBytes { get; init; } = 768L * 1024 * 1024;
    public long ResidentBytes { get; init; } = 2L * 1024 * 1024 * 1024;
    public long TileSize { get; init; } = 16;
    public long TileOverlap { get; init; } = 2;

    public int SpatialFactor
    {
        get
        {
            int factor = PatchSize;

            foreach (LtxVaeBlock block in Blocks)
            {
                factor *= block.Stride.Height;
            }

            return factor;
        }
    }

    public int TemporalFactor
    {
        get
        {
            int factor = 1;

            foreach (LtxVaeBlock block in Blocks)
            {
                factor *= block.Stride.Time;
            }

            return factor;
        }
    }

    public int BottleneckChannels => GetBottleneckChannels(Blocks);
    public int DecoderBottleneckChannels => GetBottleneckChannels(DecoderBlocks);

    public static LtxVaeOptions Distilled { get; } = new(
        LatentChannels: 128,
        BaseChannels: 128,
        PatchSize: 4,
        Blocks:
        [
            new LtxVaeBlock(LtxVaeBlockType.Residual, 4),
            new LtxVaeBlock(LtxVaeBlockType.CompressSpace, 2),
            new LtxVaeBlock(LtxVaeBlockType.Residual, 6),
            new LtxVaeBlock(LtxVaeBlockType.CompressTime, 2),
            new LtxVaeBlock(LtxVaeBlockType.Residual, 4),
            new LtxVaeBlock(LtxVaeBlockType.CompressAll, 2),
            new LtxVaeBlock(LtxVaeBlockType.Residual, 2),
            new LtxVaeBlock(LtxVaeBlockType.CompressAll, 1),
            new LtxVaeBlock(LtxVaeBlockType.Residual, 2)
        ],
        DecoderBlocks:
        [
            new LtxVaeBlock(LtxVaeBlockType.Residual, 4),
            new LtxVaeBlock(LtxVaeBlockType.CompressSpace, 2),
            new LtxVaeBlock(LtxVaeBlockType.Residual, 6),
            new LtxVaeBlock(LtxVaeBlockType.CompressTime, 2),
            new LtxVaeBlock(LtxVaeBlockType.Residual, 4),
            new LtxVaeBlock(LtxVaeBlockType.CompressAll, 1),
            new LtxVaeBlock(LtxVaeBlockType.Residual, 2),
            new LtxVaeBlock(LtxVaeBlockType.CompressAll, 2),
            new LtxVaeBlock(LtxVaeBlockType.Residual, 2)
        ],
        ChunkBytes: LtxCausalConv3d.DefaultChunkBytes);

    private int GetBottleneckChannels(IReadOnlyList<LtxVaeBlock> blocks)
    {
        int channels = BaseChannels;

        foreach (LtxVaeBlock block in blocks)
        {
            if (block.Type != LtxVaeBlockType.Residual)
            {
                channels *= block.Count;
            }
        }

        return channels;
    }
}
