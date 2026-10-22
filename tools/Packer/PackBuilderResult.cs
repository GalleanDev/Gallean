namespace Packer;

internal abstract record PackBuilderResult(string OutputPath)
{
    public sealed record Packed(string OutputPath) : PackBuilderResult(OutputPath);

    public sealed record MissingRequired(string OutputPath, IReadOnlyList<string> AssetIds) : PackBuilderResult(OutputPath);
}
