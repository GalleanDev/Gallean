using System.Collections.ObjectModel;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed class EventLogViewModel : IDisposable
{
    private readonly EventLogWorldFormatter worldFormatter;
    private readonly EventLogBattleFormatter battleFormatter;
    private readonly EventSubscription<EngineStep<WorldState, WorldEvent>> worldSubscription;
    private EventSubscription<EngineStep<BattleState, BattleEvent>>? battleSubscription;

    public EventLogViewModel(GameModel gameModel)
    {
        worldFormatter = new EventLogWorldFormatter(gameModel.Definitions, gameModel.Texts);
        battleFormatter = new EventLogBattleFormatter(gameModel.Definitions, gameModel.Texts);

        worldSubscription = new EventSubscription<EngineStep<WorldState, WorldEvent>>(gameModel.World.Events, OnWorldEvent);
    }

    public ObservableCollection<EventLogEntry> Entries { get; } = [];

    public void LogBattle(BattleModel battle)
    {
        battleSubscription?.Dispose();
        battleSubscription = new EventSubscription<EngineStep<BattleState, BattleEvent>>(battle.Events, OnBattleEvent);
    }

    public void Dispose()
    {
        worldSubscription.Dispose();
        battleSubscription?.Dispose();
    }

    private void OnWorldEvent(EngineStep<WorldState, WorldEvent> step)
    {
        Add(worldFormatter.Format(step.Event, step.OldState));
    }

    private void OnBattleEvent(EngineStep<BattleState, BattleEvent> step)
    {
        Add(battleFormatter.Format(step.Event, step.OldState));
    }

    private void Add(IReadOnlyList<EventLogEntry> entries)
    {
        foreach (EventLogEntry entry in entries)
        {
            Entries.Add(entry);
        }
    }
}
