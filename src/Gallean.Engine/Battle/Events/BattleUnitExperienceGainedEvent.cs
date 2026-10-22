namespace Gallean.Engine;

public sealed record BattleUnitExperienceGainedEvent(BattleUnitId UnitId, int ExperienceGained) : BattleEvent
{
    public override TResult Accept<TContext, TResult>(IBattleEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
