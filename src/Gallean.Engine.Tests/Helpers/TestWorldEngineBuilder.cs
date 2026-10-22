using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine.Tests;

internal sealed class TestWorldEngineBuilder
{
    private readonly Dictionary<WorldPlayerId, WorldAiMemory> aiMemories = [];
    private readonly Dictionary<WorldBagId, WorldBag> bags = [];
    private readonly TestGameDefinitionProviderBuilder definitionsBuilder = new();
    private readonly Dictionary<WorldFortificationId, WorldFortification> fortifications = [];
    private readonly Dictionary<WorldLandmarkId, WorldLandmark> landmarks = [];
    private readonly Dictionary<WorldMountainId, WorldMountain> mountains = [];
    private readonly Dictionary<WorldPlayerId, WorldPlayer> players = [];
    private readonly Dictionary<WorldRelationId, WorldRelation> relations = [];
    private readonly Dictionary<WorldResourceNodeId, WorldResourceNode> resourceNodes = [];
    private readonly Dictionary<WorldRodId, WorldRod> rods = [];
    private readonly List<ScenarioEventDefinition> scenarioEvents = [];
    private readonly List<ScenarioLocationDefinition> scenarioLocations = [];
    private readonly List<ScenarioStackTemplateDefinition> scenarioStackTemplates = [];
    private readonly List<ScenarioVariableDefinition> scenarioVariables = [];
    private readonly Dictionary<WorldSiteId, WorldSite> sites = [];
    private readonly Dictionary<WorldStackId, WorldStack> stacks = [];
    private readonly Dictionary<WorldSubraceId, WorldSubrace> subraces = [];
    private readonly Dictionary<WorldTombId, WorldTomb> tombs = [];
    private WorldDifficulty difficulty = WorldDifficulty.Average;
    private GlobalVariables globals = new();
    private WorldMap? map;
    private int maxCityLevel = 5;
    private int maxLeaderLevel = 99;
    private IRandomProvider random = new TestSeedRandomProvider();
    private ScenarioId scenarioId = new("test-scenario");
    private WorldScenarioState scenarioState = WorldScenarioState.Empty;
    private int suggestedLevel;
    private WorldTurnInfo? turnInfo;

    private static ScenarioInfoTexts MakeTexts(ScenarioId scenarioId)
    {
        return new ScenarioInfoTexts(
            new ScenarioTextId($"{scenarioId}:NAME:0"),
            new ScenarioTextId($"{scenarioId}:DESC:0"),
            new ScenarioTextId($"{scenarioId}:BRIEFING:0"),
            new ScenarioTextId($"{scenarioId}:BRIEFLONG:0"),
            new ScenarioTextId($"{scenarioId}:DEBUNKW:0"),
            new ScenarioTextId($"{scenarioId}:DEBUNKL:0"),
            new ScenarioTextId($"{scenarioId}:CREATOR:0"));
    }

    public TestWorldEngineBuilder WithMap(WorldMap newMap)
    {
        map = newMap;
        return this;
    }

    public TestWorldEngineBuilder WithRandom(IRandomProvider newRandom)
    {
        random = newRandom;
        return this;
    }

    public TestWorldEngineBuilder WithGlobals(GlobalVariables newGlobals)
    {
        globals = newGlobals;
        return this;
    }

    public TestWorldEngineBuilder WithTurnInfo(WorldTurnInfo newTurnInfo)
    {
        turnInfo = newTurnInfo;
        return this;
    }

    public TestWorldEngineBuilder WithDifficulty(WorldDifficulty newDifficulty)
    {
        difficulty = newDifficulty;
        return this;
    }

    public TestWorldEngineBuilder WithScenarioId(ScenarioId newScenarioId)
    {
        scenarioId = newScenarioId;
        return this;
    }

    public TestWorldEngineBuilder WithSuggestedLevel(int newSuggestedLevel)
    {
        suggestedLevel = newSuggestedLevel;
        return this;
    }

    public TestWorldEngineBuilder WithMaxLeaderLevel(int newMaxLeaderLevel)
    {
        maxLeaderLevel = newMaxLeaderLevel;
        return this;
    }

    public TestWorldEngineBuilder WithMaxCityLevel(int newMaxCityLevel)
    {
        maxCityLevel = newMaxCityLevel;
        return this;
    }

    public TestWorldEngineBuilder WithPlayer(WorldPlayer player)
    {
        players[player.Id] = player;
        return this;
    }

    public TestWorldEngineBuilder WithRelation(WorldRelation relation)
    {
        relations[relation.Id] = relation;
        return this;
    }

    public TestWorldEngineBuilder WithStack(WorldStack stack)
    {
        stacks[stack.Id] = stack;
        return this;
    }

    public TestWorldEngineBuilder WithAiMemory(WorldPlayerId playerId, WorldAiMemory memory)
    {
        aiMemories[playerId] = memory;
        return this;
    }

