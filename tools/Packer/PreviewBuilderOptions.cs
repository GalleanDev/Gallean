namespace Packer;

internal sealed record PreviewBuilderOptions(
    CategorySelector? Categories,
    string CacheDirectory,
    UmbraDevice Device,
    PreviewTransformType? Transform,
    string OutputPath,
    TimeSpan FrameDuration);
