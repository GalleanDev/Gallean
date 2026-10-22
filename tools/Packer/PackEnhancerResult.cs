namespace Packer;

internal abstract record PackEnhancerResult
{
    public sealed record Enhanced(string? OutputPath, int EntryCount, int CacheEntryCount) : PackEnhancerResult;

    public sealed record NoSelection : PackEnhancerResult;

    public sealed record UnmatchedCategories(IReadOnlyList<string> Patterns) : PackEnhancerResult;
}
