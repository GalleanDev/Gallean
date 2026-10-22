using System.Globalization;
using Gallean.Configuration;

namespace Gallean.Client;

internal sealed class GameSaveViewModel
{
    public GameSaveViewModel(GameSaveEntry entry)
    {
        Id = entry.Id;

        try
        {
            GameSaveMetadata metadata = entry.Metadata.Value;

            Entry = entry;
            Name = metadata.Name;
            ScenarioName = metadata.ScenarioName;
            Turn = FormatTurn(metadata);
            SavedAt = FormatSavedAt(metadata);
        }
        catch (GameSaveUnsupportedFormatException ex) when (ex.Metadata is not null)
        {
            Entry = null;
            Name = ex.Metadata.Name;
            ScenarioName = FormatIncompatible(ex.Metadata);
            Turn = FormatTurn(ex.Metadata);
            SavedAt = FormatSavedAt(ex.Metadata);
        }
        catch (GameSaveUnsupportedFormatException)
        {
            Entry = null;
            Name = Tr("Unreadable save");
            ScenarioName = Tr("Incompatible game save");
            Turn = string.Empty;
            SavedAt = string.Empty;
        }
        catch (GameSaveInvalidException)
        {
            Entry = null;
            Name = Tr("Unreadable save");
            ScenarioName = Tr("Invalid game save");
            Turn = string.Empty;
            SavedAt = string.Empty;
        }
    }

    public GameSaveId Id { get; }
    public GameSaveEntry? Entry { get; }
    public string Name { get; }
    public string ScenarioName { get; }
    public string Turn { get; }
    public string SavedAt { get; }

    public bool IsUnreadable => Entry is null;

    private static string FormatTurn(GameSaveMetadata metadata)
    {
        return Tr("Turn {0}", metadata.TurnNumber);
    }

    private static string FormatSavedAt(GameSaveMetadata metadata)
    {
        return metadata.SavedAt.LocalDateTime.ToString("g", CultureInfo.CurrentCulture);
    }

    private static string FormatIncompatible(GameSaveMetadata metadata)
    {
        if (metadata.SavedBy is not null)
        {
            ClientVersion version = ClientVersion.Deserialize(metadata.SavedBy);

            return Tr("Incompatible game save from {0}", ClientVersionFormatter.Format(version));
        }
        else
        {
            return Tr("Incompatible game save");
        }
    }
}
