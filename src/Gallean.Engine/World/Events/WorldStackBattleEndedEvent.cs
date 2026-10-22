namespace Gallean.Engine;

public sealed record WorldStackBattleEndedEvent(
    WorldStackId AttackerStackId,
    WorldStackId DefenderStackId,
    BattleSide WinningSide,
    UnitGrid<WorldUnit> SurvivingAttackerUnits,
    UnitGrid<WorldUnit> SurvivingDefenderUnits) : WorldEvent
{
    public override TResult Accept<TContext, TResult>(IWorldEventVisitor<TContext, TResult> visitor, TContext context)
    {
        return visitor.Visit(this, context);
    }
}