    public TestWorldEngineBuilder WithSubrace(WorldSubrace subrace)
    {
        subraces[subrace.Id] = subrace;
        return this;
    }

    public TestWorldEngineBuilder WithCity(WorldCity city)
    {
        fortifications[city.Id] = city;
        return this;
    }

    public TestWorldEngineBuilder WithCapital(WorldCapital capital)
    {
        fortifications[capital.Id] = capital;

        return this;
    }

    public TestWorldEngineBuilder WithBag(WorldBag bag)
    {
        bags[bag.Id] = bag;

        return this;
    }

    public TestWorldEngineBuilder WithTomb(WorldTomb tomb)
    {
        tombs[tomb.Id] = tomb;

        return this;
    }

    public TestWorldEngineBuilder WithResourceNode(WorldResourceNode node)
    {
        resourceNodes[node.Id] = node;
        return this;
    }

    public TestWorldEngineBuilder WithRod(WorldRod rod)
    {
        rods[rod.Id] = rod;
        return this;
    }

    public TestWorldEngineBuilder WithScenarioEvent(ScenarioEventDefinition scenarioEvent)
    {
        scenarioEvents.Add(scenarioEvent);
        return this;
    }

    public TestWorldEngineBuilder WithScenarioLocation(ScenarioLocationDefinition location)
    {
        scenarioLocations.Add(location);
        return this;
    }

    public TestWorldEngineBuilder WithScenarioStackTemplate(ScenarioStackTemplateDefinition stackTemplate)
    {
        scenarioStackTemplates.Add(stackTemplate);
        return this;
    }

    public TestWorldEngineBuilder WithScenarioVariable(ScenarioVariableDefinition variable)
    {
        scenarioVariables.Add(variable);
        return this;
    }

    public TestWorldEngineBuilder WithScenarioState(WorldScenarioState newScenarioState)
    {
        scenarioState = newScenarioState;
        return this;
    }

    public TestWorldEngineBuilder WithRuin(WorldRuin ruin)
    {
        fortifications[ruin.Id] = ruin;
        return this;
    }

    public TestWorldEngineBuilder WithLandmark(WorldLandmark landmark)
    {
        landmarks[landmark.Id] = landmark;
        return this;
    }

    public TestWorldEngineBuilder WithMountain(WorldMountain mountain)
    {
        mountains[mountain.Id] = mountain;
        return this;
    }

    public TestWorldEngineBuilder WithSite(WorldSite site)
    {
        sites[site.Id] = site;
        return this;
    }

    public TestWorldEngineBuilder WithDefinition(UnitDefinition definition)
    {
        definitionsBuilder.AddUnit(definition);
        return this;
    }

    public TestWorldEngineBuilder WithDefinition(UnitAttackDefinition definition)
    {
        definitionsBuilder.AddAttack(definition);
        return this;
    }

    public TestWorldEngineBuilder WithUnitUpgrade(UnitUpgradeDefinition upgrade)
    {
        definitionsBuilder.AddUnitUpgrade(upgrade);
        return this;
    }

    public TestWorldEngineBuilder WithLeaderUpgrade(LeaderUpgradeDefinition upgrade)
    {
        definitionsBuilder.AddLeaderUpgrade(upgrade);
        return this;
    }

    public TestWorldEngineBuilder WithUnitDynamicUpgrade(UnitDynamicUpgradeDefinition dynamicUpgrade)
    {
        definitionsBuilder.AddUnitDynamicUpgrade(dynamicUpgrade);
        return this;
    }

    public TestWorldEngineBuilder WithDefinition(LordDefinition definition)
    {
        definitionsBuilder.AddLord(definition);
        return this;
    }

    public TestWorldEngineBuilder WithDefinition(UnitModifierDefinition definition)
    {
        definitionsBuilder.AddUnitModifier(definition);
        return this;
    }

    public TestWorldEngineBuilder WithDefinition(WorldPlayerBuildingDefinition definition)
    {
        definitionsBuilder.AddTechBuilding(definition);
        return this;
    }

    public TestWorldEngineBuilder WithDefinition(ItemDefinition definition)
    {
        definitionsBuilder.AddItem(definition);
        return this;
    }

    public TestWorldEngineBuilder WithDefinition(WorldCityDefinition definition)
    {
        definitionsBuilder.AddCity(definition);
        return this;
    }

    public TestWorldEngineBuilder WithDefinition(WorldLandmarkDefinition definition)
    {
        definitionsBuilder.AddLandmark(definition);
        return this;
    }

    public TestWorldEngineBuilder WithRace(WorldRaceDefinition race)
    {
        definitionsBuilder.AddRace(race);
        return this;
    }

