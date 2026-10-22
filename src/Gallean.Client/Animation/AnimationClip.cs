namespace Gallean.Client;

internal sealed record AnimationClip(
    AssetAnimationKey Animation,
    AnimationPlacement Placement,
    TimeSpan Start,
    TimeSpan Duration);
