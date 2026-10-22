namespace Umbra.Ltx;

internal sealed record LtxDitOptions(
    int HiddenSize,
    int Heads,
    int Depth,
    int InputChannels,
    int OutputChannels,
    int ContextChannels,
    double RopeTheta,
    IReadOnlyList<int> RopeMaxPositions,
    double TimestepScale)
{
    public const int ModulationCount = 9;
    public const int PromptModulationCount = 2;
    public const int OutputModulationCount = 2;

    public long MlpChunkTokens { get; init; } = 8192;
    public long AttentionChunkQueries { get; init; } = 4096;
    public int HeadDimension => HiddenSize / Heads;

    public static LtxDitOptions Distilled { get; } = new(
        HiddenSize: 4096,
        Heads: 32,
        Depth: 48,
        InputChannels: 128,
        OutputChannels: 128,
        ContextChannels: 4096,
        RopeTheta: 10000,
        RopeMaxPositions: [20, 2048, 2048],
        TimestepScale: 1000);
}
