namespace Gallean.Engine.Tests;

internal static class TestWorldAiTraceFormatter
{
    public static string[] Format(IReadOnlyList<TestWorldAiRunner.Trace> traces)
    {
        List<string> lines = [];

        foreach (TestWorldAiRunner.Trace trace in traces)
        {
            lines.Add(Format(trace));
        }

        return [.. lines];
    }

    private static string Format(TestWorldAiRunner.Trace trace)
    {
        string command = trace.Plan.Command switch
        {
            WorldAiMoveStackCommand c => $"Move {c.StackId.Value} -> ({c.TargetPosition.X}, {c.TargetPosition.Y})",
            WorldAutoFitStackCommand c => $"AutoFit {c.StackId.Value}",
            WorldExitStackCommand c => $"Exit {c.StackId.Value}",
            WorldHealUnitCommand { Group: WorldUnitGroup.Stack stackGroup } c => $"Heal {stackGroup.StackId.Value} {c.UnitId.Value}",
            WorldPurchaseUnitCommand c => $"Purchase {c.UnitDefinitionId.Value} -> {c.FortificationId.Value} ({c.Position.Line}, {c.Position.Flank})",
            WorldPurchaseLeaderCommand c => $"Leader {c.LeaderId.Value} -> {c.FortificationId.Value}",
            WorldConstructBuildingCommand c => $"Build {c.BuildingId.Value}",
            WorldChangeStackOrderCommand c => $"Order {c.StackId.Value} -> {c.Order.GetType().Name}",
            WorldResearchSpellCommand c => $"Research {c.SpellId.Value}",
            WorldCastSpellCommand { Target: { } target } c => $"Cast {c.SpellId.Value} -> ({target.Center.X}, {target.Center.Y})",
            WorldCastSpellCommand { Target: null } c => $"Cast {c.SpellId.Value}",
            WorldThiefActionCommand { Action: WorldThiefSpyAction action } c => $"Spy {c.StackId.Value} -> ({action.TargetPosition.X}, {action.TargetPosition.Y})",
            WorldThiefActionCommand { Action: WorldThiefRiotAction action } c => $"Riot {c.StackId.Value} -> ({action.TargetPosition.X}, {action.TargetPosition.Y})",
            WorldCreateRodCommand c => $"Create rod {c.StackId.Value} -> ({c.TargetPosition.X}, {c.TargetPosition.Y})",
            WorldRemoveRodCommand c => $"Remove rod {c.StackId.Value} -> ({c.TargetPosition.X}, {c.TargetPosition.Y})",
            _ => throw new InvalidOperationException($"Unexpected AI command '{trace.Plan.Command}'.")
        };

        if (trace.Battle is { } outcome)
        {
            return $"[{trace.Plan.Score}] {command} ({outcome.WinningSide} wins)";
        }
        else
        {
            return $"[{trace.Plan.Score}] {command}";
        }
    }
}
