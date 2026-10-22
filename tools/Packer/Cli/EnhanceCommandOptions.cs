namespace Packer;

internal sealed record EnhanceCommandOptions(
    string PackPath,
    IReadOnlyList<string> Categories,
    IReadOnlyList<EnhancementType> Enhancements,
    string CacheDirectory,
    bool CacheOnly,
    string? OutputPath,
    bool Sparse,
    UmbraDevice Device);
