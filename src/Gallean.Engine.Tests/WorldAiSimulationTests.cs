using System.Collections.Immutable;
using Gallean.Common;
using Gallean.Entities.GameData;

namespace Gallean.Engine.Tests;

public sealed class WorldAiSimulationTests
{
    private enum SimulationSeverity
    {
        Warning,
        Critical
    }

    private sealed record SimulationOptions(
        int TurnCount,
        int MaxStacksPerPlayer,
        int MaxStackGrowth,
        int StackGrowthWindow,
        int FloatingResourceThreshold,
        int FloatingResourceWindow,
        int MovementWindow,
        int IdleTurnWindow,
        int BattleStagnationWindow,
        int MaxActionsPerTurn);

    private sealed record SimulationTurnSample(
        string ScenarioId,
        int Observation,
        int TurnNumber,
        WorldPlayerId PlayerId,
        WorldRace Race,
        int ActionCount,
        int MoveActionCount,
        int EconomyActionCount,
        int BattleCount,
        int PlayerStackCount,
        int TotalStackCount,
        WorldResourceSet Resources,
        int ResourceTotal,
        int AvailableTargetCount);

    private sealed record SimulationIssue(
        SimulationSeverity Severity,
        string Category,
        string ScenarioId,
        int TurnNumber,
        WorldPlayerId? PlayerId,
        WorldStackId? StackId,
        string Details);

    private sealed class SimulationAnalyzer(SimulationOptions options)
    {
        private readonly Dictionary<WorldPlayerId, int> battleStagnationTurns = [];
        private readonly HashSet<string> emittedIssues = [];
        private readonly Dictionary<WorldPlayerId, int> floatingResourceTurns = [];
        private readonly Dictionary<WorldPlayerId, int> idleTurns = [];
        private readonly Dictionary<WorldPlayerId, Queue<int>> stackCounts = [];
        private readonly Dictionary<WorldStackId, List<WorldPosition>> stackMoves = [];

        public List<SimulationIssue> AnalyzeTurn(
            string scenarioId,
            int observation,
            WorldState state,
            WorldTurnInfo turnInfo,
            IReadOnlyList<TestWorldAiRunner.Trace> traces)
        {
            WorldPlayer player = state.Players[turnInfo.CurrentPlayerId];
            SimulationTurnSample sample = BuildSample(scenarioId, observation, state, turnInfo, traces, player);
            List<SimulationIssue> issues = [];

            AnalyzeActionBudget(sample, issues);

            if (WorldHelpers.IsRacePlayable(sample.Race))
            {
                AnalyzeStackVolume(sample, issues);
                AnalyzeStackGrowth(sample, issues);
                AnalyzeFloatingResources(sample, issues);
                AnalyzeIdlePlayer(sample, issues);
                AnalyzeBattleStagnation(sample, issues);
            }

            AnalyzeLostAttacks(sample, traces, issues);
            AnalyzeMovement(sample, traces, issues);

            return issues;
        }

        private static SimulationTurnSample BuildSample(
            string scenarioId,
            int observation,
            WorldState state,
            WorldTurnInfo turnInfo,
            IReadOnlyList<TestWorldAiRunner.Trace> traces,
            WorldPlayer player)
        {
            return new SimulationTurnSample(
                scenarioId,
                observation,
                turnInfo.TurnNumber,
                turnInfo.CurrentPlayerId,
                player.Race,
                traces.Count,
                CountMoveActions(traces),
                CountEconomyActions(traces),
                CountBattles(traces),
                state.Stacks.Values.Count(stack => stack.OwnerId == turnInfo.CurrentPlayerId),
                state.Stacks.Count,
                player.Resources,
                GetResourceTotal(player.Resources),
                CountAvailableTargets(state, turnInfo.CurrentPlayerId));
        }

        private void AnalyzeActionBudget(SimulationTurnSample sample, List<SimulationIssue> issues)
        {
            if (sample.ActionCount >= options.MaxActionsPerTurn)
            {
                AddIssue(issues, new SimulationIssue(
                    SimulationSeverity.Critical,
                    "Action budget",
                    sample.ScenarioId,
                    sample.TurnNumber,
                    sample.PlayerId,
                    null,
                    $"AI used {sample.ActionCount} actions."));
            }
        }

