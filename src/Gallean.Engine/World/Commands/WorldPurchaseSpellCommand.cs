using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldPurchaseSpellCommand(
    WorldStackId StackId,
    WorldSiteId SiteId,
    WorldSpellDefinitionId SpellId) : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        WorldPlayerId currentPlayer = state.TurnInfo.CurrentPlayerId;

        WorldPlayer player = state.CurrentPlayer;

        if (!WorldCommandHelpers.TryGetCurrentPlayerStack(state, StackId, out WorldStack? stack))
        {
            return WorldCommandResult.Fail(WorldCommandError.StackNotFound);
        }

        if (!state.Sites.TryGetValue(SiteId, out WorldSite? site))
        {
            return WorldCommandResult.Fail(WorldCommandError.SiteNotFound);
        }

        if (site is not WorldMageSite mageSite)
        {
            return WorldCommandResult.Fail(WorldCommandError.SiteNotFound);
        }

        if (stack.Position != mageSite.Entrance)
        {
            return WorldCommandResult.Fail(WorldCommandError.StackNotAtSite);
        }

        MageSiteCalculator.PurchaseInfo purchase =
            MageSiteCalculator.GetPurchaseInfo(mageSite, stack, player, SpellId, context);

        if (purchase.Error is { } error)
        {
            return WorldCommandResult.Fail(error);
        }

        return WorldCommandResult.Success(
            Empty.Value,
            [
                new WorldSpellPurchasedEvent(
                    SiteId,
                    StackId,
                    currentPlayer,
                    SpellId,
                    purchase.Cost)
            ]);
    }
}
