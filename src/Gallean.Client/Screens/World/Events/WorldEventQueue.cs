using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class WorldEventQueue :
    IWorldEventVisitor<EngineStep<WorldState, WorldEvent>, Empty>,
    IDisposable
{
    private static readonly TimeSpan PauseDurationDefault = TimeSpan.FromSeconds(0.75);
    private static readonly TimeSpan PauseDurationScenarioPopup = TimeSpan.FromSeconds(0.25);

    private readonly IGameScenarioProvider scenario;
    private readonly IDisposable subscription;
    private readonly Lock locker = new();
    private readonly List<Entry> queue = [];
    private Entry? playingEntry;

    public WorldEventQueue(IGameScenarioProvider scenario, IObservable<EngineStep<WorldState, WorldEvent>> events)
    {
        this.scenario = scenario;

        subscription = events.Subscribe(EnqueueStep);
    }

    public Entry? PlayingEntry
    {
        get
        {
            lock (locker)
            {
                return playingEntry;
            }
        }
    }

    public bool IsIdle
    {
        get
        {
            lock (locker)
            {
                return playingEntry is null && queue.Count == 0;
            }
        }
    }

    public void Dispose()
    {
        subscription.Dispose();
    }

    private void EnqueueStep(EngineStep<WorldState, WorldEvent> step)
    {
        lock (locker)
        {
            if (queue.Count > 0 && queue[^1].TryExtend(step) is { } entry)
            {
                queue[^1] = entry;
            }
            else
            {
                step.Event.Accept(this, step);
            }
        }
    }

    public void EnqueueEntry(Entry entry)
    {
        lock (locker)
        {
            queue.Add(entry);
        }
    }

    public Entry? PlayNext()
    {
        lock (locker)
        {
            if (playingEntry is not null || queue.Count == 0)
            {
                return null;
            }

            playingEntry = queue[0];
            queue.RemoveAt(0);

            return playingEntry;
        }
    }

    public void Stop()
    {
        lock (locker)
        {
            playingEntry = null;
        }
    }
}
