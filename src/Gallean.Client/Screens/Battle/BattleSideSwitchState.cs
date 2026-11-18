using Gallean.Configuration;

namespace Gallean.Client;

internal sealed record BattleSideSwitchState(BattleRenderState Previous, BattlePosition Position) : IAnimationObject
{
    public TimeSpan Duration => TimeSpan.FromSeconds(0.75);
}
