namespace Packer;

internal abstract record PreviewBuilderResult
{
    public sealed record Written(IReadOnlyList<string> OutputPaths) : PreviewBuilderResult;

    public sealed record NoSelection : PreviewBuilderResult;

    public sealed record UnmatchedCategories(IReadOnlyList<string> Patterns) : PreviewBuilderResult;

    public sealed record MissingFrames(IReadOnlyList<string> Frames) : PreviewBuilderResult;

    public sealed record AmbiguousOutput(string OutputPath, IReadOnlyList<string> Ids) : PreviewBuilderResult;
}
