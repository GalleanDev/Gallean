namespace GameContent;

internal sealed record MqImageMetadata(
    int TransparentColorIndex,
    int OpacityAlgorithm,
    int SizeX,
    int SizeY,
    IReadOnlyList<Color> Palette);
