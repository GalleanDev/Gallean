namespace Gallean.Client;

internal sealed partial record BattleAnimationState
{
    public sealed record BodyEntry(
        TimeSpan At,
        TimeSpan Duration,
        AssetAnimationKey.BattleUnitPose Pose,
        BattleUnitState.Presence Presence,
        AnimationProperty? Opacity) : IBattleAnimationEntry;
}
