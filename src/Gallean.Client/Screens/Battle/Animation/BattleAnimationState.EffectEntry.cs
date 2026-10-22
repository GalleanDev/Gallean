namespace Gallean.Client;

internal sealed partial record BattleAnimationState
{
    public sealed record EffectEntry(
        AssetAnimationKey Animation,
        BattleGridFootprint Footprint,
        TimeSpan At,
        TimeSpan Duration) : IBattleAnimationEntry;
}
