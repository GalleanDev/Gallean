using CommunityToolkit.Mvvm.ComponentModel;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class LeaderPurchaseViewModel : AbstractGameScreenViewModel
{
    private readonly WorldFortificationId fortificationId;

    public LeaderPurchaseViewModel(
        GameModel gameModel,
        GameViewModel gameViewModel,
        WorldFortificationId fortificationId)
        : base(gameModel, gameViewModel)
    {
        this.fortificationId = fortificationId;

        RefreshView();
    }

    [ObservableProperty]
    public partial IReadOnlyList<LeaderPurchaseItemViewModel> Options { get; private set; } = [];

    protected override bool TryClose()
    {
        CloseSelf();

        return true;
    }

    protected override void OnSnapshotChanged(GameSnapshot current)
    {
        RefreshView();
    }

    private void RefreshView()
    {
        Options = BuildOptions();
    }

    public async Task PurchaseAsync(LeaderPurchaseItemViewModel item)
    {
        if (await TryDispatchWorldActionAsync(new WorldPurchaseLeaderCommand(fortificationId, item.LeaderId)))
        {
            CloseSelf();
        }
    }

    private IReadOnlyList<LeaderPurchaseItemViewModel> BuildOptions()
    {
        WorldPlayer player = WorldState.Players[ViewPlayerId];
        WorldRaceDefinition race = GameModel.Definitions.GetRace(player.Race);

        List<LeaderPurchaseItemViewModel> result = [];

        foreach (UnitDefinitionId leaderId in race.Leaders)
        {
            UnitDefinition definition = GameModel.Definitions.GetUnit(leaderId);
            string name = GameModel.Texts.GetGlobalText(definition.NameTextId).Text;
            string description = GameModel.Texts.GetGlobalText(definition.DescriptionTextId).Text;
            string? unavailableReason;

            WorldPurchaseLeaderCommand command = new(fortificationId, leaderId);
            WorldCommandResult commandResult = GameModel.World.DryRunCommand(command);

            if (commandResult.Failed && commandResult.Error.Value != WorldCommandError.InsufficientResources)
            {
                unavailableReason = FormatUnavailability(commandResult.Error.Value, definition);
            }
            else
            {
                unavailableReason = null;
            }

            WorldUnit unit = new(UnitDetailsViewModel.PreviewUnitId, leaderId, definition.MaxHitpoints, Experience: 0, definition.Level, [], null, false);
            UnitDetailsViewModel details = UnitDetailsViewModel.CreateFromWorld(unit, null, GameModel.Definitions, GameModel.Texts, GameModel.Stats);

            result.Add(new LeaderPurchaseItemViewModel(
                this,
                leaderId,
                name,
                description,
                new OfferStatusViewModel(new OfferPriceViewModel(definition.PurchaseCost, player.Resources), unavailableReason),
                details));
        }

        return result;
    }

    private string FormatUnavailability(WorldCommandError error, UnitDefinition definition)
    {
        switch (error)
        {
            case WorldCommandError.TargetOccupied:
                return Tr("City entrance is occupied");

            case WorldCommandError.UnitMissingPrerequisite when definition.RequiredBuildingId is not null:
                WorldPlayerBuildingDefinition building = GameModel.Definitions.GetPlayerBuilding(definition.RequiredBuildingId.Value);
                return Tr("Requires {0}", GameModel.Texts.GetGlobalText(building.NameTextId).Text);

            default:
                return WorldCommandErrorFormatter.Format(error);
        }
    }
}
