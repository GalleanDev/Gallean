namespace Packer;

internal sealed record PreviewCommandOptions(
    string PackPath,
    IReadOnlyList<string> Categories,
    string CacheDirectory,
    UmbraDevice Device,
    PreviewTransformType? Transform,
    string OutputPath,
    uint FramesPerSecond);
