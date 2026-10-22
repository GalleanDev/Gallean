namespace Gallean.Assets;

public readonly record struct AssetAnimationMetadata(
    int FrameCount,
    int Width,
    int Height,
    int OffsetX,
    int OffsetY,
    int? TriggerFrame,
    int? SecondTriggerFrame);
