namespace Gallean.Engine;

internal static partial class ThiefCalculator
{
    public static ThiefTarget? ResolveTarget(
        WorldState state,
        IEngineContext context,
        WorldPlayerId player,
        WorldPosition position)
    {
        WorldPlan plan = WorldPlan.For(state);

        WorldStack? stack = plan.GetStackAt(position);

        if (stack is not null)
        {
            (_, _, UnitDefinition leaderDefinition) = stack.GetLeader(context.Definitions);

            if (!DiplomacyCalculator.IsHostile(state, player, stack.OwnerId) ||
                leaderDefinition.Category == UnitCategory.Illusion)
            {
                return null;
            }

            StackStats stats = context.UnitStats.GetWorldStackStats(stack);

            return new ThiefStackTarget(stack, stats.Negotiate);
        }

        WorldFortification? fortification = plan.GetFortificationAt(position);

        if (fortification is WorldCity or WorldCapital)
        {
            WorldPlayerId? ownerId = FortificationCalculator.GetOwner(fortification);

            if (ownerId is null)
            {
                return null;
            }

            if (!DiplomacyCalculator.IsHostile(state, player, ownerId.Value))
            {
                return null;
            }

            if (FortificationCalculator.IsRioting(fortification))
            {
                return null;
            }

            int protection = FortificationCalculator.GetThiefProtection(fortification, context.Globals);

            return new ThiefFortificationTarget(fortification, ownerId.Value, protection);
        }

        if (fortification is WorldRuin ruin)
        {
            return new ThiefRuinTarget(ruin);
        }

        WorldSite? site = plan.GetSiteAt(position);

        if (site is WorldMerchantSite merchantSite)
        {
            return new ThiefMerchantSiteTarget(merchantSite);
        }

        if (site is WorldMageSite mageSite)
        {
            return new ThiefMageSiteTarget(mageSite);
        }

        return null;
    }
}
