namespace GameContent;

internal sealed record MqImage(
    string Name,
    int FileId,
    int Width,
    int Height,
    MqImageMetadata Metadata,
    IReadOnlyList<MqImagePart> ImageParts);
