namespace Gallean.Client;

internal sealed partial class ManagementScreenViewModel
{
    private sealed class CreateSiteTabVisitor(GameModel gameModel, GameViewModel gameViewModel, ManagementScreenViewModel screen) :
        IWorldSiteVisitor<WorldStackId, ManagementTabViewModel>
    {
        public ManagementTabViewModel Visit(WorldMerchantSite site, WorldStackId stackId)
        {
            return new MerchantTabViewModel(gameModel, gameViewModel, screen, site.Id, stackId);
        }

        public ManagementTabViewModel Visit(WorldMageSite site, WorldStackId stackId)
        {
            return new MageTabViewModel(gameModel, gameViewModel, screen, site.Id, stackId);
        }

        public ManagementTabViewModel Visit(WorldMercenarySite site, WorldStackId stackId)
        {
            return new MercenaryTabViewModel(gameModel, gameViewModel, screen, site.Id, stackId);
        }

        public ManagementTabViewModel Visit(WorldTrainerSite site, WorldStackId stackId)
        {
            return new TrainerTabViewModel(gameModel, gameViewModel, screen, stackId);
        }
    }
}
