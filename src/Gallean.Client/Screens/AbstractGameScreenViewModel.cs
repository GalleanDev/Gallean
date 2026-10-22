using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gallean.Engine;

namespace Gallean.Client;

internal abstract partial class AbstractGameScreenViewModel : AbstractScreenViewModel
{
    private readonly List<IRelayCommand> snapshotGatedCommands = [];

    protected AbstractGameScreenViewModel(GameModel gameModel, GameViewModel gameViewModel)
    {
        GameModel = gameModel;
        Game = gameViewModel;
        Snapshot = gameModel.Snapshot;

        gameModel.PropertyChanged += OnGameModelPropertyChanged;
    }

    public GameModel GameModel { get; }
    public GameViewModel Game { get; }

    protected GameSnapshot Snapshot { get; private set; }

    protected WorldState WorldState => Snapshot.WorldState;
    protected WorldPlayerId ViewPlayerId => Snapshot.ViewPlayerId;

    protected virtual bool IsBusy => Snapshot.IsBusy;

    public sealed override void Dispose()
    {
        GameModel.PropertyChanged -= OnGameModelPropertyChanged;
        OnDispose();

        base.Dispose();
    }

    protected virtual void OnDispose()
    {
    }

    protected abstract void OnSnapshotChanged(GameSnapshot current);

    protected void RegisterSnapshotGatedCommands(params IRelayCommand[] commands)
    {
        snapshotGatedCommands.AddRange(commands);
    }

    protected async Task<bool> TryDispatchWorldActionAsync<TResult>(IWorldCommand<TResult> command)
    {
        ICommandResult<WorldCommandError, WorldEvent, TResult>? result = await DispatchWorldActionAsync(command);

        return result is not null;
    }

    protected async Task<ICommandResult<WorldCommandError, WorldEvent, TResult>?> DispatchWorldActionAsync<TResult>(
        IWorldCommand<TResult> command)
    {
        if (IsBusy)
        {
            return null;
        }

        ICommandResult<WorldCommandError, WorldEvent, TResult> result = await GameModel.World.DispatchCommandAsync(command);

        if (result.Failed)
        {
            Game.ShowError(WorldCommandErrorFormatter.Format(result.Error.Value));

            return null;
        }

        return result;
    }

    private void OnGameModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(GameModel.Snapshot))
        {
            Snapshot = GameModel.Snapshot;
            OnSnapshotChanged(Snapshot);
            NotifySnapshotGatedCommandsChanged();
        }
    }

    protected void NotifySnapshotGatedCommandsChanged()
    {
        foreach (IRelayCommand command in snapshotGatedCommands)
        {
            command.NotifyCanExecuteChanged();
        }
    }
}
