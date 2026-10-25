using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Gallean.Client;

internal sealed partial class GameMenuViewModel : AbstractGameScreenViewModel
{
    public GameMenuViewModel(GameModel gameModel, GameViewModel gameViewModel)
        : base(gameModel, gameViewModel)
    {
        SaveTip = FormatSaveTip(Snapshot);

        RegisterSnapshotGatedCommands(SaveGameCommand);
    }

    public override bool BlurBackdrop => true;

    [ObservableProperty]
    public partial string? SaveTip { get; private set; }

    protected override bool TryClose()
    {
        CloseSelf();

        return true;
    }

    protected override void OnSnapshotChanged(GameSnapshot current)
    {
        SaveTip = FormatSaveTip(current);
    }

    [RelayCommand(CanExecute = nameof(CanSaveGame))]
    private async Task SaveGameAsync()
    {
        await Game.ShowSaveGameAsync(GameModel, CloseSelf);
    }

    private bool CanSaveGame()
    {
        return Snapshot.IsSaveAllowed;
    }

    private static string? FormatSaveTip(GameSnapshot snapshot)
    {
        if (snapshot.BattleState is not null)
        {
            return Tr("Saving is not allowed during battle");
        }
        else if (!snapshot.IsViewPlayerTurn)
        {
            return Tr("Saving is not allowed during another player's turn");
        }
        else
        {
            return null;
        }
    }
}
