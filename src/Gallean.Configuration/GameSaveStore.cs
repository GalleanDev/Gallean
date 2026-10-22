using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using Gallean.Common;
using Gallean.Engine.Contracts;

namespace Gallean.Configuration;

public sealed class GameSaveStore(string saveDirectory, int autoSaveLimit)
{
    private const int BufferSize = 16 * 1024;
    private const string SaveFilePrefix = "save-";
    private const string SaveFileExtension = ".json.gz";
    private const string SaveFilePattern = $"{SaveFilePrefix}*{SaveFileExtension}";

    private readonly SemaphoreSlim operationLock = new(1, 1);

    public int AutoSaveLimit { get; set; } = autoSaveLimit;

    public async Task<IReadOnlyList<GameSaveEntry>> EnumerateAsync(CancellationToken cancellationToken = default)
    {
        using IDisposable operation = await BeginOperationAsync(cancellationToken);

        return await ReadEntriesAsync(cancellationToken);
    }

    public async Task<GameSaveEntry> SaveAsync(
        GameSaveId? id,
        GameSaveMetadata metadata,
        WorldState worldState,
        CancellationToken cancellationToken = default)
    {
        using IDisposable operation = await BeginOperationAsync(cancellationToken);

        GameSaveEntry entry = await WriteAsync(id ?? GetNewId(), metadata, worldState, cancellationToken);

        await PruneAsync(cancellationToken);

        return entry;
    }

    public async Task<GameSave?> TryLoadAsync(GameSaveId id, CancellationToken cancellationToken = default)
    {
        using IDisposable operation = await BeginOperationAsync(cancellationToken);

        string savePath = GetSavePath(id);

        try
        {
            int format = await ReadAsync(savePath, JsonFormatVersionReader.ReadAsync, cancellationToken);

            if (format != GameSave.CurrentFormatVersion)
            {
                throw new GameSaveUnsupportedFormatException(
                    $"Unable to load the saved game from '{savePath}': Unsupported save format '{format}'.",
                    metadata: null);
            }

            return await ReadAsync(savePath, GameSaveJsonSerializerContext.Default.GameSave.DeserializeRequiredAsync, cancellationToken);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            return null;
        }
    }

    public async Task DeleteAsync(GameSaveId id, CancellationToken cancellationToken = default)
    {
        using IDisposable operation = await BeginOperationAsync(cancellationToken);

        File.Delete(GetSavePath(id));
    }

    private static string GetSaveFileName(GameSaveId id)
    {
        return $"{SaveFilePrefix}{id.Number}{SaveFileExtension}";
    }

