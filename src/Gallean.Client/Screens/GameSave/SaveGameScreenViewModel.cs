using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gallean.Configuration;

namespace Gallean.Client;

internal sealed partial class SaveGameScreenViewModel : AbstractGameScreenViewModel
{
    private readonly Action onSaved;

    public SaveGameScreenViewModel(
        GameModel game,
        GameViewModel gameViewModel,
        IReadOnlyList<GameSaveEntry> saves,
        Action onSaved)
        : base(game, gameViewModel)
    {
        this.onSaved = onSaved;
        SaveList = new GameSaveListViewModel(saves);
        SaveList.PropertyChanged += OnSaveListPropertyChanged;
        Name = Tr("{0}, turn {1}", game.ScenarioName, WorldState.TurnInfo.TurnNumber);
    }

    public GameSaveListViewModel SaveList { get; }

    public int MaxNameLength => GameSaveMetadata.MaxNameLength;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    public partial string Name { get; set; }

    public string SaveLabel => SaveList.SelectedSave is null ? Tr("Save") : Tr("Overwrite");

    protected override bool TryClose()
    {
        CloseSelf();

        return true;
    }

    protected override void OnSnapshotChanged(GameSnapshot current)
    {
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        if (SaveList.SelectedSave is { } selected)
        {
            Game.OpenScreen(new ConfirmPromptViewModel(
                Tr("Overwrite saved game"),
                TrSpans("Replace \"{0}\" with the current game?", selected.Name),
                Tr("Overwrite"),
                ConfirmPromptType.Primary,
                () => WriteAsync(selected)));
        }
        else
        {
            await WriteAsync(null);
        }
    }

    private void OnSaveListPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GameSaveListViewModel.SelectedSave))
        {
            if (SaveList.SelectedSave is { IsUnreadable: false } selected)
            {
                Name = selected.Name;
            }

            OnPropertyChanged(nameof(SaveLabel));
        }
    }

    partial void OnNameChanged(string value)
    {
        string name = value.Trim();

        SaveList.SelectedSave = SaveList.FindByName(name);
    }

    private bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(Name);
    }

    private async Task WriteAsync(GameSaveViewModel? existingSave)
    {
        string name = Name.Trim();

        GameSaveEntry? saveEntry = await Game.SaveGameAsync(GameModel, existingSave?.Id, name);

        if (saveEntry is not null)
        {
            Game.ShowInfo(IconMoniker.SaveGame, Tr("Game saved"), name);
            onSaved();
        }
    }
}
