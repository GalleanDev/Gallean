using Gallean.Engine;

namespace Gallean.Client;

internal sealed class BattleHistory
{
    private readonly Lock locker = new();
    private readonly List<EngineStep<BattleState, BattleEvent>> steps = [];

    public BattleHistory(IObservable<EngineStep<BattleState, BattleEvent>> events)
    {
        events.Subscribe(OnEvent);
    }

    public IReadOnlyList<EngineStep<BattleState, BattleEvent>> Steps
    {
        get
        {
            lock (locker)
            {
                return [.. steps];
            }
        }
    }

    private void OnEvent(EngineStep<BattleState, BattleEvent> step)
    {
        lock (locker)
        {
            steps.Add(step);
        }
    }
}
