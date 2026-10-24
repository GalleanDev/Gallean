using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace Gallean.Client;

internal sealed class ViewDataTemplate : IDataTemplate
{
    public Control Build(object? data)
    {
        Control control = data switch
        {
            AlertViewModel => new AlertView(),
            AttackPromptViewModel => new AttackPromptView(),
            BattleLogPopupViewModel => new BattleLogPopupView(),
            BattleScreenViewModel => new BattleScreenView(),
            BriefingScreenViewModel => new BriefingScreenView(),
            BuildingTabViewModel => new BuildingTabView(),
            CampaignSelectScreenViewModel => new CampaignSelectScreenView(),
            CapitalSceneViewModel => new CapitalSceneView(),
            CityTabViewModel => new CityTabView(),
            ConfirmPromptViewModel => new ConfirmPromptView(),
            BuildingDetailsViewModel => new BuildingDetailsView(),
            DetailsViewModel => new DetailsView(),
            DevToolsViewModel => new DevToolsView(),
            DiplomacyProposalViewModel => new DiplomacyProposalView(),
            DiplomacyTabViewModel => new DiplomacyTabView(),
            ErrorScreenViewModel => new ErrorScreenView(),
            ExchangeTabViewModel => new ExchangeTabView(),
            GameEndedScreenViewModel => new GameEndedScreenView(),
            GameMenuViewModel => new GameMenuView(),
            HotseatTurnScreenViewModel => new HotseatTurnScreenView(),
            ItemDetailsViewModel => new ItemDetailsView(),
            LeaderBlankslateViewModel => new LeaderBlankslateView(),
            LeaderExportScreenViewModel => new LeaderExportScreenView(),
            LeaderPurchaseViewModel => new LeaderPurchaseView(),
            LeaderUpgradeViewModel => new LeaderUpgradeView(),
            LoadGameScreenViewModel => new LoadGameScreenView(),
            LoadingScreenViewModel => new LoadingScreenView(),
            LootScreenViewModel => new LootScreenView(),
            MageTabViewModel => new MageTabView(),
            MainMenuScreenViewModel => new MainMenuScreenView(),
            ManagementScreenViewModel => new ManagementScreenView(),
            MercenaryTabViewModel => new MercenaryTabView(),
            MerchantTabViewModel => new MerchantTabView(),
            OfferPriceViewModel => new OfferPriceView(),
            OfferStatusViewModel => new OfferStatusView(),
            RaceSelectScreenViewModel => new RaceSelectScreenView(),
            ResourceSetViewModel => new ResourceSetView(),
            SaveGameScreenViewModel => new SaveGameScreenView(),
            ScenarioConfigurationScreenViewModel => new ScenarioConfigurationScreenView(),
            ScenarioEventPopupViewModel => new ScenarioEventPopupView(),
            ScenarioSelectScreenViewModel => new ScenarioSelectScreenView(),
            SettingsScreenViewModel => new SettingsScreenView(),
            SpellDetailsViewModel => new SpellDetailsView(),
            SpellbookTabViewModel => new SpellbookTabView(),
            StackLeaderViewModel => new StackLeaderView(),
            StackTabViewModel => new StackTabView(),
            ThiefActionResultScreenViewModel => new ThiefActionResultScreenView(),
            ThiefActionScreenViewModel => new ThiefActionScreenView(),
            ThiefStealScreenViewModel => new ThiefStealScreenView(),
            TrainerTabViewModel => new TrainerTabView(),
            UnitDetailsViewModel => new UnitDetailsView(),
            UnitPurchaseViewModel => new UnitPurchaseView(),
            WorldScreenViewModel world => new WorldScreenView(world.GameModel.Definitions, world.GameModel.Scenario, world.Game.DevTools.Diagnostics),
            _ => throw new NotSupportedException()
        };

        control.DataContext = data;

        return control;
    }

    public bool Match(object? data)
    {
        return data is not null && data.GetType().Name.EndsWith("ViewModel", StringComparison.Ordinal);
    }
}
