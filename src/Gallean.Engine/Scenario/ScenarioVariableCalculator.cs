namespace Gallean.Engine;

internal static class ScenarioVariableCalculator
{
    public static int GetValue(ScenarioVariableId variable, WorldState state, IGameScenarioProvider scenario)
    {
        if (state.ScenarioState.Variables.TryGetValue(variable, out int value))
        {
            return value;
        }
        else
        {
            return scenario.GetVariable(variable).InitialValue;
        }
    }

    public static bool IsInRange(
        ScenarioEventVariableInRangeCondition condition,
        WorldState state,
        IGameScenarioProvider scenario)
    {
        bool first = IsInRange(condition.Range, state, scenario);

        if (condition.Second is null)
        {
            return first;
        }

        bool second = IsInRange(condition.Second.Range, state, scenario);

        return condition.Second.Operator switch
        {
            ScenarioVariableOperator.And => first && second,
            ScenarioVariableOperator.Or => first || second
        };
    }

    public static int Modify(int value, ScenarioVariableOperation operation, int operand)
    {
        return operation switch
        {
            ScenarioVariableOperation.Add => value + operand,
            ScenarioVariableOperation.Subtract => value - operand,
            ScenarioVariableOperation.Multiply => value * operand,
            ScenarioVariableOperation.Divide => value / operand,
            ScenarioVariableOperation.Replace => operand
        };
    }

    private static bool IsInRange(ScenarioVariableRange range, WorldState state, IGameScenarioProvider scenario)
    {
        int value = GetValue(range.Variable, state, scenario);

        return value >= range.Minimum && value <= range.Maximum;
    }
}