    public TestWorldEngineBuilder WithDefinition(WorldSpellDefinition definition)
    {
        definitionsBuilder.AddSpell(definition);
        return this;
    }

    public TestWorldEngineBuilder WithDefinition(WorldThiefActionDefinition definition)
    {
        definitionsBuilder.AddThiefAction(definition);
        return this;
    }

    public TestWorldEngineBuilder WithResearchableSpell(WorldSpellResearchCost researchableSpell)
    {
        definitionsBuilder.AddResearchableSpell(researchableSpell);
        return this;
    }

    public WorldEngine Build()
    {
        (WorldEngine Engine, IEngineContext Context) built = BuildEngineWithContext();

        return built.Engine;
    }

    public IEngineContext BuildContext()
    {
        (WorldEngine Engine, IEngineContext Context) built = BuildEngineWithContext();

        return built.Context;
    }

    public (WorldEngine Engine, IEngineContext Context) BuildEngineWithContext()
    {
        if (map is null)
        {
            throw new InvalidOperationException("The map is not set.");
        }

        IGameDefinitionProvider definitions = definitionsBuilder.Build();

        ImmutableSortedDictionary<WorldPlayerId, WorldPlayer> sortedPlayers = players.ToImmutableSortedDictionary();

        WorldTurnInfo resolvedTurnInfo;
        if (turnInfo is not null)
        {
            resolvedTurnInfo = turnInfo;
        }
        else
        {
            resolvedTurnInfo = new WorldTurnInfo(1, sortedPlayers.Keys.First());
        }

        WorldState state = new(
            map,
            resolvedTurnInfo,
            difficulty,
            sortedPlayers,
            relations.ToImmutableSortedDictionary(),
            stacks.ToImmutableSortedDictionary(),
            fortifications.ToImmutableSortedDictionary(),
            resourceNodes.ToImmutableSortedDictionary(),
            rods.ToImmutableSortedDictionary(),
            bags.ToImmutableSortedDictionary(),
            tombs.ToImmutableSortedDictionary(),
            landmarks.ToImmutableSortedDictionary(),
            mountains.ToImmutableSortedDictionary(),
            sites.ToImmutableSortedDictionary(),
            subraces.ToImmutableSortedDictionary(),
            scenarioState,
            aiMemories.ToImmutableSortedDictionary());

        TestWorldStateValidator.Validate(state, definitions);

        ScenarioDefinition scenarioDefinition = new(scenarioId, MakeTexts(scenarioId), suggestedLevel, maxLeaderLevel, maxCityLevel, NextScenarioId: null);
        EngineContext context = new(random, definitions, new UnitStatsProvider(definitions, globals, scenarioDefinition), new GameScenarioProvider(scenarioEvents, scenarioLocations, [], scenarioStackTemplates, scenarioVariables, scenarioDefinition), globals);

        return (new WorldEngine(context, state), context);
    }

    public (GameSession Session, IEngineContext Context) BuildSession()
    {
        if (map is null)
        {
            throw new InvalidOperationException("The map is not set.");
        }

        IGameDefinitionProvider definitions = definitionsBuilder.Build();

        ImmutableSortedDictionary<WorldPlayerId, WorldPlayer> sortedPlayers = players.ToImmutableSortedDictionary();

        WorldTurnInfo resolvedTurnInfo;
        if (turnInfo is not null)
        {
            resolvedTurnInfo = turnInfo;
        }
        else
        {
            resolvedTurnInfo = new WorldTurnInfo(1, sortedPlayers.Keys.First());
        }

        WorldState state = new(
            map,
            resolvedTurnInfo,
            difficulty,
            sortedPlayers,
            relations.ToImmutableSortedDictionary(),
            stacks.ToImmutableSortedDictionary(),
            fortifications.ToImmutableSortedDictionary(),
            resourceNodes.ToImmutableSortedDictionary(),
            rods.ToImmutableSortedDictionary(),
            bags.ToImmutableSortedDictionary(),
            tombs.ToImmutableSortedDictionary(),
            landmarks.ToImmutableSortedDictionary(),
            mountains.ToImmutableSortedDictionary(),
            sites.ToImmutableSortedDictionary(),
            subraces.ToImmutableSortedDictionary(),
            scenarioState,
            aiMemories.ToImmutableSortedDictionary());

        TestWorldStateValidator.Validate(state, definitions);

        ScenarioDefinition scenarioDefinition = new(scenarioId, MakeTexts(scenarioId), suggestedLevel, maxLeaderLevel, maxCityLevel, NextScenarioId: null);
        EngineContext context = new(random, definitions, new UnitStatsProvider(definitions, globals, scenarioDefinition), new GameScenarioProvider(scenarioEvents, scenarioLocations, [], scenarioStackTemplates, scenarioVariables, scenarioDefinition), globals);
        GameSession session = new(context, state);

        return (session, context);
    }
}
