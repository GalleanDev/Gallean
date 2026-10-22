using Gallean.Common;

namespace Gallean.Engine;

internal sealed class SummonSpellResolver : IWorldSpellResolver<WorldSummonStackSpellEffect>, IStaticInstance<SummonSpellResolver>
{
    public WorldCommandResult Resolve(
        WorldState state,
        IEngineContext context,
        WorldPlayerId casterId,
        WorldSpellTarget? target,
        WorldSpellDefinition spell,
        WorldSummonStackSpellEffect effect)
    {
        if (target is null)
        {
            return WorldCommandResult.Fail(WorldCommandError.SpellTargetInvalid);
        }

        UnitDefinition unitDefinition = context.Definitions.GetUnit(effect.Unit);
        WorldPosition? tile = SpellTargetCalculator.FindSummonTarget(state, target, unitDefinition);

        if (tile is null)
        {
            WorldMovementCalculator.MovementProfile movementProfile =
                new(unitDefinition.MoveAbility, unitDefinition.IsWaterOnly, IsLeaderDead: false, IsThief: false);

            if (state.Map.IsValidPosition(target.Center) &&
                WorldMovementCalculator.IsPassable(state.Map, target.Center, movementProfile))
            {
                return WorldCommandResult.Fail(WorldCommandError.TargetOccupied);
            }
            else
            {
                return WorldCommandResult.Fail(WorldCommandError.SpellTargetInvalid);
            }
        }

        WorldStackBuilder stackBuilder = new WorldStackBuilder(context.Definitions)
            .PlaceUnit(
                new WorldUnit(
                    WorldCommandHelpers.GenerateUnitIds(state).First(),
                    effect.Unit,
                    unitDefinition.MaxHitpoints,
                    0,
                    unitDefinition.Level,
                    [],
                    Name: null,
                    IsTypeLocked: false),
                GridPosition.FrontCenter);

        if (!stackBuilder.TryBuild(out UnitGrid<WorldUnit>? units, out WorldCommandError? error))
        {
            return WorldCommandResult.Fail(error.Value);
        }

        int? expirationTurn;

        if (unitDefinition.Lifetime is not null)
        {
            expirationTurn = state.TurnInfo.TurnNumber + unitDefinition.Lifetime.Value;
        }
        else
        {
            expirationTurn = null;
        }

        WorldStack stack = new(
            WorldCommandHelpers.GenerateStackId(state),
            casterId,
            tile.Value,
            WorldStackDirection.South,
            units,
            unitDefinition.MovementPoints ?? 0,
            Inventory: WorldInventory.Empty,
            LeaderEquipment.Empty,
            WorldCommandHelpers.GetPlayerSubraceId(state, casterId),
            context.Globals.AiDefaultPriority,
            AiIgnore: false,
            Concealed: false,
            WorldStackOrder.Normal.Instance,
            expirationTurn,
            PendingLeaderUpgrades: 0,
            BattlesWon: 0,
            SourceTemplateId: null);

        return WorldCommandResult.Success(
            Empty.Value,
            [
                new WorldStackCreatedEvent(stack, WorldResourceSet.Empty),
                new WorldScenarioCheckEvent(new ScenarioEventStackCreatedTrigger(stack.Id), [])
            ]);
    }
}
