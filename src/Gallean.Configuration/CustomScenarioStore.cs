using Gallean.Common;

namespace Gallean.Configuration;

public sealed class CustomScenarioStore(string directory)
{
    private readonly SemaphoreSlim operationLock = new(1, 1);

    public async Task<IReadOnlyList<CustomScenarioFile>> EnumerateAsync(CustomScenarioType type, CancellationToken cancellationToken = default)
    {
        using IDisposable operation = await BeginOperationAsync(cancellationToken);

        List<CustomScenarioId> ids = [.. EnumerateIds(type)];
        List<CustomScenarioFile> files = [];

        ids.Sort(CompareIds);

        foreach (CustomScenarioId id in ids)
        {
            files.Add(await ReadAsync(id, type, cancellationToken));
        }

        return files;
    }

    public async Task SaveAsync(CustomScenarioFile file, CancellationToken cancellationToken = default)
    {
        using IDisposable operation = await BeginOperationAsync(cancellationToken);

        Directory.CreateDirectory(directory);

        using AtomicFileWriter writer = new(GetPath(file.Id, file.Type), FileOptions.Asynchronous, flushToDisk: true);

        await writer.Stream.WriteAsync(file.Data, cancellationToken);
        await writer.CommitAsync();
    }

    public async Task<CustomScenarioFile?> TryLoadAsync(CustomScenarioId id, CancellationToken cancellationToken = default)
    {
        using IDisposable operation = await BeginOperationAsync(cancellationToken);

        foreach (CustomScenarioType type in Enum.GetValues<CustomScenarioType>())
        {
            if (File.Exists(GetPath(id, type)))
            {
                return await ReadAsync(id, type, cancellationToken);
            }
        }

        return null;
    }

    public async Task DeleteAsync(CustomScenarioId id, CancellationToken cancellationToken = default)
    {
        using IDisposable operation = await BeginOperationAsync(cancellationToken);

        foreach (CustomScenarioType type in Enum.GetValues<CustomScenarioType>())
        {
            File.Delete(GetPath(id, type));
        }
    }

    private static int CompareIds(CustomScenarioId left, CustomScenarioId right)
    {
        return string.CompareOrdinal(left.Value, right.Value);
    }

    private static string GetFileExtension(CustomScenarioType type)
    {
        return type switch
        {
            CustomScenarioType.Scenario => ".sg",
            CustomScenarioType.Campaign => ".csg"
        };
    }

    private static string GetFileName(CustomScenarioId id, CustomScenarioType type)
    {
        return $"{id.Value}{GetFileExtension(type)}";
    }

    private static string GetFilePattern(CustomScenarioType type)
    {
        return $"*{GetFileExtension(type)}";
    }

    private async Task<IDisposable> BeginOperationAsync(CancellationToken cancellationToken)
    {
        await operationLock.WaitAsync(cancellationToken);

        return new ActionDisposable(() => operationLock.Release());
    }

    private IEnumerable<CustomScenarioId> EnumerateIds(CustomScenarioType type)
    {
        if (!Directory.Exists(directory))
        {
            yield break;
        }

        foreach (string path in Directory.EnumerateFiles(directory, GetFilePattern(type)))
        {
            if (CustomScenarioId.TryParse(Path.GetFileNameWithoutExtension(path)) is { } id)
            {
                yield return id;
            }
        }
    }

    private string GetPath(CustomScenarioId id, CustomScenarioType type)
    {
        return Path.Combine(directory, GetFileName(id, type));
    }

    private async Task<CustomScenarioFile> ReadAsync(CustomScenarioId id, CustomScenarioType type, CancellationToken cancellationToken)
    {
        byte[] data = await File.ReadAllBytesAsync(GetPath(id, type), cancellationToken);

        return new CustomScenarioFile(id, type, data);
    }
}