        private void AnalyzeStackVolume(SimulationTurnSample sample, List<SimulationIssue> issues)
        {
            if (sample.PlayerStackCount > options.MaxStacksPerPlayer)
            {
                AddIssue(issues, new SimulationIssue(
                    SimulationSeverity.Critical,
                    "Stack volume",
                    sample.ScenarioId,
                    sample.TurnNumber,
                    sample.PlayerId,
                    null,
                    $"Player controls {sample.PlayerStackCount} stacks out of {sample.TotalStackCount}."));
            }
        }

        private void AnalyzeStackGrowth(SimulationTurnSample sample, List<SimulationIssue> issues)
        {
            if (!stackCounts.TryGetValue(sample.PlayerId, out Queue<int>? counts))
            {
                counts = [];
                stackCounts[sample.PlayerId] = counts;
            }

            counts.Enqueue(sample.PlayerStackCount);

            while (counts.Count > options.StackGrowthWindow)
            {
                counts.Dequeue();
            }

            if (counts.Count < options.StackGrowthWindow)
            {
                return;
            }

            int growth = sample.PlayerStackCount - counts.Peek();

            if (growth > options.MaxStackGrowth)
            {
                AddIssue(issues, new SimulationIssue(
                    SimulationSeverity.Critical,
                    "Stack growth",
                    sample.ScenarioId,
                    sample.TurnNumber,
                    sample.PlayerId,
                    null,
                    $"Player stack count grew by {growth} across {options.StackGrowthWindow} observed turns."));
            }
        }

        private void AnalyzeFloatingResources(SimulationTurnSample sample, List<SimulationIssue> issues)
        {
            floatingResourceTurns.TryAdd(sample.PlayerId, 0);

            if (sample.ResourceTotal < options.FloatingResourceThreshold || sample.EconomyActionCount > 0)
            {
                floatingResourceTurns[sample.PlayerId] = 0;

                return;
            }

            floatingResourceTurns[sample.PlayerId]++;

            if (floatingResourceTurns[sample.PlayerId] >= options.FloatingResourceWindow)
            {
                AddIssue(issues, new SimulationIssue(
                    SimulationSeverity.Warning,
                    "Floating resources",
                    sample.ScenarioId,
                    sample.TurnNumber,
                    sample.PlayerId,
                    null,
                    $"Player holds {sample.Resources} after {floatingResourceTurns[sample.PlayerId]} observed turns without economy actions."));
            }
        }

        private void AnalyzeIdlePlayer(SimulationTurnSample sample, List<SimulationIssue> issues)
        {
            idleTurns.TryAdd(sample.PlayerId, 0);

            if (sample.ActionCount > 0)
            {
                idleTurns[sample.PlayerId] = 0;

                return;
            }

            idleTurns[sample.PlayerId]++;

            if (idleTurns[sample.PlayerId] >= options.IdleTurnWindow)
            {
                AddIssue(issues, new SimulationIssue(
                    SimulationSeverity.Warning,
                    "Idle player",
                    sample.ScenarioId,
                    sample.TurnNumber,
                    sample.PlayerId,
                    null,
                    $"Player had no actions for {idleTurns[sample.PlayerId]} observed turns."));
            }
        }

        private void AnalyzeBattleStagnation(SimulationTurnSample sample, List<SimulationIssue> issues)
        {
            battleStagnationTurns.TryAdd(sample.PlayerId, 0);

            if (sample.BattleCount > 0 || sample.PlayerStackCount == 0 || sample.AvailableTargetCount == 0)
            {
                battleStagnationTurns[sample.PlayerId] = 0;

                return;
            }

            battleStagnationTurns[sample.PlayerId]++;

            if (battleStagnationTurns[sample.PlayerId] >= options.BattleStagnationWindow)
            {
                AddIssue(issues, new SimulationIssue(
                    SimulationSeverity.Warning,
                    "Battle stagnation",
                    sample.ScenarioId,
                    sample.TurnNumber,
                    sample.PlayerId,
                    null,
                    $"Player fought no battles for {battleStagnationTurns[sample.PlayerId]} observed turns while {sample.AvailableTargetCount} available targets remain."));
            }
        }

