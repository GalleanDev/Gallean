using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gallean.Configuration;

namespace Gallean.Client;

internal sealed partial class LoadGameScreenViewModel : AbstractScreenViewModel
{
    public LoadGameScreenViewModel(GameViewModel game, IReadOnlyList<GameSaveEntry> saves)
    {
        Game = game;
        SaveList = new GameSaveListViewModel(saves);
        SaveList.SelectedSave = SaveList.Saves.FirstOrDefault();
        SaveList.PropertyChanged += OnSaveListPropertyChanged;
    }

    public GameViewModel Game { get; }
    public GameSaveListViewModel SaveList { get; }

    protected override bool TryClose()
    {
        CloseSelf();

        return true;
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private void Delete()
    {
        if (SaveList.SelectedSave is { } selected)
        {
            Game.OpenScreen(new ConfirmPromptViewModel(
                Tr("Delete saved game"),
                TrSpans("Delete \"{0}\" permanently?", selected.Name),
                Tr("Delete"),
                ConfirmPromptType.Danger,
                () => DeleteSaveAsync(selected)));
        }
    }

    private bool CanDelete()
    {
        return SaveList.SelectedSave is not null;
    }

    private void OnSaveListPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GameSaveListViewModel.SelectedSave))
        {
            DeleteCommand.NotifyCanExecuteChanged();
        }
    }

    private async Task DeleteSaveAsync(GameSaveViewModel save)
    {
        if (await Game.TryDeleteSaveAsync(save.Id))
        {
            SaveList.Remove(save);
            SaveList.SelectedSave = SaveList.Saves.FirstOrDefault();
        }
    }
}
