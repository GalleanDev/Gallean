using Gallean.Common;
using Gallean.Engine;
using Gallean.Tests.Common;

namespace Gallean.Client.Tests;

[Explicit]
[NonParallelizable]
public sealed class WorldRenderStatePerformanceTests
{
    [TestCase("S095SC0000")]
    [TestCase("S099SC0000")]
    public async Task BuildStateTest(string scenarioId)
    {
        GameLoader loader = new(new RandomProvider(0));
        (GameSession session, IGameTextProvider texts) = loader.LoadScenario(scenarioId, worldState: null);
        using GameModel gameModel = new(session, texts, NullLog.Instance, SynchronousTaskScheduler.Instance);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        WorldState state = gameModel.Snapshot.WorldState;
        IGameDefinitionProvider definitions = gameModel.Definitions;

        WorldRenderRequest request = new(
            state,
            state.TurnInfo.CurrentPlayerId,
            Cursor: WorldCursor.Empty,
            FogEnabled: true,
            Animation: null);

        PerformanceMeasurement measurement = PerformanceTestHelpers.Measure(() =>
        {
            WorldRenderComposer composer = new(definitions, GameScenarioProvider.Empty, new TestMetadataProviderBuilder().Build());
            composer.Compose(request);
        }, 100, 10);

        await TestContext.Out.WriteLineAsync($"=== {scenarioId}  {state.Map.Width}x{state.Map.Height} ({state.Map.Width * state.Map.Height:N0} cells), {state.Stacks.Count} stacks ===");
        await TestContext.Out.WriteLineAsync(
            $"Build render state: {measurement.ElapsedTime.TotalMilliseconds:F3} ms, CPU = {measurement.CpuTime.TotalMilliseconds:F3} ms, {measurement.AllocatedBytes / 1024:N0} KB");
    }
}
