using System.Collections.Immutable;
using Gallean.Entities.GameData;
using Gallean.Tests.Common;

namespace Gallean.Engine.Tests;

public sealed class WorldObjectiveTests
{
    private readonly GlobalVariables globals = new();
    private CampaignDatabase campaignDb = null!;
    private IGameDefinitionProvider definitions = null!;
    private GlobalDatabase globalDb = null!;
    private ScenarioDatabase scenarioDb = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        globalDb = GlobalDatabaseLoader.Load(RiseOfTheElves.GlobalDatabasePath);
        scenarioDb = ScenarioDatabaseLoader.Load(RiseOfTheElves.ScenarioDatabasePath);
        campaignDb = CampaignDatabaseLoader.Load(RiseOfTheElves.CampaignDatabasePath);
        definitions = new GameDefinitionProvider(globalDb);
    }

    private static string FormatTarget(WorldPlan plan, WorldObjective objective)
    {
        WorldPosition entrance = FortificationCalculator.GetEntrance(objective.Footprint);

        if (objective.StackId is not null)
        {
            return $"stack {objective.StackId.Value.Value}";
        }
        else if (plan.GetFortificationAt(entrance) is { } fortification)
        {
            return $"fortification {fortification.Id.Value}";
        }
        else if (plan.GetSiteAt(entrance) is { } site)
        {
            return $"site {site.Id.Value}";
        }
        else
        {
            return "zone";
        }
    }

    private static IReadOnlyList<string> FormatObjectives(WorldState state, ImmutableArray<WorldObjective> objectives)
    {
        WorldPlan plan = WorldPlan.For(state);
        List<string> rows = [];

        foreach (WorldObjective objective in objectives)
        {
            WorldFootprint footprint = objective.Footprint;

            rows.Add(
                $"{FormatTarget(plan, objective)} at " +
                $"({footprint.TopLeft.X},{footprint.TopLeft.Y}) {footprint.Width}x{footprint.Height}");
        }

        return rows;
    }

    private static WorldPlayerId GetPlayer(WorldState state, WorldRace race)
    {
        foreach (WorldPlayer player in state.Players.Values)
        {
            if (player.Race == race)
            {
                return player.Id;
            }
        }

        throw new InvalidOperationException($"No player of race {race}.");
    }

    private Scenario LoadScenario(string id)
    {
        return ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, new ScenarioId(id), definitions, globals);
    }

    [Test]
    public void NecromancersSpiritTest()
    {
        Scenario scenario = LoadScenario("S107SC0000");
        WorldState state = scenario.State;
        WorldPlan plan = WorldPlan.For(state);

        ImmutableArray<WorldObjective> objectives =
            plan.GetObjectives(scenario.Provider, GetPlayer(state, WorldRace.Human));

        Assert.That(FormatObjectives(state, objectives), Is.EqualTo(
        [
            "stack S107KC0020 at (42,23) 4x4"
        ]));
    }

    [Test]
    public void BethrezensRetreatTest()
    {
        Scenario scenario = LoadScenario("S101SC0000");
        WorldState state = scenario.State;
        WorldPlan plan = WorldPlan.For(state);

        ImmutableArray<WorldObjective> objectives =
            plan.GetObjectives(scenario.Provider, GetPlayer(state, WorldRace.Heretic));

        Assert.That(FormatObjectives(state, objectives), Is.EqualTo(
        [
            "zone at (29,1) 1x1",
            "stack S101KC0013 at (35,21) 1x1"
        ]));
    }

    [Test]
    public void LostRunesTest()
    {
        Scenario scenario = LoadScenario("S116SC0000");
        WorldState state = scenario.State;
        WorldPlan plan = WorldPlan.For(state);

        ImmutableArray<WorldObjective> objectives =
            plan.GetObjectives(scenario.Provider, GetPlayer(state, WorldRace.Dwarf));

        Assert.That(FormatObjectives(state, objectives), Is.EqualTo(
        [
            "fortification S116RU0004 at (17,1) 3x3",
            "fortification S116RU0001 at (44,30) 3x3",
            "fortification S116RU0002 at (8,44) 3x3"
        ]));
    }

    [Test]
    public void NoWinningEventTest()
    {
        Scenario scenario = LoadScenario("S107SC0000");
        WorldState state = scenario.State;
        WorldPlan plan = WorldPlan.For(state);

        ImmutableArray<WorldObjective> objectives =
            plan.GetObjectives(scenario.Provider, GetPlayer(state, WorldRace.Neutral));

        Assert.That(FormatObjectives(state, objectives), Is.Empty);
    }

    [Test]
    public void TriggeredEventTest()
    {
        Scenario scenario = LoadScenario("S107SC0000");
        WorldState state = scenario.State with
        {
            ScenarioState = scenario.State.ScenarioState with
            {
                EventStates = scenario.State.ScenarioState.EventStates.SetItem(new ScenarioEventId("S107EV0014"), false)
            }
        };

        WorldPlan plan = WorldPlan.For(state);
        ImmutableArray<WorldObjective> objectives = plan.GetObjectives(scenario.Provider, GetPlayer(state, WorldRace.Human));

        Assert.That(FormatObjectives(state, objectives), Is.Empty);
    }

    [Test]
    public void DisabledEventTest()
    {
        Scenario scenario = LoadScenario("S107SC0000");
        WorldState state = scenario.State with
        {
            ScenarioState = scenario.State.ScenarioState with
            {
                EventStates = scenario.State.ScenarioState.EventStates
                    .SetItem(new ScenarioEventId("S107EV0014"), false)
            }
        };

        WorldPlan plan = WorldPlan.For(state);
        ImmutableArray<WorldObjective> objectives = plan.GetObjectives(scenario.Provider, GetPlayer(state, WorldRace.Human));

        Assert.That(FormatObjectives(state, objectives), Is.Empty);
    }

    [Test]
    public void DestroyedTargetTest()
    {
        Scenario scenario = LoadScenario("S107SC0000");
        WorldState state = scenario.State with
        {
            Stacks = scenario.State.Stacks.Remove(new WorldStackId("S107KC0020"))
        };

        WorldPlan plan = WorldPlan.For(state);
        ImmutableArray<WorldObjective> objectives = plan.GetObjectives(scenario.Provider, GetPlayer(state, WorldRace.Human));

        Assert.That(FormatObjectives(state, objectives), Is.Empty);
    }

    [Test]
    public void LootedRuinTest()
    {
        Scenario scenario = LoadScenario("S116SC0000");
        WorldFortificationId ruinId = new("S116RU0004");
        WorldRuin ruin = Assert.IsOfType<WorldRuin>(scenario.State.Fortifications[ruinId]);
        WorldPlayerId playerId = GetPlayer(scenario.State, WorldRace.Dwarf);
        WorldState state = scenario.State with
        {
            Fortifications = scenario.State.Fortifications.SetItem(ruinId, ruin with { LooterId = playerId })
        };

        WorldPlan plan = WorldPlan.For(state);
        ImmutableArray<WorldObjective> objectives = plan.GetObjectives(scenario.Provider, playerId);

        Assert.That(FormatObjectives(state, objectives), Is.EqualTo(
        [
            "fortification S116RU0001 at (44,30) 3x3",
            "fortification S116RU0002 at (8,44) 3x3"
        ]));
    }

    [Test]
    public void ConcealedEnemyStackTest()
    {
        Scenario scenario = LoadScenario("S101SC0000");
        WorldStackId stackId = new("S101KC0013");
        WorldStack stack = scenario.State.Stacks[stackId];
        WorldState state = scenario.State with
        {
            Stacks = scenario.State.Stacks.SetItem(
                stackId,
                stack with { OwnerId = GetPlayer(scenario.State, WorldRace.Undead), Concealed = true })
        };

        WorldPlan plan = WorldPlan.For(state);
        ImmutableArray<WorldObjective> objectives = plan.GetObjectives(scenario.Provider, GetPlayer(state, WorldRace.Heretic));

        Assert.That(FormatObjectives(state, objectives), Is.EqualTo(
        [
            "zone at (29,1) 1x1"
        ]));
    }

    [Test]
    public void ConcealedOwnStackTest()
    {
        Scenario scenario = LoadScenario("S101SC0000");
        WorldStackId stackId = new("S101KC0013");
        WorldStack stack = scenario.State.Stacks[stackId];
        WorldState state = scenario.State with
        {
            Stacks = scenario.State.Stacks.SetItem(stackId, stack with { Concealed = true })
        };

        WorldPlan plan = WorldPlan.For(state);
        ImmutableArray<WorldObjective> objectives = plan.GetObjectives(scenario.Provider, GetPlayer(state, WorldRace.Heretic));

        Assert.That(FormatObjectives(state, objectives), Is.EqualTo(
        [
            "zone at (29,1) 1x1",
            "stack S101KC0013 at (35,21) 1x1"
        ]));
    }

    [Test]
    public void StackOnSiteTest()
    {
        Scenario scenario = LoadScenario("S101SC0000");
        WorldStackId stackId = new("S101KC0013");
        WorldStack stack = scenario.State.Stacks[stackId];
        WorldState state = scenario.State with
        {
            Stacks = scenario.State.Stacks.SetItem(stackId, stack with { Position = new WorldPosition(13, 43) })
        };

        WorldPlan plan = WorldPlan.For(state);
        ImmutableArray<WorldObjective> objectives = plan.GetObjectives(scenario.Provider, GetPlayer(state, WorldRace.Heretic));

        Assert.That(plan.GetSiteAt(new WorldPosition(13, 43)), Is.Not.Null);
        Assert.That(FormatObjectives(state, objectives), Is.EqualTo(
        [
            "zone at (29,1) 1x1",
            "stack S101KC0013 at (13,43) 1x1"
        ]));
    }
}
