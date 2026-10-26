using CommunityToolkit.Mvvm.ComponentModel;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class UnitPurchaseViewModel : AbstractGameScreenViewModel
{
    private readonly WorldFortificationId fortificationId;
    private readonly GridPosition position;

    public UnitPurchaseViewModel(
        GameModel gameModel,
        GameViewModel gameViewModel,
        WorldFortificationId fortificationId,
        GridPosition position)
        : base(gameModel, gameViewModel)
    {
        this.fortificationId = fortificationId;
        this.position = position;

        RefreshView();
    }

    [ObservableProperty]
    public partial IReadOnlyList<UnitPurchaseItemViewModel> Options { get; private set; } = [];

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

    public async Task PurchaseAsync(UnitPurchaseItemViewModel item)
    {
        if (await TryDispatchWorldActionAsync(new WorldPurchaseUnitCommand(fortificationId, item.UnitDefinitionId, position)))
        {
            CloseSelf();
        }
    }

    private IReadOnlyList<UnitPurchaseItemViewModel> BuildOptions()
    {
        WorldPlayer player = WorldState.Players[ViewPlayerId];
        WorldRaceDefinition race = GameModel.Definitions.GetRace(player.Race);

        List<UnitPurchaseItemViewModel> result = [];

        foreach (UnitDefinitionId unitDefinitionId in race.Units)
        {
            UnitDefinition definition = GameModel.Definitions.GetUnit(unitDefinitionId);
            string name = GameModel.Texts.GetGlobalText(definition.NameTextId).Text;
            string description = GameModel.Texts.GetGlobalText(definition.DescriptionTextId).Text;
            string? unavailableReason;

            WorldPurchaseUnitCommand command = new(fortificationId, unitDefinitionId, position);
            WorldCommandResult commandResult = GameModel.World.DryRunCommand(command);

            if (commandResult.Failed && commandResult.Error.Value != WorldCommandError.InsufficientResources)
            {
                unavailableReason = FormatUnavailability(commandResult.Error.Value, definition);
            }
            else
            {
                unavailableReason = null;
            }

            WorldUnit unit = new(UnitDetailsViewModel.PreviewUnitId, unitDefinitionId, definition.MaxHitpoints, Experience: 0, definition.Level, [], null, false);
            UnitDetailsViewModel details = UnitDetailsViewModel.CreateFromWorld(unit, null, GameModel.Definitions, GameModel.Texts, GameModel.Stats);

            result.Add(new UnitPurchaseItemViewModel(
                this,
                unitDefinitionId,
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
                return Tr("Requires two free slots");

            case WorldCommandError.UnitMissingPrerequisite when definition.RequiredBuildingId is not null:
                WorldPlayerBuildingDefinition building = GameModel.Definitions.GetPlayerBuilding(definition.RequiredBuildingId.Value);
                return Tr("Requires {0}", GameModel.Texts.GetGlobalText(building.NameTextId).Text);

            default:
                return WorldCommandErrorFormatter.Format(error);
        }
    }
}
