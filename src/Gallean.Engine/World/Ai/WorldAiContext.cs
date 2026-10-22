namespace Gallean.Engine;

internal sealed class WorldAiContext(IEngineContext engine, IWorldAiTrace trace)
{
    public IEngineContext Engine { get; } = engine;
    public IWorldAiTrace Trace { get; } = trace;
    public WorldAiEstimator Estimator { get; } = new(engine);
    public WorldAiPathfinding Pathfinding { get; } = new(engine);
    public WorldAiScenario Scenario { get; } = new(engine);
}
