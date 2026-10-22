namespace Umbra.CorridorKey;

internal sealed record CorridorKeyOptions(
    int ImageSize,
    int InputChannels,
    int EmbedDimension,
    int Heads,
    IReadOnlyList<int> Stages,
    IReadOnlyList<bool> MaskUnitAttention,
    int PoolStages,
    int MaskUnitSize,
    int PatchKernel,
    int PatchStride,
    int PatchPadding,
    double MlpRatio,
    int DecoderDimension,
    int RefinerChannels)
{
    public const int QueryStride = 2;
    public const int DimensionMultiplier = 2;
    public const int HeadMultiplier = 2;
    public const int AlphaChannels = 1;
    public const int ForegroundChannels = 3;
    public const double NormEpsilon = 1e-6;

    public int Depth => Stages.Sum();
    public int TokenGrid => ImageSize / PatchStride;
    public int FlatQueryStride => QueryStride * QueryStride;
    public int FlatMaskUnitSize => MaskUnitSize * MaskUnitSize;

    public IReadOnlyList<int> StageEnds
    {
        get
        {
            List<int> ends = [];
            int total = 0;

            foreach (int blocks in Stages)
            {
                total += blocks;
                ends.Add(total - 1);
            }

            return ends;
        }
    }

    public IReadOnlyList<int> FeatureChannels
    {
        get
        {
            List<int> channels = [];
            int dimension = EmbedDimension;

            for (int stage = 0; stage < Stages.Count; stage++)
            {
                channels.Add(dimension);
                dimension *= DimensionMultiplier;
            }

            return channels;
        }
    }

    public static CorridorKeyOptions BasePlus(int imageSize)
    {
        return new CorridorKeyOptions(
            ImageSize: imageSize,
            InputChannels: 4,
            EmbedDimension: 112,
            Heads: 2,
            Stages: [2, 3, 16, 3],
            MaskUnitAttention: [true, true, false, false],
            PoolStages: 3,
            MaskUnitSize: 8,
            PatchKernel: 7,
            PatchStride: 4,
            PatchPadding: 3,
            MlpRatio: 4,
            DecoderDimension: 256,
            RefinerChannels: 64);
    }
}
