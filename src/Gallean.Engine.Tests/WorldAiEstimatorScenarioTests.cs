using Gallean.Entities.GameData;

namespace Gallean.Engine.Tests;

public sealed class WorldAiEstimatorScenarioTests
{
    private sealed record ScenarioFixture(WorldState State, IEngineContext Context, WorldAiEstimator Estimator);

    private static readonly string[] ScenarioIds = ["S095SC0000", "S099SC0000"];

    private readonly Dictionary<string, ScenarioFixture> scenarios = [];

    [OneTimeSetUp]
    public void SetUp()
    {
        GlobalDatabase globalDatabase = GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath);
        ScenarioDatabase scenarioDatabase = ScenarioDatabaseLoader.Load(RiseOfTheElves.ScenarioDatabasePath);
        CampaignDatabase campaignDatabase = CampaignDatabaseLoader.Load(RiseOfTheElves.CampaignDatabasePath);
        IGameDefinitionProvider definitions = new GameDefinitionProvider(globalDatabase);

        foreach (string scenarioId in ScenarioIds)
        {
            GlobalVariables globals = new();
            Scenario scenario = ScenarioLoader.Load(
                globalDatabase,
                scenarioDatabase,
                campaignDatabase,
                new ScenarioId(scenarioId),
                definitions,
                globals);
            IEngineContext context = new EngineContext(
                new FixedRandomProvider(),
                definitions,
                new UnitStatsProvider(definitions, globals, scenario.Provider.Definition),
                scenario.Provider,
                globals);

            scenarios.Add(scenarioId, new ScenarioFixture(scenario.State, context, new WorldAiEstimator(context)));
        }
    }

    private static string FormatBattle(ScenarioFixture fixture, string attackerId, string defenderId, BattleState battleState)
    {
        WorldAiEstimator.PowerEstimate powerEstimate = fixture.Estimator.EstimatePower(battleState);
        WorldAiEstimator.BattleEstimate battleEstimate = fixture.Estimator.EstimateBattle(battleState);
        string leader = battleEstimate.IsLeaderAlive ? "leader alive" : "leader dead";

        return $"{attackerId} ({powerEstimate.Attacker}) vs {defenderId} ({powerEstimate.Defender}): {battleEstimate.Outcome.WinningSide} wins, {leader}";
    }

    private string FormatStackBattle(string scenarioId, string attackerId, string defenderId)
    {
        ScenarioFixture fixture = scenarios[scenarioId];
        WorldStack attacker = fixture.State.Stacks[new WorldStackId(attackerId)];
        WorldStack defender = fixture.State.Stacks[new WorldStackId(defenderId)];
        BattleState battleState = WorldCommandHelpers.CreateStackBattleState(attacker, defender, BattleModifiers.None, fixture.Context);

        return FormatBattle(fixture, attackerId, defenderId, battleState);
    }

    private string FormatFortificationBattle(string scenarioId, string attackerId, string fortificationId)
    {
        ScenarioFixture fixture = scenarios[scenarioId];
        WorldStack attacker = fixture.State.Stacks[new WorldStackId(attackerId)];
        WorldFortification defender = fixture.State.Fortifications[new WorldFortificationId(fortificationId)];
        int fortificationArmor = FortificationCalculator.GetArmorBonus(defender, fixture.Context.Globals);
        BattleState battleState = WorldCommandHelpers.CreateGarrisonBattleState(
            attacker,
            defender.Garrison,
            fortificationArmor,
            BattleModifiers.None,
            fixture.Context);

        return FormatBattle(fixture, attackerId, fortificationId, battleState);
    }

    [Test]
    public void StackBattlesTest()
    {
        Assert.That(
            FormatStackBattle("S095SC0000", "S095KC0003", "S095KC001e"),
            Is.EqualTo("S095KC0003 (397) vs S095KC001e (275): Attacker wins, leader alive"));
        Assert.That(
            FormatStackBattle("S095SC0000", "S095KC0027", "S095KC001e"),
            Is.EqualTo("S095KC0027 (647) vs S095KC001e (316): Attacker wins, leader alive"));
        Assert.That(
            FormatStackBattle("S095SC0000", "S095KC0017", "S095KC0000"),
            Is.EqualTo("S095KC0017 (63) vs S095KC0000 (77): Defender wins, leader dead"));
        Assert.That(
            FormatStackBattle("S095SC0000", "S095KC0000", "S095KC0011"),
            Is.EqualTo("S095KC0000 (77) vs S095KC0011 (92): Attacker wins, leader alive"));
        Assert.That(
            FormatStackBattle("S095SC0000", "S095KC0011", "S095KC0000"),
            Is.EqualTo("S095KC0011 (92) vs S095KC0000 (77): Defender wins, leader dead"));
        Assert.That(
            FormatStackBattle("S095SC0000", "S095KC001e", "S095KC0007"),
            Is.EqualTo("S095KC001e (316) vs S095KC0007 (290): Attacker wins, leader dead"));
        Assert.That(
            FormatStackBattle("S099SC0000", "S099KC0074", "S099KC0004"),
            Is.EqualTo("S099KC0074 (425) vs S099KC0004 (468): Defender wins, leader dead"));
        Assert.That(
            FormatStackBattle("S099SC0000", "S099KC0074", "S099KC005d"),
            Is.EqualTo("S099KC0074 (449) vs S099KC005d (680): Attacker wins, leader alive"));
        Assert.That(
            FormatStackBattle("S099SC0000", "S099KC005d", "S099KC0074"),
            Is.EqualTo("S099KC005d (680) vs S099KC0074 (449): Defender wins, leader dead"));
        Assert.That(
            FormatStackBattle("S099SC0000", "S099KC0079", "S099KC0041"),
            Is.EqualTo("S099KC0079 (206) vs S099KC0041 (295): Attacker wins, leader alive"));
        Assert.That(
            FormatStackBattle("S099SC0000", "S099KC0076", "S099KC005f"),
            Is.EqualTo("S099KC0076 (144) vs S099KC005f (305): Attacker wins, leader alive"));
    }

    [Test]
    public void RuinBattleTest()
    {
        Assert.That(
            FormatFortificationBattle("S099SC0000", "S099KC005d", "S099RU0000"),
            Is.EqualTo("S099KC005d (732) vs S099RU0000 (533): Defender wins, leader dead"));
    }
}