        private void AnalyzeLostAttacks(
            SimulationTurnSample sample,
            IReadOnlyList<TestWorldAiRunner.Trace> traces,
            List<SimulationIssue> issues)
        {
            foreach (TestWorldAiRunner.Trace trace in traces)
            {
                if (trace.Battle is null)
                {
                    continue;
                }

                if (trace.Battle.WinningSide != BattleSide.Defender)
                {
                    continue;
                }

                if (trace.Plan.Command is not WorldAiMoveStackCommand command)
                {
                    continue;
                }

                if (trace.StackOrder is WorldStackOrder.AttackStack)
                {
                    continue;
                }

                AddIssue(issues, new SimulationIssue(
                    SimulationSeverity.Critical,
                    "Lost attack",
                    sample.ScenarioId,
                    sample.TurnNumber,
                    sample.PlayerId,
                    command.StackId,
                    $"Stack lost an initiated attack toward ({command.TargetPosition.X}, {command.TargetPosition.Y})."));
            }
        }

        private void AnalyzeMovement(
            SimulationTurnSample sample,
            IReadOnlyList<TestWorldAiRunner.Trace> traces,
            List<SimulationIssue> issues)
        {
            foreach (TestWorldAiRunner.Trace trace in traces)
            {
                if (trace.Plan.Command is WorldAiMoveStackCommand command)
                {
                    if (!stackMoves.TryGetValue(command.StackId, out List<WorldPosition>? history))
                    {
                        history = [];
                        stackMoves[command.StackId] = history;
                    }

                    if (history.Count == 0 || history[^1] != command.TargetPosition)
                    {
                        history.Add(command.TargetPosition);

                        if (history.Count > options.MovementWindow)
                        {
                            history.RemoveAt(0);
                        }

                        AnalyzeTwoCellOscillation(sample, command.StackId, history, issues);
                        AnalyzeSmallMoveLoop(sample, command.StackId, history, issues);
                    }
                }
            }
        }

        private void AnalyzeTwoCellOscillation(
            SimulationTurnSample sample,
            WorldStackId stackId,
            IReadOnlyList<WorldPosition> history,
            List<SimulationIssue> issues)
        {
            if (history.Count < 4)
            {
                return;
            }

            int lastIndex = history.Count - 1;
            WorldPosition first = history[lastIndex - 3];
            WorldPosition second = history[lastIndex - 2];
            WorldPosition third = history[lastIndex - 1];
            WorldPosition fourth = history[lastIndex];

            if (first == third && second == fourth && first != second)
            {
                AddIssue(issues, new SimulationIssue(
                    SimulationSeverity.Warning,
                    "Move oscillation",
                    sample.ScenarioId,
                    sample.TurnNumber,
                    sample.PlayerId,
                    stackId,
                    $"Stack alternated between ({first.X}, {first.Y}) and ({second.X}, {second.Y})."));
            }
        }

        private void AnalyzeSmallMoveLoop(
            SimulationTurnSample sample,
            WorldStackId stackId,
            IReadOnlyList<WorldPosition> history,
            List<SimulationIssue> issues)
        {
            if (history.Count < options.MovementWindow)
            {
                return;
            }

            HashSet<WorldPosition> positions = [.. history];

            if (positions.Count <= 2)
            {
                AddIssue(issues, new SimulationIssue(
                    SimulationSeverity.Warning,
                    "Move loop",
                    sample.ScenarioId,
                    sample.TurnNumber,
                    sample.PlayerId,
                    stackId,
                    $"Stack used {history.Count} moves across {positions.Count} cells."));
            }
        }

        private void AddIssue(List<SimulationIssue> issues, SimulationIssue issue)
        {
            string key = $"{issue.Severity}|{issue.Category}|{issue.ScenarioId}|{issue.PlayerId}|{issue.StackId}|{issue.Details}";

            if (!emittedIssues.Add(key))
            {
                return;
            }

            issues.Add(issue);
        }

