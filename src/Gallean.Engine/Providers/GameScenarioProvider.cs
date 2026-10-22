using System.Collections.Immutable;

namespace Gallean.Engine;

internal sealed class GameScenarioProvider : IGameScenarioProvider
{
    private readonly ImmutableDictionary<ScenarioEventId, ScenarioEventDefinition> eventsById;
    private readonly ImmutableDictionary<ScenarioLocationId, ScenarioLocationDefinition> locationsById;
    private readonly ImmutableDictionary<ScenarioStackTemplateId, ScenarioStackTemplateDefinition> stackTemplatesById;
    private readonly ImmutableDictionary<ScenarioVariableId, ScenarioVariableDefinition> variablesById;

    public GameScenarioProvider(
        IEnumerable<ScenarioEventDefinition> events,
        IEnumerable<ScenarioLocationDefinition> locations,
        IEnumerable<ScenarioName> names,
        IEnumerable<ScenarioStackTemplateDefinition> stackTemplates,
        IEnumerable<ScenarioVariableDefinition> variables,
        ScenarioDefinition definition)
    {
        ImmutableArray<ScenarioEventDefinition> allEvents = [.. events.OrderBy(e => e.Order).ThenBy(e => e.Id)];
        Events = allEvents;
        eventsById = allEvents.ToImmutableDictionary(e => e.Id);

        ImmutableArray<ScenarioLocationDefinition> allLocations = [.. locations];
        Locations = allLocations;
        locationsById = allLocations.ToImmutableDictionary(l => l.Id);

        Names = [.. names];

        ImmutableArray<ScenarioStackTemplateDefinition> allStackTemplates = [.. stackTemplates];
        StackTemplates = allStackTemplates;
        stackTemplatesById = allStackTemplates.ToImmutableDictionary(t => t.Id);

        ImmutableArray<ScenarioVariableDefinition> allVariables = [.. variables];
        Variables = allVariables;
        variablesById = allVariables.ToImmutableDictionary(v => v.Id);

        Definition = definition;
    }

    public static GameScenarioProvider Empty { get; } = new(
        [],
        [],
        [],
        [],
        [],
        new ScenarioDefinition(
            new ScenarioId(string.Empty),
            new ScenarioInfoTexts(
                new ScenarioTextId(string.Empty),
                Description: null,
                new ScenarioTextId(string.Empty),
                Briefing: null,
                new ScenarioTextId(string.Empty),
                new ScenarioTextId(string.Empty),
                Creator: null),
            SuggestedLevel: 0,
            MaxLeaderLevel: 0,
            MaxCityLevel: 0,
            NextScenarioId: null));

    public IReadOnlyList<ScenarioEventDefinition> Events { get; }
    public IReadOnlyList<ScenarioLocationDefinition> Locations { get; }
    public IReadOnlyList<ScenarioName> Names { get; }
    public IReadOnlyList<ScenarioStackTemplateDefinition> StackTemplates { get; }
    public IReadOnlyList<ScenarioVariableDefinition> Variables { get; }
    public ScenarioDefinition Definition { get; }

    public ScenarioEventDefinition GetEvent(ScenarioEventId id)
    {
        if (eventsById.TryGetValue(id, out ScenarioEventDefinition? definition))
        {
            return definition;
        }

        throw new KeyNotFoundException($"Scenario event '{id}' was not found.");
    }

    public ScenarioLocationDefinition GetLocation(ScenarioLocationId id)
    {
        if (locationsById.TryGetValue(id, out ScenarioLocationDefinition? definition))
        {
            return definition;
        }

        throw new KeyNotFoundException($"Scenario location '{id}' was not found.");
    }

    public ScenarioStackTemplateDefinition GetStackTemplate(ScenarioStackTemplateId id)
    {
        if (stackTemplatesById.TryGetValue(id, out ScenarioStackTemplateDefinition? definition))
        {
            return definition;
        }

        throw new KeyNotFoundException($"Scenario stack template '{id}' was not found.");
    }

    public ScenarioVariableDefinition GetVariable(ScenarioVariableId id)
    {
        if (variablesById.TryGetValue(id, out ScenarioVariableDefinition? definition))
        {
            return definition;
        }

        throw new KeyNotFoundException($"Scenario variable '{id}' was not found.");
    }
}
