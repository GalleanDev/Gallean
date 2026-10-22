using Gallean.Tests.Common;

namespace Gallean.Client.Tests;

public sealed class FileLogTests
{
    [Test]
    public void WriteTest()
    {
        using Sandbox sb = Sandbox.Create();

        TimeZoneInfo timeZone = TimeZoneInfo.CreateCustomTimeZone("Test", TimeSpan.FromHours(2.0), "Test", "Test");
        TestTimeProvider timeProvider = new(new DateTimeOffset(2026, 1, 2, 23, 4, 5, 678, TimeSpan.Zero), timeZone);
        string logDirectory = sb.GetPath("logs");
        FileLog log = new(logDirectory, timeProvider);

        log.Write("First entry.");
        log.Write("Second entry.");

        Assert.That(sb.GetTree(), Is.EquivalentTo(["logs/", "logs/client-20260102-230405-678Z.log"]));
        Assert.That(log.Location, Is.EqualTo(Path.Combine(logDirectory, "client-20260102-230405-678Z.log")));
        Assert.That(File.ReadAllLines(Path.Combine(logDirectory, "client-20260102-230405-678Z.log")), Is.EqualTo(
        [
            "[2026-01-03T01:04:05.678+02:00] First entry.",
            "[2026-01-03T01:04:05.678+02:00] Second entry."
        ]));
    }

    [Test]
    public void NoEntriesTest()
    {
        using Sandbox sb = Sandbox.Create();

        TimeZoneInfo timeZone = TimeZoneInfo.CreateCustomTimeZone("Test", TimeSpan.FromHours(2.0), "Test", "Test");
        TestTimeProvider timeProvider = new(new DateTimeOffset(2026, 1, 2, 23, 4, 5, 678, TimeSpan.Zero), timeZone);
        FileLog log = new(sb.GetPath("logs"), timeProvider);

        Assert.That(log.Location, Is.Null);
        Assert.That(sb.GetTree(), Is.Empty);
    }

    [Test]
    public void OldLogFilesTest()
    {
        using Sandbox sb = Sandbox.Create();

        string directory = sb.CreateDirectory("logs");

        for (int index = 1; index <= 50; index++)
        {
            string path = Path.Combine(directory, $"client-{index:00}.log");

            File.WriteAllText(path, "Old entry.");
            File.SetLastWriteTimeUtc(path, new DateTime(2026, 1, 1, 0, index, 0, DateTimeKind.Utc));
        }

        TimeZoneInfo timeZone = TimeZoneInfo.CreateCustomTimeZone("Test", TimeSpan.FromHours(2.0), "Test", "Test");
        TestTimeProvider timeProvider = new(new DateTimeOffset(2026, 1, 2, 23, 4, 5, 678, TimeSpan.Zero), timeZone);
        FileLog log = new(directory, timeProvider);

        log.Write("Entry.");

        Assert.That(sb.GetTree(), Is.EquivalentTo(
        [
            "logs/",
            "logs/client-02.log",
            "logs/client-03.log",
            "logs/client-04.log",
            "logs/client-05.log",
            "logs/client-06.log",
            "logs/client-07.log",
            "logs/client-08.log",
            "logs/client-09.log",
            "logs/client-10.log",
            "logs/client-11.log",
            "logs/client-12.log",
            "logs/client-13.log",
            "logs/client-14.log",
            "logs/client-15.log",
            "logs/client-16.log",
            "logs/client-17.log",
            "logs/client-18.log",
            "logs/client-19.log",
            "logs/client-20.log",
            "logs/client-21.log",
            "logs/client-22.log",
            "logs/client-23.log",
            "logs/client-24.log",
            "logs/client-25.log",
            "logs/client-26.log",
            "logs/client-27.log",
            "logs/client-28.log",
            "logs/client-29.log",
            "logs/client-30.log",
            "logs/client-31.log",
            "logs/client-32.log",
            "logs/client-33.log",
            "logs/client-34.log",
            "logs/client-35.log",
            "logs/client-36.log",
            "logs/client-37.log",
            "logs/client-38.log",
            "logs/client-39.log",
            "logs/client-40.log",
            "logs/client-41.log",
            "logs/client-42.log",
            "logs/client-43.log",
            "logs/client-44.log",
            "logs/client-45.log",
            "logs/client-46.log",
            "logs/client-47.log",
            "logs/client-48.log",
            "logs/client-49.log",
            "logs/client-50.log",
            "logs/client-20260102-230405-678Z.log"
        ]));
    }
}