        private static int CountMoveActions(IReadOnlyList<TestWorldAiRunner.Trace> traces)
        {
            int count = 0;

            foreach (TestWorldAiRunner.Trace trace in traces)
            {
                if (trace.Plan.Command is WorldAiMoveStackCommand)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountEconomyActions(IReadOnlyList<TestWorldAiRunner.Trace> traces)
        {
            int count = 0;

            foreach (TestWorldAiRunner.Trace trace in traces)
            {
                if (trace.Plan.Command is WorldPurchaseLeaderCommand or WorldPurchaseUnitCommand or WorldConstructBuildingCommand)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountBattles(IReadOnlyList<TestWorldAiRunner.Trace> traces)
        {
            int count = 0;

            foreach (TestWorldAiRunner.Trace trace in traces)
            {
                if (trace.Battle is not null)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountAvailableTargets(WorldState state, WorldPlayerId playerId)
        {
            WorldPlayer player = state.Players[playerId];
            int count = 0;

            foreach (WorldStack stack in state.Stacks.Values)
            {
                if (state.Players[stack.OwnerId].Race == WorldRace.Neutral && IsDiscovered(player, stack.Position))
                {
                    count++;
                }
            }

            foreach (WorldCity city in state.Fortifications.Values.OfType<WorldCity>())
            {
                if (city.OwnerId != playerId && IsDiscovered(player, city.Entrance))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool IsDiscovered(WorldPlayer player, WorldPosition position)
        {
            return player.FogOfWar.Get(position.X, position.Y);
        }

        private static int GetResourceTotal(WorldResourceSet resources)
        {
            return resources.Gold +
                   resources.DeathMana +
                   resources.RuneMana +
                   resources.LifeMana +
                   resources.InfernalMana +
                   resources.GroveMana;
        }
    }

    private sealed record SimulationResult(IReadOnlyList<SimulationIssue> Issues);

    private static readonly SimulationOptions SmokeCheckOptions = new(
        TurnCount: 1,
        MaxStacksPerPlayer: 40,
        MaxStackGrowth: 12,
        StackGrowthWindow: 6,
        FloatingResourceThreshold: 12000,
        FloatingResourceWindow: 4,
        MovementWindow: 6,
        IdleTurnWindow: 6,
        BattleStagnationWindow: 6,
        MaxActionsPerTurn: 95);

    private static readonly SimulationOptions MediumCheckOptions = SmokeCheckOptions with { TurnCount = 12 };
    private static readonly SimulationOptions FullCheckOptions = SmokeCheckOptions with { TurnCount = 120 };

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

    private static IEnumerable<TestCaseData> GetSmokeCheckScenarioCases()
    {
        ScenarioDatabaseIndex index = ScenarioDatabaseLoader.LoadIndex(RiseOfTheElves.ScenarioDatabasePath);

        foreach (Db.ScenarioInfo scenarioInfo in index.ScenarioInfos.OrderBy(info => info.ScenarioId.Value))
        {
            yield return new TestCaseData(scenarioInfo.ScenarioId.Value);
        }
    }

    private static IEnumerable<TestCaseData> GetMediumCheckScenarioCases()
    {
        yield return new TestCaseData("S095SC0000");
        yield return new TestCaseData("S099SC0000");
        yield return new TestCaseData("S107SC0000");
    }

    private static IEnumerable<TestCaseData> GetFullCheckScenarioCases()
    {
        yield return new TestCaseData("S095SC0000");
        yield return new TestCaseData("S099SC0000");
        yield return new TestCaseData("S107SC0000");
        yield return new TestCaseData("S110SC0000");
        yield return new TestCaseData("S120SC0000");
        yield return new TestCaseData("S130SC0000");
        yield return new TestCaseData("S142SC0000");
    }

    private SimulationResult RunSimulation(string scenarioId, SimulationOptions options)
    {
        GlobalVariables globals = new();
        Scenario scenario = ScenarioLoader.Load(globalDb, scenarioDb, campaignDb, new ScenarioId(scenarioId), definitions, globals);
        EngineContext context = new(new FixedRandomProvider(), definitions, new UnitStatsProvider(definitions, globals, scenario.Provider.Definition), scenario.Provider, globals);
        GameSession session = new(context, scenario.State with { Difficulty = WorldDifficulty.Hard });
        WorldCommandResult startResult = session.World.DispatchCommand(new WorldStartGameCommand());
        List<SimulationIssue> issues = [];
        SimulationAnalyzer analyzer = new(options);

        if (startResult.Failed)
        {
            issues.Add(new SimulationIssue(
                SimulationSeverity.Critical,
                "Start failure",
                scenarioId,
                session.World.State.TurnInfo.TurnNumber,
                null,
                null,
                $"Start game failed with {startResult.Error}."));

            return new SimulationResult(issues);
        }

        TestWorldAiRunner runner = new(session, context);
        int observation = 0;

        while (observation < options.TurnCount)
        {
            WorldTurnInfo turnInfo = session.World.State.TurnInfo;
            WorldPlayer player = session.World.State.Players[turnInfo.CurrentPlayerId];

            if (WorldHelpers.IsRacePlayable(player.Race))
            {
                observation++;
            }

            IReadOnlyList<TestWorldAiRunner.Trace> traces = runner.PlayTurn();
            issues.AddRange(analyzer.AnalyzeTurn(scenarioId, observation, session.World.State, turnInfo, traces));
        }

        return new SimulationResult(issues);
    }

    private static IOrderedEnumerable<SimulationIssue> GetIssues(SimulationResult result)
    {
        return result.Issues
            .OrderBy(issue => issue.TurnNumber)
            .ThenBy(issue => issue.Category);
    }

    private static IReadOnlyList<TestWorldAiRunner.Trace> BuildMovementTraces(WorldStackId stackId, WorldPosition target)
    {
        return [BuildTrace(new WorldAiMoveStackCommand(stackId, target))];
    }

    private static TestWorldAiRunner.Trace BuildTrace(IWorldCommand<Empty> command)
    {
        WorldAiActionPlan plan = new(default, command, WorldAiMemory.Empty);

        return new TestWorldAiRunner.Trace(plan, null, null);
    }

    private static TestWorldAiRunner.Trace BuildBattleTrace(IWorldCommand<Empty> command, BattleOutcome outcome)
    {
        WorldAiActionPlan plan = new(default, command, WorldAiMemory.Empty);

        return new TestWorldAiRunner.Trace(plan, outcome, null);
    }

    private static WorldState BuildAnalyzerState(WorldPlayerId playerId, WorldResourceSet resources, WorldStack stack)
    {
        WorldMap map = TestHelpers.SimpleMap(3, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(playerId, WorldRace.Human, map) with
        {
            Resources = resources
        };

        return new WorldState(
            map,
            new WorldTurnInfo(1, playerId),
            WorldDifficulty.Average,
            new Dictionary<WorldPlayerId, WorldPlayer>
            {
                [player.Id] = player
            }.ToImmutableSortedDictionary(),
            ImmutableSortedDictionary<WorldRelationId, WorldRelation>.Empty,
            new Dictionary<WorldStackId, WorldStack>
            {
                [stack.Id] = stack
            }.ToImmutableSortedDictionary(),
            ImmutableSortedDictionary<WorldFortificationId, WorldFortification>.Empty,
            ImmutableSortedDictionary<WorldResourceNodeId, WorldResourceNode>.Empty,
            ImmutableSortedDictionary<WorldRodId, WorldRod>.Empty,
            ImmutableSortedDictionary<WorldBagId, WorldBag>.Empty,
            ImmutableSortedDictionary<WorldTombId, WorldTomb>.Empty,
            ImmutableSortedDictionary<WorldLandmarkId, WorldLandmark>.Empty,
            ImmutableSortedDictionary<WorldMountainId, WorldMountain>.Empty,
            ImmutableSortedDictionary<WorldSiteId, WorldSite>.Empty,
            ImmutableSortedDictionary<WorldSubraceId, WorldSubrace>.Empty,
            WorldScenarioState.Empty,
            ImmutableSortedDictionary<WorldPlayerId, WorldAiMemory>.Empty);
    }

    [TestCaseSource(nameof(GetSmokeCheckScenarioCases))]
    public void SmokeCheckTest(string scenarioId)
    {
        SimulationResult result = RunSimulation(scenarioId, SmokeCheckOptions);

        Assert.That(GetIssues(result), Is.Empty);
    }

    [TestCaseSource(nameof(GetMediumCheckScenarioCases))]
    public void MediumCheckTest(string scenarioId)
    {
        SimulationResult result = RunSimulation(scenarioId, MediumCheckOptions);

        Assert.That(GetIssues(result), Is.Empty);
    }

    [TestCaseSource(nameof(GetFullCheckScenarioCases))]
    [Explicit]
    public void FullCheckTest(string scenarioId)
    {
        SimulationResult result = RunSimulation(scenarioId, FullCheckOptions);

        Assert.That(GetIssues(result), Is.Empty);
    }

    [Test]
    public void MovementOscillationAnalyzerTest()
    {
        WorldPlayerId playerId = new(1);
        WorldStackId stackId = new("Stack1");
        WorldStack stack = TestHelpers.SimpleStack(stackId, playerId, new WorldPosition(0, 0), UnitGrid<WorldUnit>.Empty, movementPoints: 6);
        WorldState state = BuildAnalyzerState(playerId, WorldResourceSet.Empty, stack);
        SimulationAnalyzer analyzer = new(MediumCheckOptions);
        List<SimulationIssue> issues = [];

        issues.AddRange(analyzer.AnalyzeTurn("Synthetic", 1, state, new WorldTurnInfo(1, playerId), BuildMovementTraces(stackId, new WorldPosition(0, 0))));
        issues.AddRange(analyzer.AnalyzeTurn("Synthetic", 2, state, new WorldTurnInfo(2, playerId), BuildMovementTraces(stackId, new WorldPosition(1, 0))));
        issues.AddRange(analyzer.AnalyzeTurn("Synthetic", 3, state, new WorldTurnInfo(3, playerId), BuildMovementTraces(stackId, new WorldPosition(0, 0))));
        issues.AddRange(analyzer.AnalyzeTurn("Synthetic", 4, state, new WorldTurnInfo(4, playerId), BuildMovementTraces(stackId, new WorldPosition(1, 0))));

        Assert.That(GetIssues(new SimulationResult(issues)), Is.EqualTo([
            new SimulationIssue(
                SimulationSeverity.Warning,
                "Move oscillation",
                "Synthetic",
                4,
                playerId,
                stackId,
                "Stack alternated between (0, 0) and (1, 0).")
        ]));
    }

    [Test]
    public void LostAttackAnalyzerTest()
    {
        WorldPlayerId playerId = new(1);
        WorldStackId stackId = new("Stack1");
        WorldStack stack = TestHelpers.SimpleStack(stackId, playerId, new WorldPosition(0, 0), UnitGrid<WorldUnit>.Empty, movementPoints: 6);
        WorldState state = BuildAnalyzerState(playerId, WorldResourceSet.Empty, stack);
        SimulationAnalyzer analyzer = new(MediumCheckOptions);
        List<SimulationIssue> issues = [];

        issues.AddRange(analyzer.AnalyzeTurn("Synthetic", 1, state, new WorldTurnInfo(1, playerId),
            [BuildBattleTrace(new WorldAiMoveStackCommand(stackId, new WorldPosition(1, 0)), new BattleOutcome(BattleSide.Defender))]));

        Assert.That(GetIssues(new SimulationResult(issues)), Is.EqualTo([
            new SimulationIssue(
                SimulationSeverity.Critical,
                "Lost attack",
                "Synthetic",
                1,
                playerId,
                stackId,
                "Stack lost an initiated attack toward (1, 0).")
        ]));
    }

    [Test]
    public void OrderedLostAttackAnalyzerTest()
    {
        WorldPlayerId playerId = new(1);
        WorldStackId stackId = new("Stack1");
        WorldStack stack = TestHelpers.SimpleStack(stackId, playerId, new WorldPosition(0, 0), UnitGrid<WorldUnit>.Empty, movementPoints: 6);
        WorldState state = BuildAnalyzerState(playerId, WorldResourceSet.Empty, stack);
        SimulationAnalyzer analyzer = new(MediumCheckOptions);

        WorldAiActionPlan plan = new(
            new WorldAiActionScore(2, 0),
            new WorldAiMoveStackCommand(stackId, new WorldPosition(1, 0)),
            new WorldAiMemory(new WorldAiTurnMemory(1, playerId)));

        IReadOnlyList<TestWorldAiRunner.Trace> traces =
        [
            new(plan, new BattleOutcome(BattleSide.Defender), new WorldStackOrder.AttackStack(new WorldStackId("Target")))
        ];

        IEnumerable<SimulationIssue> issues = analyzer.AnalyzeTurn("Synthetic", 1, state, new WorldTurnInfo(1, playerId), traces);

        Assert.That(GetIssues(new SimulationResult([.. issues])), Is.Empty);
    }

    [Test]
    public void FloatingResourcesAnalyzerTest()
    {
        WorldPlayerId playerId = new(1);
        WorldStackId stackId = new("Stack1");
        WorldStack stack = TestHelpers.SimpleStack(stackId, playerId, new WorldPosition(0, 0), UnitGrid<WorldUnit>.Empty, movementPoints: 6);
        WorldState state = BuildAnalyzerState(playerId, new WorldResourceSet(20000, 0, 0, 0, 0, 0), stack);
        SimulationAnalyzer analyzer = new(MediumCheckOptions);
        List<SimulationIssue> issues = [];

        issues.AddRange(analyzer.AnalyzeTurn("Synthetic", 1, state, new WorldTurnInfo(1, playerId), []));
        issues.AddRange(analyzer.AnalyzeTurn("Synthetic", 2, state, new WorldTurnInfo(2, playerId), []));
        issues.AddRange(analyzer.AnalyzeTurn("Synthetic", 3, state, new WorldTurnInfo(3, playerId), []));
        issues.AddRange(analyzer.AnalyzeTurn("Synthetic", 4, state, new WorldTurnInfo(4, playerId), []));

        Assert.That(GetIssues(new SimulationResult(issues)), Is.EqualTo([
            new SimulationIssue(
                SimulationSeverity.Warning,
                "Floating resources",
                "Synthetic",
                4,
                playerId,
                null,
                "Player holds G20000:DM0:RM0:LM0:IM0:GM0 after 4 observed turns without economy actions.")
        ]));
    }

    [Test]
    public void BattleStagnationAnalyzerTest()
    {
        WorldPlayerId playerId = new(1);
        WorldStackId stackId = new("Stack1");
        WorldStack stack = TestHelpers.SimpleStack(stackId, playerId, new WorldPosition(0, 0), UnitGrid<WorldUnit>.Empty, movementPoints: 6);
        WorldMap map = TestHelpers.SimpleMap(5, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(playerId, WorldRace.Undead, map);

        WorldPlayerId neutralId = new(9);
        WorldPlayer neutral = TestHelpers.SimplePlayer(neutralId, WorldRace.Neutral, map);
        WorldStack neutralStack = TestHelpers.SimpleStack(
            new WorldStackId("Neutral1"),
            neutralId,
            new WorldPosition(4, 0),
            UnitGrid<WorldUnit>.Empty,
            movementPoints: 0);

        WorldState state = new(
            map,
            new WorldTurnInfo(1, new WorldPlayerId(1)),
            WorldDifficulty.Average,
            new Dictionary<WorldPlayerId, WorldPlayer>
            {
                [player.Id] = player,
                [neutral.Id] = neutral
            }.ToImmutableSortedDictionary(),
            ImmutableSortedDictionary<WorldRelationId, WorldRelation>.Empty,
            new Dictionary<WorldStackId, WorldStack>
            {
                [stack.Id] = stack,
                [neutralStack.Id] = neutralStack
            }.ToImmutableSortedDictionary(),
            ImmutableSortedDictionary<WorldFortificationId, WorldFortification>.Empty,
            ImmutableSortedDictionary<WorldResourceNodeId, WorldResourceNode>.Empty,
            ImmutableSortedDictionary<WorldRodId, WorldRod>.Empty,
            ImmutableSortedDictionary<WorldBagId, WorldBag>.Empty,
            ImmutableSortedDictionary<WorldTombId, WorldTomb>.Empty,
            ImmutableSortedDictionary<WorldLandmarkId, WorldLandmark>.Empty,
            ImmutableSortedDictionary<WorldMountainId, WorldMountain>.Empty,
            ImmutableSortedDictionary<WorldSiteId, WorldSite>.Empty,
            ImmutableSortedDictionary<WorldSubraceId, WorldSubrace>.Empty,
            WorldScenarioState.Empty,
            ImmutableSortedDictionary<WorldPlayerId, WorldAiMemory>.Empty);

        SimulationOptions options = MediumCheckOptions;
        SimulationAnalyzer analyzer = new(MediumCheckOptions);
        List<SimulationIssue> issues = [];

        for (int turn = 1; turn <= options.BattleStagnationWindow; turn++)
        {
            IReadOnlyList<TestWorldAiRunner.Trace> traces = [BuildTrace(new WorldAutoFitStackCommand(stackId))];

            issues.AddRange(analyzer.AnalyzeTurn("Synthetic", turn, state, new WorldTurnInfo(turn, playerId), traces));
        }

        Assert.That(GetIssues(new SimulationResult(issues)), Is.EqualTo([
            new SimulationIssue(
                SimulationSeverity.Warning,
                "Battle stagnation",
                "Synthetic",
                6,
                playerId,
                null,
                "Player fought no battles for 6 observed turns while 1 available targets remain.")
        ]));
    }

    [Test]
    public void MoveLoopAnalyzerTest()
    {
        WorldPlayerId playerId = new(1);
        WorldStackId stackId = new("Stack1");
        WorldStack stack = TestHelpers.SimpleStack(stackId, playerId, new WorldPosition(0, 0), UnitGrid<WorldUnit>.Empty, movementPoints: 6);
        WorldState state = BuildAnalyzerState(playerId, WorldResourceSet.Empty, stack);
        SimulationAnalyzer analyzer = new(MediumCheckOptions);
        List<SimulationIssue> issues = [];

        issues.AddRange(analyzer.AnalyzeTurn("Synthetic", 1, state, new WorldTurnInfo(1, playerId), BuildMovementTraces(stackId, new WorldPosition(0, 0))));
        issues.AddRange(analyzer.AnalyzeTurn("Synthetic", 2, state, new WorldTurnInfo(2, playerId), BuildMovementTraces(stackId, new WorldPosition(1, 0))));
        issues.AddRange(analyzer.AnalyzeTurn("Synthetic", 3, state, new WorldTurnInfo(3, playerId), BuildMovementTraces(stackId, new WorldPosition(0, 0))));
        issues.AddRange(analyzer.AnalyzeTurn("Synthetic", 4, state, new WorldTurnInfo(4, playerId), BuildMovementTraces(stackId, new WorldPosition(1, 0))));
        issues.AddRange(analyzer.AnalyzeTurn("Synthetic", 5, state, new WorldTurnInfo(5, playerId), BuildMovementTraces(stackId, new WorldPosition(0, 0))));
        issues.AddRange(analyzer.AnalyzeTurn("Synthetic", 6, state, new WorldTurnInfo(6, playerId), BuildMovementTraces(stackId, new WorldPosition(1, 0))));

        Assert.That(GetIssues(new SimulationResult(issues)), Is.EqualTo([
            new SimulationIssue(
                SimulationSeverity.Warning,
                "Move oscillation",
                "Synthetic",
                4,
                playerId,
                stackId,
                "Stack alternated between (0, 0) and (1, 0)."),
            new SimulationIssue(
                SimulationSeverity.Warning,
                "Move oscillation",
                "Synthetic",
                5,
                playerId,
                stackId,
                "Stack alternated between (1, 0) and (0, 0)."),
            new SimulationIssue(
                SimulationSeverity.Warning,
                "Move loop",
                "Synthetic",
                6,
                playerId,
                stackId,
                "Stack used 6 moves across 2 cells.")
        ]));
    }

    [Test]
    public void RepeatedMoveTargetAnalyzerTest()
    {
        WorldPlayerId playerId = new(1);
        WorldStackId stackId = new("Stack1");
        WorldStack stack = TestHelpers.SimpleStack(stackId, playerId, new WorldPosition(0, 0), UnitGrid<WorldUnit>.Empty, movementPoints: 6);
        WorldState state = BuildAnalyzerState(playerId, WorldResourceSet.Empty, stack);
        SimulationAnalyzer analyzer = new(MediumCheckOptions);

        IReadOnlyList<TestWorldAiRunner.Trace> traces =
        [
            BuildTrace(new WorldAiMoveStackCommand(stackId, new WorldPosition(7, 0))),
            BuildTrace(new WorldAiMoveStackCommand(stackId, new WorldPosition(7, 0))),
            BuildTrace(new WorldAiMoveStackCommand(stackId, new WorldPosition(7, 0))),
            BuildTrace(new WorldAiMoveStackCommand(stackId, new WorldPosition(7, 0))),
            BuildTrace(new WorldAiMoveStackCommand(stackId, new WorldPosition(7, 0))),
            BuildTrace(new WorldAiMoveStackCommand(stackId, new WorldPosition(7, 0)))
        ];

        IEnumerable<SimulationIssue> issues = analyzer.AnalyzeTurn("Synthetic", 1, state, new WorldTurnInfo(1, playerId), traces);

        Assert.That(GetIssues(new SimulationResult([.. issues])), Is.Empty);
    }
}
