namespace Packer;

internal sealed record PackEnhancerOptions(
    CategorySelector? Categories,
    string ModelDirectory,
    int SurfaceDenoisingSteps,
    UmbraDevice Device,
    string? OutputPath,
    bool Sparse,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    string CacheDirectory,
    string ScratchDirectory,
    IReadOnlyList<EnhancementType> Enhancements);
