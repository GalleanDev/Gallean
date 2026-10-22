using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldResolveFortificationBattleCommand(
    WorldStackId AttackerStackId,
    WorldFortificationId DefenderFortificationId,
    BattleState FinalBattleState) : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        if (!state.Stacks.TryGetValue(AttackerStackId, out WorldStack? attackerStack))
        {
            return WorldCommandResult.Fail(WorldCommandError.StackNotFound);
        }

        if (!state.Fortifications.ContainsKey(DefenderFortificationId))
        {
            return WorldCommandResult.Fail(WorldCommandError.FortificationNotFound);
        }

        if (FinalBattleState.Outcome is null)
        {
            return WorldCommandResult.Fail(WorldCommandError.BattleNotFinished);
        }

        UnitGrid<WorldUnit> survivingAttackerUnits = WorldCommandHelpers.BuildSurvivingGrid(
            FinalBattleState,
            BattleSide.Attacker,
            duelStack: null,
            context);
        UnitGrid<WorldUnit> survivingDefenderUnits = WorldCommandHelpers.BuildSurvivingGrid(
            FinalBattleState,
            BattleSide.Defender,
            duelStack: null,
            context);

        WorldFortificationBattleEndedEvent endedEvent = new(
            AttackerStackId,
            DefenderFortificationId,
            FinalBattleState.Outcome.WinningSide,
            survivingAttackerUnits,
            survivingDefenderUnits);

        List<WorldEvent> events =
        [
            .. WorldCommandHelpers.CreateBattleItemEvents(FinalBattleState, BattleSide.Attacker, attackerStack),
            endedEvent,
            .. LeaderUpgradeCalculator.CreateUpgradeEvents(attackerStack, survivingAttackerUnits, context.Definitions, context.Globals)
        ];

        return WorldCommandResult.Success(Empty.Value, [.. events]);
    }
}
