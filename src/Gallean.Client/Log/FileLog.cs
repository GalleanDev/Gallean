using System.Diagnostics;
using System.Globalization;

namespace Gallean.Client;

internal sealed class FileLog(string directory, TimeProvider timeProvider) : ILog
{
    private const int MaxLogFiles = 50;

    private readonly Lazy<string?> logFile = new(() => CreateLogFile(directory, timeProvider));
    private readonly Lock locker = new();

    public string? Location
    {
        get
        {
            lock (locker)
            {
                if (logFile.IsValueCreated)
                {
                    return logFile.Value;
                }
                else
                {
                    return null;
                }
            }
        }
    }

    public void Write(string text)
    {
        DateTimeOffset now = timeProvider.GetLocalNow();
        string timestamp = now.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", CultureInfo.InvariantCulture);
        string logEntry = $"[{timestamp}] {text}{Environment.NewLine}";

        string? path = logFile.Value;

        if (path is not null)
        {
            lock (locker)
            {
                try
                {
                    File.AppendAllText(path, logEntry);
                }
                catch (Exception ex)
                {
                    Debug.Fail($"Failed to write to the log file: {ex}");
                }
            }
        }
    }

    private static string CreateLogFile(string directory, TimeProvider time)
    {
        try
        {
            Directory.CreateDirectory(directory);

            DeleteOldLogFiles(directory);

            DateTimeOffset now = time.GetUtcNow();
            string timestamp = now.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);

            return Path.Combine(directory, $"client-{timestamp}Z.log");
        }
        catch (Exception ex)
        {
            Debug.Fail($"Failed to create the log file: {ex}");

            return null;
        }
    }

    private static void DeleteOldLogFiles(string directory)
    {
        DirectoryInfo directoryInfo = new(directory);

        FileInfo[] files = directoryInfo.GetFiles("client-*.log");
        Array.Sort(files, static (a, b) => b.LastWriteTimeUtc.CompareTo(a.LastWriteTimeUtc));

        for (int index = MaxLogFiles - 1; index < files.Length; index++)
        {
            FileInfo file = files[index];

            try
            {
                file.Delete();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to delete the old log file '{file.FullName}': {ex}");
            }
        }
    }
}
