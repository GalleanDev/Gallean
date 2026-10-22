namespace Gallean.Engine;

public interface IGameScenarioProvider
{
    IReadOnlyList<ScenarioEventDefinition> Events { get; }
    IReadOnlyList<ScenarioLocationDefinition> Locations { get; }
    IReadOnlyList<ScenarioName> Names { get; }
    IReadOnlyList<ScenarioStackTemplateDefinition> StackTemplates { get; }
    IReadOnlyList<ScenarioVariableDefinition> Variables { get; }
    ScenarioDefinition Definition { get; }

    ScenarioEventDefinition GetEvent(ScenarioEventId id);
    ScenarioLocationDefinition GetLocation(ScenarioLocationId id);
    ScenarioStackTemplateDefinition GetStackTemplate(ScenarioStackTemplateId id);
    ScenarioVariableDefinition GetVariable(ScenarioVariableId id);
}
