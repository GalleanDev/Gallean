using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Gallean.Configuration;

namespace Gallean.Client;

internal sealed partial class GameSaveListViewModel : ObservableObject
{
    public GameSaveListViewModel(IReadOnlyList<GameSaveEntry> entries)
    {
        List<GameSaveViewModel> saves = [];

        foreach (GameSaveEntry entry in entries)
        {
            saves.Add(new GameSaveViewModel(entry));
        }

        IEnumerable<GameSaveViewModel> ordered = saves
            .OrderBy(save => save.IsUnreadable)
            .ThenByDescending(save => save.Entry?.Metadata.Value.SavedAt)
            .ThenByDescending(save => save.Id.Number);

        Saves = [.. ordered];
    }

    public ObservableCollection<GameSaveViewModel> Saves { get; }

    [ObservableProperty]
    public partial GameSaveViewModel? SelectedSave { get; set; }

    public GameSaveViewModel? FindByName(string name)
    {
        foreach (GameSaveViewModel save in Saves)
        {
            if (!save.IsUnreadable && save.Name == name)
            {
                return save;
            }
        }

        return null;
    }

    public void Remove(GameSaveViewModel save)
    {
        Saves.Remove(save);

        if (SelectedSave == save)
        {
            SelectedSave = null;
        }
    }
}
