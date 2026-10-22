using System.Text.Json;
using Gallean.Common;

namespace Gallean.Configuration;

public sealed class GameSettingsStore(string settingsDirectory)
{
    private const int BufferSize = 4 * 1024;
    private const string SettingsFileName = "settings.json";

    private readonly string settingsPath = Path.Combine(settingsDirectory, SettingsFileName);

    public async Task<GameSettings?> TryLoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await LoadAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            return null;
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException)
        {
            throw new GameSettingsInvalidException(
                $"Unable to load the settings from '{settingsPath}': {ex.Message}",
                ex);
        }
    }

    public void Save(GameSettings settings)
    {
        Directory.CreateDirectory(settingsDirectory);

        using AtomicFileWriter writer = new(settingsPath);

        JsonSerializer.Serialize(
            writer.Stream,
            settings,
            GameSettingsJsonSerializerContext.Default.GameSettings);

        writer.Commit();
    }

    private async Task<GameSettings> LoadAsync(CancellationToken cancellationToken)
    {
        await using FileStream stream = new(
            settingsPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            BufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        int format = await JsonFormatVersionReader.ReadAsync(stream, cancellationToken);

        if (format != GameSettings.CurrentFormatVersion)
        {
            throw new GameSettingsUnsupportedFormatException(
                $"Unable to load the settings from '{settingsPath}': Unsupported settings format '{format}'.");
        }

        stream.Position = 0;

        return await GameSettingsJsonSerializerContext.Default.GameSettings.DeserializeRequiredAsync(stream, cancellationToken);
    }
}
