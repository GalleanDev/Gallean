namespace Packer;

internal abstract record PackEnhancerProgress
{
    public sealed record Selected(
        int EntryCount,
        int UpscaleCount,
        int RefineCount,
        int AlphaCount,
        int SurfaceCount) : PackEnhancerProgress;

    public sealed record Written(int Processed, int Total) : PackEnhancerProgress;
}
