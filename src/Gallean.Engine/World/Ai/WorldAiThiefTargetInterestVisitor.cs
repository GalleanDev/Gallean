namespace Gallean.Engine;

internal sealed class WorldAiThiefTargetInterestVisitor(WorldAiAttitudeDefinition attitude)
    : IWorldAiTargetVisitor<WorldAiTargetInterestVisitor.Context, int>
{
    private readonly WorldAiTargetInterestVisitor targetInterestVisitor = new(attitude);

    public int Visit(WorldAiTarget.Stack target, WorldAiTargetInterestVisitor.Context context)
    {
        return targetInterestVisitor.Visit(target, context);
    }

    public int Visit(WorldAiTarget.Fortification target, WorldAiTargetInterestVisitor.Context context)
    {
        WorldFortification fortification = context.State.Fortifications[target.FortificationId];

        if (FortificationCalculator.GetOwner(fortification) is not null)
        {
            return attitude.ThiefCityInterest;
        }
        else
        {
            return attitude.RuinInterest;
        }
    }

    public int Visit(WorldAiTarget.Location target, WorldAiTargetInterestVisitor.Context context)
    {
        return 0;
    }

    public int Visit(WorldAiTarget.Bag target, WorldAiTargetInterestVisitor.Context context)
    {
        return 0;
    }

    public int Visit(WorldAiTarget.Site target, WorldAiTargetInterestVisitor.Context context)
    {
        WorldSite site = context.State.Sites[target.SiteId];

        return site switch
        {
            WorldMerchantSite => attitude.ThiefMerchantInterest,
            WorldMageSite => attitude.MageInterest,
            WorldMercenarySite => 0,
            WorldTrainerSite => 0
        };
    }
}
