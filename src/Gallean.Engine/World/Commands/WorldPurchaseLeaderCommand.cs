using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldPurchaseLeaderCommand(
    WorldFortificationId FortificationId,
    UnitDefinitionId LeaderId) : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        WorldPlayer player = state.CurrentPlayer;

        if (!WorldCommandHelpers.TryGetCurrentPlayerFortification(state, FortificationId, out WorldFortification? fortification))
        {
            return WorldCommandResult.Fail(WorldCommandError.FortificationNotFound);
        }

        if (fortification is not (WorldCity or WorldCapital))
        {
            return WorldCommandResult.Fail(WorldCommandError.FortificationNotFound);
        }

        if (FortificationCalculator.IsRioting(fortification))
        {
            return WorldCommandResult.Fail(WorldCommandError.CityRioting);
        }

        WorldRaceDefinition raceDefinition = context.Definitions.GetRace(player.Race);

        if (!raceDefinition.Leaders.Contains(LeaderId))
        {
            return WorldCommandResult.Fail(WorldCommandError.UnitNotAvailable);
        }

        UnitDefinition leaderDefinition = context.Definitions.GetUnit(LeaderId);

        if (leaderDefinition.RequiredBuildingId is not null &&
            !player.Buildings.Acquired.Contains(leaderDefinition.RequiredBuildingId.Value))
        {
            return WorldCommandResult.Fail(WorldCommandError.UnitMissingPrerequisite);
        }

        if (leaderDefinition.Category == UnitCategory.Thief &&
            !PlayerBuildingsCalculator.IsAcquired(player.Buildings, WorldPlayerBuildingCategory.Guild, context.Definitions))
        {
            return WorldCommandResult.Fail(WorldCommandError.GuildBuildingRequired);
        }

        if (!ResourceSetCalculator.CanAfford(player.Resources, leaderDefinition.PurchaseCost))
        {
            return WorldCommandResult.Fail(WorldCommandError.InsufficientResources);
        }

        WorldPlan plan = WorldPlan.For(state);

        if (plan.GetStackAt(fortification.Entrance) is not null)
        {
            return WorldCommandResult.Fail(WorldCommandError.TargetOccupied);
        }

        string? leaderName = LeaderNameCalculator.TryGetName(leaderDefinition, context.Definitions, context.Random);

        WorldStackBuilder stackBuilder = new WorldStackBuilder(context.Definitions)
            .PlaceUnit(
                new WorldUnit(
                    WorldCommandHelpers.GenerateUnitIds(state).First(),
                    LeaderId,
                    leaderDefinition.MaxHitpoints,
                    0,
                    leaderDefinition.Level,
                    [],
                    leaderName,
                    IsTypeLocked: false),
                GridPosition.FrontCenter);

        if (!stackBuilder.TryBuild(out UnitGrid<WorldUnit>? units, out WorldCommandError? stackError))
        {
            return WorldCommandResult.Fail(stackError.Value);
        }

        WorldStack stack = new(
            WorldCommandHelpers.GenerateStackId(state),
            player.Id,
            fortification.Entrance,
            WorldStackDirection.South,
            units,
            leaderDefinition.MovementPoints ?? 0,
            Inventory: WorldInventory.Empty,
            LeaderEquipment.Empty,
            WorldCommandHelpers.GetPlayerSubraceId(state, player.Id),
            context.Globals.AiDefaultPriority,
            AiIgnore: false,
            Concealed: false,
            WorldStackOrder.Normal.Instance,
            ExpirationTurn: null,
            PendingLeaderUpgrades: 0,
            BattlesWon: 0,
            SourceTemplateId: null);

        return WorldCommandResult.Success(
            Empty.Value,
            [new WorldStackCreatedEvent(stack, leaderDefinition.PurchaseCost)]);
    }
}