    private static GameSaveId? TryParseId(string fileName)
    {
        if (!fileName.StartsWith(SaveFilePrefix, StringComparison.OrdinalIgnoreCase) ||
            !fileName.EndsWith(SaveFileExtension, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        ReadOnlySpan<char> number = fileName.AsSpan(SaveFilePrefix.Length..^SaveFileExtension.Length);

        if (!int.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out int parsed))
        {
            return null;
        }

        if (parsed <= 0)
        {
            return null;
        }

        string canonical = parsed.ToString(CultureInfo.InvariantCulture);

        if (!number.SequenceEqual(canonical))
        {
            return null;
        }

        return new GameSaveId(parsed);
    }

    private static int CompareEntries(GameSaveEntry left, GameSaveEntry right)
    {
        return left.Id.Number.CompareTo(right.Id.Number);
    }

    private static int CompareNewestFirst(GameSaveEntry left, GameSaveEntry right)
    {
        int rv;

        rv = right.Metadata.Value.SavedAt.CompareTo(left.Metadata.Value.SavedAt);
        if (rv != 0)
        {
            return rv;
        }

        rv = right.Id.Number.CompareTo(left.Id.Number);
        if (rv != 0)
        {
            return rv;
        }

        return 0;
    }

    private static FileStream OpenRead(string savePath)
    {
        return new FileStream(
            savePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            BufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
    }

    private static async Task<T> ReadAsync<T>(
        string savePath,
        Func<Stream, CancellationToken, Task<T>> readFunc,
        CancellationToken cancellationToken)
    {
        try
        {
            await using FileStream stream = OpenRead(savePath);
            await using GZipStream decompressStream = new(stream, CompressionMode.Decompress);

            return await readFunc(decompressStream, cancellationToken);
        }
        catch (Exception ex) when (ex is JsonException or InvalidDataException)
        {
            throw new GameSaveInvalidException(
                $"Unable to load the saved game from '{savePath}': {ex.Message}",
                ex);
        }
    }

    private static async Task<GameSaveMetadata> ReadMetadataAsync(string savePath, CancellationToken cancellationToken)
    {
        int format = await ReadAsync(savePath, JsonFormatVersionReader.ReadAsync, cancellationToken);

        if (format == GameSave.CurrentFormatVersion)
        {
            return await ReadAsync(savePath, GameSaveMetadataReader.ReadAsync, cancellationToken);
        }
        else
        {
            GameSaveMetadata? metadata = await TryReadMetadataAsync(savePath, cancellationToken);

            throw new GameSaveUnsupportedFormatException(
                $"Unable to load the saved game from '{savePath}': Unsupported save format '{format}'.",
                metadata);
        }
    }

    private static async Task<GameSaveMetadata?> TryReadMetadataAsync(string savePath, CancellationToken cancellationToken)
    {
        try
        {
            return await ReadAsync(savePath, GameSaveMetadataReader.ReadAsync, cancellationToken);
        }
        catch (GameSaveInvalidException)
        {
            return null;
        }
    }

    private async Task<IDisposable> BeginOperationAsync(CancellationToken cancellationToken)
    {
        await operationLock.WaitAsync(cancellationToken);

        return new ActionDisposable(() => operationLock.Release());
    }

    private async Task<IReadOnlyList<GameSaveEntry>> ReadEntriesAsync(CancellationToken cancellationToken)
    {
        List<GameSaveEntry> entries = [];

        foreach (GameSaveId id in EnumerateIds())
        {
            string savePath = GetSavePath(id);

            try
            {
                GameSaveMetadata metadata = await ReadMetadataAsync(savePath, cancellationToken);

                entries.Add(new GameSaveEntry(id, Result<GameSaveMetadata>.FromValue(metadata)));
            }
            catch (GameSaveUnsupportedFormatException ex)
            {
                entries.Add(new GameSaveEntry(id, Result<GameSaveMetadata>.FromException(ex)));
            }
            catch (GameSaveInvalidException ex)
            {
                entries.Add(new GameSaveEntry(id, Result<GameSaveMetadata>.FromException(ex)));
            }
        }

        entries.Sort(CompareEntries);

        return entries;
    }

    private async Task PruneAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<GameSaveEntry> entries = await ReadEntriesAsync(cancellationToken);

        List<GameSaveEntry> readableEntries = [];

        foreach (GameSaveEntry entry in entries)
        {
            if (entry.Metadata.IsSuccessful)
            {
                readableEntries.Add(entry);
            }
        }

        readableEntries.Sort(CompareNewestFirst);

        Dictionary<(GameSaveType Type, string? ScenarioId), int> keptCounts = [];

        foreach (GameSaveEntry entry in readableEntries)
        {
            GameSaveMetadata metadata = entry.Metadata.Value;
            (GameSaveType Type, string? ScenarioId) group = GetGroup(metadata);
            int toKeep = keptCounts.GetValueOrDefault(group);

            if (toKeep < GetKeepCount(metadata.Type))
            {
                keptCounts[group] = toKeep + 1;
            }
            else
            {
                File.Delete(GetSavePath(entry.Id));
            }
        }

        static (GameSaveType Type, string? ScenarioId) GetGroup(GameSaveMetadata metadata)
        {
            return metadata.Type switch
            {
                GameSaveType.Turn => (metadata.Type, null),
                GameSaveType.Victory => (metadata.Type, metadata.ScenarioId),
                GameSaveType.Exit => (metadata.Type, metadata.ScenarioId),
                GameSaveType.Manual => (metadata.Type, metadata.ScenarioId)
            };
        }
    }

    private int GetKeepCount(GameSaveType type)
    {
        return type switch
        {
            GameSaveType.Turn => AutoSaveLimit,
            GameSaveType.Victory => int.MaxValue,
            GameSaveType.Exit => 1,
            GameSaveType.Manual => int.MaxValue
        };
    }

    private async Task<GameSaveEntry> WriteAsync(
        GameSaveId id,
        GameSaveMetadata metadata,
        WorldState worldState,
        CancellationToken cancellationToken)
    {
        if (metadata.Name.Length > GameSaveMetadata.MaxNameLength)
        {
            throw new ArgumentOutOfRangeException(nameof(metadata), metadata, null);
        }

        Directory.CreateDirectory(saveDirectory);

        using AtomicFileWriter writer = new(
            GetSavePath(id),
            FileOptions.Asynchronous | FileOptions.SequentialScan,
            BufferSize,
            flushToDisk: true);

        await using (GZipStream compressStream = new(writer.Stream, CompressionLevel.Optimal, leaveOpen: true))
        {
            await JsonSerializer.SerializeAsync(
                compressStream,
                GameSave.Create(metadata, worldState),
                GameSaveJsonSerializerContext.Default.GameSave,
                cancellationToken);
        }

        await writer.CommitAsync();

        return new GameSaveEntry(id, Result<GameSaveMetadata>.FromValue(metadata));
    }

    private IEnumerable<GameSaveId> EnumerateIds()
    {
        if (!Directory.Exists(saveDirectory))
        {
            yield break;
        }

        foreach (string path in Directory.EnumerateFiles(saveDirectory, SaveFilePattern))
        {
            if (TryParseId(Path.GetFileName(path)) is { } id)
            {
                yield return id;
            }
        }
    }

    private GameSaveId GetNewId()
    {
        int highest = 0;

        foreach (GameSaveId id in EnumerateIds())
        {
            highest = Math.Max(highest, id.Number);
        }

        return new GameSaveId(highest + 1);
    }

    private string GetSavePath(GameSaveId id)
    {
        return Path.Combine(saveDirectory, GetSaveFileName(id));
    }
}
