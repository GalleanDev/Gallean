namespace Gallean.Engine;

internal sealed class WorldAiThiefTargetInterestVisitor(WorldAiAttitudeDefinition attitude)
    : IWorldAiTargetVisitor<WorldAiTargetInterestVisitor.Context, int>
{
    private readonly WorldAiTargetInterestVisitor stackInterestVisitor = new(attitude);

    public int Visit(WorldAiTarget.Stack target, WorldAiTargetInterestVisitor.Context context)
    {
        return stackInterestVisitor.Visit(target, context);
    }

    public int Visit(WorldAiTarget.Fortification target, WorldAiTargetInterestVisitor.Context context)
    {
        return attitude.ThiefCityInterest;
    }

    public int Visit(WorldAiTarget.Location target, WorldAiTargetInterestVisitor.Context context)
    {
        return 0;
    }

    public int Visit(WorldAiTarget.Bag target, WorldAiTargetInterestVisitor.Context context)
    {
        return 0;
    }
}
