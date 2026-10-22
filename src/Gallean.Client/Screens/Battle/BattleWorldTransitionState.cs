namespace Gallean.Client;

internal sealed record BattleWorldTransitionState(BattleTransitionType Type) : IAnimationObject
{
    public TimeSpan Duration => Type switch
    {
        BattleTransitionType.In => TimeSpan.FromSeconds(1.3),
        BattleTransitionType.Out => TimeSpan.FromSeconds(0.5)
    };
}
