namespace Umbra;

internal sealed record MosrOptions(
    int InChannels,
    int OutChannels,
    int Scale,
    int BlockCount,
    int Dimension,
    int KernelSize,
    double ExpansionRatio,
    double ConvRatio,
    int SampleGroups);
