using System.Globalization;
using System.Text.Json;
using Gallean.Common;
using Gallean.Engine.Contracts;

namespace Gallean.Configuration;

public sealed class LeaderStore(string directory)
{
    private const int BufferSize = 4096;
    private const string FilePrefix = "leader-";
    private const string FileExtension = ".json";
    private const string FilePattern = $"{FilePrefix}*{FileExtension}";

    private readonly SemaphoreSlim operationLock = new(1, 1);

    public async Task<IReadOnlyList<LeaderEntry>> EnumerateAsync(CancellationToken cancellationToken = default)
    {
        using IDisposable operation = await BeginOperationAsync(cancellationToken);

        List<LeaderId> ids = [.. EnumerateIds()];
        List<LeaderEntry> entries = [];

        ids.Sort(CompareIds);

        foreach (LeaderId id in ids)
        {
            LeaderFile? leader = await TryReadAsync(GetPath(id), cancellationToken);

            if (leader is not null)
            {
                entries.Add(new LeaderEntry(id, leader.Leader));
            }
        }

        return entries;
    }

    public async Task<LeaderEntry> SaveAsync(ScenarioLeader leader, CancellationToken cancellationToken = default)
    {
        using IDisposable operation = await BeginOperationAsync(cancellationToken);

        Directory.CreateDirectory(directory);

        LeaderId id = GetNewId();

        using AtomicFileWriter writer = new(GetPath(id), FileOptions.Asynchronous, flushToDisk: true);

        await JsonSerializer.SerializeAsync(
            writer.Stream,
            LeaderFile.Create(leader),
            LeaderJsonSerializerContext.Default.LeaderFile,
            cancellationToken);

        await writer.CommitAsync();

        return new LeaderEntry(id, leader);
    }

    public async Task DeleteAsync(LeaderId id, CancellationToken cancellationToken = default)
    {
        using IDisposable operation = await BeginOperationAsync(cancellationToken);

        File.Delete(GetPath(id));
    }

    private static int CompareIds(LeaderId left, LeaderId right)
    {
        return left.Value.CompareTo(right.Value);
    }

    private static string GetFileName(LeaderId id)
    {
        return $"{FilePrefix}{id.Value}{FileExtension}";
    }

    private static LeaderId? TryParseId(string fileName)
    {
        ReadOnlySpan<char> number = fileName.AsSpan(FilePrefix.Length..^FileExtension.Length);

        if (int.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out int parsed) && parsed > 0)
        {
            return new LeaderId(parsed);
        }
        else
        {
            return null;
        }
    }

    private static async Task<LeaderFile?> TryReadAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            await using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.Asynchronous);

            int format = await JsonFormatVersionReader.ReadAsync(stream, cancellationToken);

            if (format == LeaderFile.CurrentFormatVersion)
            {
                stream.Position = 0;

                return await LeaderJsonSerializerContext.Default.LeaderFile.DeserializeRequiredAsync(stream, cancellationToken);
            }
            else
            {
                return null;
            }
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task<IDisposable> BeginOperationAsync(CancellationToken cancellationToken)
    {
        await operationLock.WaitAsync(cancellationToken);

        return new ActionDisposable(() => operationLock.Release());
    }

    private IEnumerable<LeaderId> EnumerateIds()
    {
        if (!Directory.Exists(directory))
        {
            yield break;
        }

        foreach (string path in Directory.EnumerateFiles(directory, FilePattern))
        {
            if (TryParseId(Path.GetFileName(path)) is { } id)
            {
                yield return id;
            }
        }
    }

    private LeaderId GetNewId()
    {
        int highest = 0;

        foreach (LeaderId id in EnumerateIds())
        {
            highest = Math.Max(highest, id.Value);
        }

        return new LeaderId(highest + 1);
    }

    private string GetPath(LeaderId id)
    {
        return Path.Combine(directory, GetFileName(id));
    }
}
