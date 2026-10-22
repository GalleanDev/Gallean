using Gallean.Common;

namespace Gallean.Engine.Tests;

internal sealed class TestBattleEngineBuilder
{
    private readonly List<(BattleUnit Unit, GridPosition Position)> attackerUnits = [];
    private readonly List<(BattleUnit Unit, GridPosition Position)> defenderUnits = [];
    private readonly TestGameDefinitionProviderBuilder definitionsBuilder = new();
    private readonly List<BattleItem> items = [];
    private BattleModifiers modifiers = BattleModifiers.None;
    private GlobalVariables globals = new() { BattleInitiativeVariance = 0, BattleDamageVariance = 0 };
    private IRandomProvider random = new TestSeedRandomProvider();
    private int roundNumber = 1;
    private IBattleUnitUpgradeResolver upgradeResolver = NullBattleUnitUpgradeResolver.Instance;

    public TestBattleEngineBuilder WithUpgradeResolver(IBattleUnitUpgradeResolver newUpgradeResolver)
    {
        upgradeResolver = newUpgradeResolver;
        return this;
    }

    public TestBattleEngineBuilder WithModifiers(BattleModifiers newModifiers)
    {
        modifiers = newModifiers;
        return this;
    }

    public TestBattleEngineBuilder WithDefinition(UnitDefinition definition)
    {
        definitionsBuilder.AddUnit(definition);
        return this;
    }

    public TestBattleEngineBuilder WithDefinition(UnitAttackDefinition definition)
    {
        definitionsBuilder.AddAttack(definition);
        return this;
    }

    public TestBattleEngineBuilder WithDefinition(UnitModifierDefinition definition)
    {
        definitionsBuilder.AddUnitModifier(definition);
        return this;
    }

    public TestBattleEngineBuilder WithDefinition(UnitDynamicUpgradeDefinition definition)
    {
        definitionsBuilder.AddUnitDynamicUpgrade(definition);
        return this;
    }

    public TestBattleEngineBuilder WithDefinition(ItemDefinition definition)
    {
        definitionsBuilder.AddItem(definition);
        return this;
    }

    public TestBattleEngineBuilder WithDefinition(WorldRaceDefinition definition)
    {
        definitionsBuilder.AddRace(definition);
        return this;
    }

    public TestBattleEngineBuilder WithItem(BattleItem item)
    {
        items.Add(item);
        return this;
    }

    public TestBattleEngineBuilder WithGlobals(GlobalVariables newGlobals)
    {
        globals = newGlobals;
        return this;
    }

    public TestBattleEngineBuilder WithRandom(IRandomProvider newRandom)
    {
        random = newRandom;
        return this;
    }

    public TestBattleEngineBuilder WithRoundNumber(int newRoundNumber)
    {
        roundNumber = newRoundNumber;
        return this;
    }

    public TestBattleEngineBuilder WithAttacker(BattleUnit unit, GridPosition position)
    {
        attackerUnits.Add((unit, position));
        return this;
    }

    public TestBattleEngineBuilder WithDefender(BattleUnit unit, GridPosition position)
    {
        defenderUnits.Add((unit, position));
        return this;
    }

    public BattleEngine Build()
    {
        IGameDefinitionProvider definitions = definitionsBuilder.Build();
        BattleStateBuilder stateBuilder = new(definitions);

        foreach ((BattleUnit unit, GridPosition position) in attackerUnits)
        {
            stateBuilder.Add(unit, position);
        }

        foreach ((BattleUnit unit, GridPosition position) in defenderUnits)
        {
            stateBuilder.Add(unit, position);
        }

        foreach (BattleItem item in items)
        {
            stateBuilder.AddItem(item);
        }

        BattleState state = stateBuilder.Build(modifiers) with { CurrentRound = BattleRound.Normal(roundNumber) };
        BattleEngine engine = new(
            new EngineContext(random, definitions, new UnitStatsProvider(definitions, globals, GameScenarioProvider.Empty.Definition), GameScenarioProvider.Empty, globals),
            state,
            upgradeResolver);

        engine.DispatchCommand(new BattleStartCommand());

        return engine;
    }
}
