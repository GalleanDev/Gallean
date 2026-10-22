using Gallean.Common;

namespace Gallean.Engine;

public sealed record WorldResolveStackBattleCommand(
    WorldStackId AttackerStackId,
    WorldStackId DefenderStackId,
    BattleState FinalBattleState,
    bool IsDuel) : IWorldCommand<Empty>
{
    public WorldCommandResult Execute(WorldState state, IEngineContext context)
    {
        if (!state.Stacks.TryGetValue(AttackerStackId, out WorldStack? attackerStack))
        {
            return WorldCommandResult.Fail(WorldCommandError.StackNotFound);
        }

        if (!state.Stacks.TryGetValue(DefenderStackId, out WorldStack? defenderStack))
        {
            return WorldCommandResult.Fail(WorldCommandError.StackNotFound);
        }

        if (FinalBattleState.Outcome is null)
        {
            return WorldCommandResult.Fail(WorldCommandError.BattleNotFinished);
        }

        UnitGrid<WorldUnit> attackerUnits = WorldCommandHelpers.BuildSurvivingGrid(
            FinalBattleState,
            BattleSide.Attacker,
            IsDuel ? attackerStack : null,
            context);
        UnitGrid<WorldUnit> defenderUnits = WorldCommandHelpers.BuildSurvivingGrid(
            FinalBattleState,
            BattleSide.Defender,
            IsDuel ? defenderStack : null,
            context);

        WorldStackBattleEndedEvent endedEvent = new(
            AttackerStackId,
            DefenderStackId,
            FinalBattleState.Outcome.WinningSide,
            attackerUnits,
            defenderUnits);

        List<WorldEvent> events =
        [
            .. WorldCommandHelpers.CreateBattleItemEvents(FinalBattleState, BattleSide.Attacker, attackerStack),
            .. WorldCommandHelpers.CreateBattleItemEvents(FinalBattleState, BattleSide.Defender, defenderStack),
            endedEvent,
            .. LeaderUpgradeCalculator.CreateUpgradeEvents(attackerStack, attackerUnits, context.Definitions, context.Globals),
            .. LeaderUpgradeCalculator.CreateUpgradeEvents(defenderStack, defenderUnits, context.Definitions, context.Globals)
        ];

        return WorldCommandResult.Success(Empty.Value, [.. events]);
    }
}
