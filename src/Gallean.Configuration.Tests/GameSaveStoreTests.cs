using System.IO.Compression;
using System.Text;
using Gallean.Common;
using Gallean.Engine;
using Gallean.Engine.Contracts;
using Gallean.Engine.Tests;
using Gallean.Tests.Common;

namespace Gallean.Configuration.Tests;

public sealed class GameSaveStoreTests
{
    private static List<string> FormatEntries(IReadOnlyList<GameSaveEntry> entries)
    {
        List<string> rows = [];

        foreach (GameSaveEntry entry in entries)
        {
            try
            {
                rows.Add($"{entry.Id} | {entry.Metadata.Value.Name}");
            }
            catch (Exception ex)
            {
                rows.Add($"{entry.Id} | {ex.GetType().Name} | {ex.Message}");
            }
        }

        return rows;
    }

    private static List<GameSaveMetadata?> GetIncompatibleMetadata(IReadOnlyList<GameSaveEntry> entries)
    {
        List<GameSaveMetadata?> metadata = [];

        foreach (GameSaveEntry entry in entries)
        {
            try
            {
                _ = entry.Metadata.Value;
            }
            catch (GameSaveUnsupportedFormatException ex)
            {
                metadata.Add(ex.Metadata);
            }
        }

        return metadata;
    }

    [Test]
    public async Task SaveOverwriteTest()
    {
        using Sandbox sb = Sandbox.Create();

        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();
        WorldState initial = session.World.State;
        WorldState replacement = initial with { TurnInfo = initial.TurnInfo with { TurnNumber = 19 } };
        GameSaveMetadata first = new("First", GameSaveType.Manual, "scenario-1", "Scenario One", 1, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        GameSaveMetadata second = new("Second", GameSaveType.Manual, "scenario-2", "Scenario Two", 19, new DateTimeOffset(2026, 1, 3, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        GameSaveStore store = new(sb.GetPath("saves"), 10);

        await store.SaveAsync(new GameSaveId(1), first, initial);
        await store.SaveAsync(new GameSaveId(1), second, replacement);

        Assert.That(await store.TryLoadAsync(new GameSaveId(1)), Is.EqualTo(GameSave.Create(second, replacement)));
        Assert.That(sb.GetTree(), Is.EquivalentTo(["saves/", "saves/save-1.json.gz"]));
    }

    [Test]
    public async Task SaveSeveralTest()
    {
        using Sandbox sb = Sandbox.Create();

        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();
        WorldState state = session.World.State;
        GameSaveMetadata first = new("First", GameSaveType.Manual, "scenario-1", "Scenario One", 1, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        GameSaveMetadata second = new("Second", GameSaveType.Manual, "scenario-1", "Scenario One", 4, new DateTimeOffset(2026, 1, 3, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        GameSaveStore store = new(sb.GetPath("saves"), 10);

        await store.SaveAsync(new GameSaveId(1), first, state);
        await store.SaveAsync(new GameSaveId(2), second, state);

        Assert.That(await store.TryLoadAsync(new GameSaveId(1)), Is.EqualTo(GameSave.Create(first, state)));
        Assert.That(await store.TryLoadAsync(new GameSaveId(2)), Is.EqualTo(GameSave.Create(second, state)));
        Assert.That(sb.GetTree(), Is.EquivalentTo(["saves/", "saves/save-1.json.gz", "saves/save-2.json.gz"]));
    }

    [Test]
    public async Task SaveWithoutIdTest()
    {
        using Sandbox sb = Sandbox.Create();

        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();
        WorldState state = session.World.State;
        GameSaveMetadata metadata = new("First", GameSaveType.Manual, "scenario-1", "Scenario One", 1, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        GameSaveStore store = new(sb.GetPath("saves"), 10);

        Assert.That(
            await store.SaveAsync(id: null, metadata, state),
            Is.EqualTo(new GameSaveEntry(new GameSaveId(1), Result<GameSaveMetadata>.FromValue(metadata))));

        await store.SaveAsync(new GameSaveId(4), metadata, state);

        Assert.That(
            await store.SaveAsync(id: null, metadata, state),
            Is.EqualTo(new GameSaveEntry(new GameSaveId(5), Result<GameSaveMetadata>.FromValue(metadata))));
        Assert.That(
            sb.GetTree(),
            Is.EquivalentTo(["saves/", "saves/save-1.json.gz", "saves/save-4.json.gz", "saves/save-5.json.gz"]));
    }

    [Test]
    public async Task EnumerateTest()
    {
        using Sandbox sb = Sandbox.Create();

        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();
        WorldState state = session.World.State;
        GameSaveMetadata first = new("First", GameSaveType.Manual, "scenario-1", "Scenario One", 1, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        GameSaveMetadata second = new("Second", GameSaveType.Manual, "scenario-2", "Scenario Two", 12, new DateTimeOffset(2026, 3, 4, 5, 6, 7, TimeSpan.Zero), "0.1.0+2d40300");
        GameSaveMetadata third = new("Third", GameSaveType.Manual, "scenario-3", "Scenario Three", 30, new DateTimeOffset(2026, 2, 1, 1, 1, 1, TimeSpan.Zero), "0.1.0+2d40300");
        GameSaveStore store = new(sb.GetPath("saves"), 10);

        await store.SaveAsync(new GameSaveId(1), first, state);
        await store.SaveAsync(new GameSaveId(2), second, state);
        await store.SaveAsync(new GameSaveId(3), third, state);

        Assert.That(
            await store.EnumerateAsync(),
            Is.EqualTo([
                new GameSaveEntry(new GameSaveId(1), Result<GameSaveMetadata>.FromValue(first)),
                new GameSaveEntry(new GameSaveId(2), Result<GameSaveMetadata>.FromValue(second)),
                new GameSaveEntry(new GameSaveId(3), Result<GameSaveMetadata>.FromValue(third))
            ]));
    }

    [Test]
    public async Task EnumerateNonCanonicalNameTest()
    {
        using Sandbox sb = Sandbox.Create();

        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();
        WorldState state = session.World.State;
        GameSaveMetadata metadata = new("First", GameSaveType.Manual, "scenario-1", "Scenario One", 1, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        GameSaveStore store = new(sb.GetPath("saves"), 10);

        await store.SaveAsync(new GameSaveId(1), metadata, state);
        File.Copy(
            sb.GetPath(Path.Combine("saves", "save-1.json.gz")),
            sb.GetPath(Path.Combine("saves", "save-02.json.gz")));

        Assert.That(
            await store.EnumerateAsync(),
            Is.EqualTo([new GameSaveEntry(new GameSaveId(1), Result<GameSaveMetadata>.FromValue(metadata))]));
    }

    [Test]
    public async Task EnumerateLockedTest()
    {
        using Sandbox sb = Sandbox.Create();

        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();
        WorldState state = session.World.State;
        GameSaveMetadata first = new("First", GameSaveType.Manual, "scenario-1", "Scenario One", 1, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        GameSaveMetadata second = new("Second", GameSaveType.Manual, "scenario-1", "Scenario One", 4, new DateTimeOffset(2026, 1, 3, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        GameSaveStore store = new(sb.GetPath("saves"), 10);

        await store.SaveAsync(new GameSaveId(1), first, state);
        await store.SaveAsync(new GameSaveId(2), second, state);

        string savePath = sb.GetPath(Path.Combine("saves", "save-2.json.gz"));

        await using FileStream lockingStream = new(savePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

        Assert.That(
            async () => await store.EnumerateAsync(),
            Throws.TypeOf<IOException>().With.Message.EqualTo(
                $"The process cannot access the file \'{savePath}\' because it is being used by another process."));
    }

    [Test]
    public async Task EnumerateLongNameTest()
    {
        using Sandbox sb = Sandbox.Create();

        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();
        WorldState state = session.World.State;
        GameSaveMetadata metadata = new(
            new string('\u044f', 256),
            GameSaveType.Manual,
            "scenario-1",
            "Scenario One",
            1,
            new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
            "0.1.0+2d40300");
        GameSaveStore store = new(sb.GetPath("saves"), 10);

        await store.SaveAsync(new GameSaveId(1), metadata, state);

        Assert.That(
            await store.EnumerateAsync(),
            Is.EqualTo([new GameSaveEntry(new GameSaveId(1), Result<GameSaveMetadata>.FromValue(metadata))]));
    }

    [Test]
    public async Task SaveLongNameTest()
    {
        using Sandbox sb = Sandbox.Create();

        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();
        WorldState state = session.World.State;
        GameSaveMetadata metadata = new(
            new string('\u044f', 257),
            GameSaveType.Manual,
            "scenario-1",
            "Scenario One",
            1,
            new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
            "0.1.0+2d40300");
        GameSaveStore store = new(sb.GetPath("saves"), 10);

        Assert.That(
            async () => await store.SaveAsync(new GameSaveId(1), metadata, state),
            Throws.InstanceOf<ArgumentOutOfRangeException>());

        Assert.That(sb.GetTree(), Is.Empty);
    }

    [Test]
    public async Task EnumerateLongMetadataTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameSaveStore store = new(sb.GetPath("saves"), 10);
        string savePath = sb.GetPath(Path.Combine("saves", "save-1.json.gz"));

        sb.CreateDirectory("saves");

        await using (FileStream stream = File.Create(savePath))
        await using (GZipStream compressStream = new(stream, CompressionLevel.Optimal))
        {
            compressStream.Write(Encoding.UTF8.GetBytes(
                $$$"""{"formatVersion":{{{GameSave.CurrentFormatVersion}}},"metadata":{"name":"{{{new string('a', 5000)}}}","type":"Manual","scenarioId":"scenario-1","scenarioName":"Scenario One","turnNumber":1,"savedAt":"2026-01-02T03:04:05+00:00","savedBy":"0.1.0+2d40300"}}"""));
        }

        Assert.That(
            FormatEntries(await store.EnumerateAsync()),
            Is.EqualTo([$"1 | GameSaveInvalidException | Unable to load the saved game from '{savePath}': The save is invalid."]));
    }

    [Test]
    public async Task EnumerateMissingDirTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameSaveStore store = new(sb.GetPath("saves"), 10);

        Assert.That(await store.EnumerateAsync(), Is.Empty);
    }

    [Test]
    public async Task EnumerateUnreadableTest()
    {
        using Sandbox sb = Sandbox.Create();

        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();
        WorldState state = session.World.State;
        GameSaveMetadata metadata = new("First", GameSaveType.Manual, "scenario-1", "Scenario One", 1, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        GameSaveStore store = new(sb.GetPath("saves"), 10);

        await store.SaveAsync(new GameSaveId(1), metadata, state);

        await using (FileStream stream = File.Create(sb.GetPath(Path.Combine("saves", "save-2.json.gz"))))
        await using (GZipStream compressStream = new(stream, CompressionLevel.Optimal))
        {
            compressStream.Write("""{"formatVersion":1,"scenarioId":"scenario-1"}"""u8);
        }

        await using (FileStream stream = File.Create(sb.GetPath(Path.Combine("saves", "save-3.json.gz"))))
        await using (GZipStream compressStream = new(stream, CompressionLevel.Optimal))
        {
            compressStream.Write("{not-json"u8);
        }

        await using (FileStream stream = File.Create(sb.GetPath(Path.Combine("saves", "save-4.json.gz"))))
        await using (GZipStream compressStream = new(stream, CompressionLevel.Optimal))
        {
            compressStream.Write(Encoding.UTF8.GetBytes(
                $$"""{"version":{{GameSave.CurrentFormatVersion}},"name":"Stray","scenarioId":"scenario-1","scenarioName":"Scenario One","turnNumber":1,"savedAt":"2026-01-02T03:04:05+00:00"}"""));
        }

        await using (FileStream stream = File.Create(sb.GetPath(Path.Combine("saves", "save-x.json.gz"))))
        await using (GZipStream compressStream = new(stream, CompressionLevel.Optimal))
        {
            compressStream.Write("""{"formatVersion":21}"""u8);
        }

        sb.WriteTextFile(Path.Combine("saves", "notes.txt"), "ignored");

        string savesPath = sb.GetPath("saves");

        Assert.That(
            FormatEntries(await store.EnumerateAsync()),
            Is.EqualTo([
                "1 | First",
                $"2 | GameSaveUnsupportedFormatException | Unable to load the saved game from '{Path.Combine(savesPath, "save-2.json.gz")}': Unsupported save format '1'.",
                $"3 | GameSaveInvalidException | Unable to load the saved game from '{Path.Combine(savesPath, "save-3.json.gz")}': 'n' is an invalid start of a property name. Expected a '\"'. LineNumber: 0 | BytePositionInLine: 1.",
                $"4 | GameSaveInvalidException | Unable to load the saved game from '{Path.Combine(savesPath, "save-4.json.gz")}': The file does not start with a format version."
            ]));
    }

    [Test]
    public async Task DeleteTest()
    {
        using Sandbox sb = Sandbox.Create();

        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();
        WorldState state = session.World.State;
        GameSaveMetadata metadata = new("First", GameSaveType.Manual, "scenario-1", "Scenario One", 1, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        GameSaveStore store = new(sb.GetPath("saves"), 10);

        await store.SaveAsync(new GameSaveId(1), metadata, state);
        await store.DeleteAsync(new GameSaveId(1));

        Assert.That(await store.TryLoadAsync(new GameSaveId(1)), Is.Null);
        Assert.That(sb.GetTree(), Is.EquivalentTo(["saves/"]));
    }

    [Test]
    public async Task SaveCompressedTest()
    {
        using Sandbox sb = Sandbox.Create();

        WorldMap map = TestHelpers.SimpleMap(4, 4);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();
        WorldState state = session.World.State;
        GameSaveMetadata metadata = new("First", GameSaveType.Manual, "scenario-1", "Scenario One", 1, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        GameSaveStore store = new(sb.GetPath("saves"), 10);

        await store.SaveAsync(new GameSaveId(1), metadata, state);

        byte[] save = await File.ReadAllBytesAsync(sb.GetPath(Path.Combine("saves", "save-1.json.gz")));

        Assert.That(save[..3], Is.EqualTo(new byte[] { 0x1f, 0x8b, 0x08 }));
    }

    [Test]
    public async Task LoadMissingDirTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameSaveStore store = new(sb.GetPath("saves"), 10);

        Assert.That(await store.TryLoadAsync(new GameSaveId(1)), Is.Null);
    }

    [Test]
    public async Task LoadFormatFieldOrderTest()
    {
        using Sandbox sb = Sandbox.Create();

        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();
        WorldState state = session.World.State;
        GameSaveMetadata metadata = new("First", GameSaveType.Manual, "scenario-1", "Scenario One", 1, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        GameSaveStore store = new(sb.GetPath("saves"), 10);

        await store.SaveAsync(new GameSaveId(1), metadata, state);

        await using FileStream stream = File.OpenRead(sb.GetPath(Path.Combine("saves", "save-1.json.gz")));
        await using GZipStream decompressStream = new(stream, CompressionMode.Decompress);
        using StreamReader reader = new(decompressStream);

        Assert.That(
            await reader.ReadToEndAsync(),
            Does.StartWith(
                $$"""{"formatVersion":{{GameSave.CurrentFormatVersion}},"metadata":{"name":"First","type":"Manual","scenarioId":"scenario-1","scenarioName":"Scenario One","turnNumber":1,"savedAt":"2026-01-02T03:04:05+00:00","savedBy":"0.1.0\u002B2d40300"},"""));
    }

    [Test]
    public void LoadOldFormatTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameSaveStore store = new(sb.GetPath("saves"), 10);
        string savePath = sb.GetPath(Path.Combine("saves", "save-1.json.gz"));

        sb.CreateDirectory("saves");

        using (FileStream stream = File.Create(savePath))
        using (GZipStream compressStream = new(stream, CompressionLevel.Optimal))
        {
            compressStream.Write("""{"formatVersion":1,"scenarioId":"scenario-1","worldState":{"stacks":{"1":{"inventory":["G000IG0001"]}}}}"""u8);
        }

        Assert.That(
            async () => await store.TryLoadAsync(new GameSaveId(1)),
            Throws.TypeOf<GameSaveUnsupportedFormatException>().With.Message.EqualTo(
                $"Unable to load the saved game from '{savePath}': " +
                $"Unsupported save format '1'."));
    }

    [Test]
    public void LoadEmptySaveTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameSaveStore store = new(sb.GetPath("saves"), 10);
        string savePath = sb.GetPath(Path.Combine("saves", "save-1.json.gz"));

        sb.CreateDirectory("saves");

        using (FileStream stream = File.Create(savePath))
        using (GZipStream compressStream = new(stream, CompressionLevel.Optimal))
        {
            compressStream.Write(Encoding.UTF8.GetBytes(string.Empty));
        }

        Assert.That(
            async () => await store.TryLoadAsync(new GameSaveId(1)),
            Throws.TypeOf<GameSaveInvalidException>().With.Message.EqualTo(
                $"Unable to load the saved game from '{savePath}': " +
                "The input does not contain any JSON tokens. " +
                "Expected the input to start with a valid JSON token, when isFinalBlock is true. " +
                "LineNumber: 0 | BytePositionInLine: 0."));
    }

    [Test]
    public void LoadInvalidSaveTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameSaveStore store = new(sb.GetPath("saves"), 10);
        string savePath = sb.GetPath(Path.Combine("saves", "save-1.json.gz"));

        sb.CreateDirectory("saves");

        using (FileStream stream = File.Create(savePath))
        using (GZipStream compressStream = new(stream, CompressionLevel.Optimal))
        {
            compressStream.Write("{not-json"u8);
        }

        Assert.That(
            async () => await store.TryLoadAsync(new GameSaveId(1)),
            Throws.TypeOf<GameSaveInvalidException>().With.Message.EqualTo(
                $"Unable to load the saved game from '{savePath}': " +
                """'n' is an invalid start of a property name. Expected a '"'. LineNumber: 0 | BytePositionInLine: 1."""));
    }

    [Test]
    public void LoadUncompressedSaveTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameSaveStore store = new(sb.GetPath("saves"), 10);

        sb.CreateDirectory("saves");
        sb.WriteTextFile(
            Path.Combine("saves", "save-1.json.gz"),
            $$"""{"formatVersion":{{GameSave.CurrentFormatVersion}},"scenarioId":"scenario-1"}""");

        Assert.That(
            async () => await store.TryLoadAsync(new GameSaveId(1)),
            Throws.TypeOf<GameSaveInvalidException>().With.Message.EqualTo(
                $"Unable to load the saved game from '{sb.GetPath(Path.Combine("saves", "save-1.json.gz"))}': " +
                "The archive entry was compressed using an unsupported compression method."));
    }

    [Test]
    public void LoadInvalidFormatFieldOrderTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameSaveStore store = new(sb.GetPath("saves"), 10);
        string savePath = sb.GetPath(Path.Combine("saves", "save-1.json.gz"));

        sb.CreateDirectory("saves");

        using (FileStream stream = File.Create(savePath))
        using (GZipStream compressStream = new(stream, CompressionLevel.Optimal))
        {
            compressStream.Write(Encoding.UTF8.GetBytes(
                $$"""{"scenarioId":"scenario-1","formatVersion":{{GameSave.CurrentFormatVersion}}}"""));
        }

        Assert.That(
            async () => await store.TryLoadAsync(new GameSaveId(1)),
            Throws.TypeOf<GameSaveInvalidException>().With.Message.EqualTo(
                $"Unable to load the saved game from '{savePath}': " +
                "The file does not start with a format version."));
    }

    [Test]
    public void LoadLockedTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameSaveStore store = new(sb.GetPath("saves"), 10);
        string savePath = sb.GetPath(Path.Combine("saves", "save-1.json.gz"));

        sb.CreateDirectory("saves");

        using FileStream lockingStream = new(savePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);

        Assert.That(
            async () => await store.TryLoadAsync(new GameSaveId(1)),
            Throws.TypeOf<IOException>().With.Message.EqualTo(
                $"The process cannot access the file '{savePath}' because it is being used by another process."));
    }

    [Test]
    public void LoadMissingWorldStateTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameSaveStore store = new(sb.GetPath("saves"), 10);
        string savePath = sb.GetPath(Path.Combine("saves", "save-1.json.gz"));

        sb.CreateDirectory("saves");

        using (FileStream stream = File.Create(savePath))
        using (GZipStream compressStream = new(stream, CompressionLevel.Optimal))
        {
            compressStream.Write(Encoding.UTF8.GetBytes($$$"""{"formatVersion":{{{GameSave.CurrentFormatVersion}}},"metadata":{"name":"First","type":"Manual","scenarioId":"scenario-1","scenarioName":"Scenario One","turnNumber":1,"savedAt":"2026-01-02T03:04:05+00:00","savedBy":"0.1.0+2d40300"}}"""));
        }

        Assert.That(
            async () => await store.TryLoadAsync(new GameSaveId(1)),
            Throws.TypeOf<GameSaveInvalidException>().With.Message.EqualTo(
                $"Unable to load the saved game from '{savePath}': " +
                "JSON deserialization for type 'Gallean.Configuration.GameSave' was missing required properties including: 'worldState'."));
    }

    [Test]
    public async Task AutoSaveScenarioTest()
    {
        using Sandbox sb = Sandbox.Create();

        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();
        WorldState state = session.World.State;
        GameSaveMetadata manual = new("Manual", GameSaveType.Manual, "scenario-1", "Scenario One", 4, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        GameSaveMetadata victory = new("Victory", GameSaveType.Victory, "scenario-1", "Scenario One", 5, new DateTimeOffset(2026, 1, 3, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        GameSaveMetadata exit = new("Exit", GameSaveType.Exit, "scenario-1", "Scenario One", 5, new DateTimeOffset(2026, 1, 4, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        GameSaveMetadata otherExit = new("Other exit", GameSaveType.Exit, "scenario-2", "Scenario Two", 7, new DateTimeOffset(2026, 1, 5, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        GameSaveMetadata newVictory = new("New victory", GameSaveType.Victory, "scenario-1", "Scenario One", 10, new DateTimeOffset(2026, 1, 6, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        GameSaveMetadata newExit = new("New exit", GameSaveType.Exit, "scenario-1", "Scenario One", 12, new DateTimeOffset(2026, 1, 7, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        GameSaveStore store = new(sb.GetPath("saves"), 10);

        await store.SaveAsync(null, manual, state);
        await store.SaveAsync(null, victory, state);
        await store.SaveAsync(null, exit, state);
        await store.SaveAsync(null, otherExit, state);

        Assert.That(
            await store.SaveAsync(null, newVictory, state),
            Is.EqualTo(new GameSaveEntry(new GameSaveId(5), Result<GameSaveMetadata>.FromValue(newVictory))));
        Assert.That(
            await store.SaveAsync(null, newExit, state),
            Is.EqualTo(new GameSaveEntry(new GameSaveId(6), Result<GameSaveMetadata>.FromValue(newExit))));
        Assert.That(
            FormatEntries(await store.EnumerateAsync()),
            Is.EqualTo(["1 | Manual", "2 | Victory", "4 | Other exit", "5 | New victory", "6 | New exit"]));
    }

    [Test]
    public async Task EnumerateMisplacedMetadataTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameSaveStore store = new(sb.GetPath("saves"), 10);
        string savePath = sb.GetPath(Path.Combine("saves", "save-1.json.gz"));

        sb.CreateDirectory("saves");

        await using (FileStream stream = File.Create(savePath))
        await using (GZipStream compressStream = new(stream, CompressionLevel.Optimal))
        {
            compressStream.Write(Encoding.UTF8.GetBytes(
                $$$"""{"formatVersion":{{{GameSave.CurrentFormatVersion}}},"worldState":{},"metadata":{"name":"First","type":"Manual","scenarioId":"scenario-1","scenarioName":"Scenario One","turnNumber":1,"savedAt":"2026-01-02T03:04:05+00:00","savedBy":"0.1.0+2d40300"}}"""));
        }

        Assert.That(
            FormatEntries(await store.EnumerateAsync()),
            Is.EqualTo([
                $"1 | GameSaveInvalidException | Unable to load the saved game from '{savePath}': The save is invalid."
            ]));
    }

    [Test]
    public async Task EnumerateInvalidWorldStateTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameSaveStore store = new(sb.GetPath("saves"), 10);
        string savePath = sb.GetPath(Path.Combine("saves", "save-1.json.gz"));
        GameSaveMetadata metadata = new("First", GameSaveType.Manual, "scenario-1", "Scenario One", 1, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");

        sb.CreateDirectory("saves");

        await using (FileStream stream = File.Create(savePath))
        await using (GZipStream compressStream = new(stream, CompressionLevel.Optimal))
        {
            compressStream.Write(Encoding.UTF8.GetBytes(
                $$"""{"formatVersion":{{GameSave.CurrentFormatVersion}},"metadata":{"name":"First","type":"Manual","scenarioId":"scenario-1","scenarioName":"Scenario One","turnNumber":1,"savedAt":"2026-01-02T03:04:05+00:00","savedBy":"0.1.0+2d40300"},"worldState":"not a world"}"""));
        }

        Assert.That(
            await store.EnumerateAsync(),
            Is.EqualTo([new GameSaveEntry(new GameSaveId(1), Result<GameSaveMetadata>.FromValue(metadata))]));
        Assert.That(
            async () => await store.TryLoadAsync(new GameSaveId(1)),
            Throws.TypeOf<GameSaveInvalidException>().With.Message.StartWith(
                $"Unable to load the saved game from '{savePath}': "));
    }

    [Test]
    public async Task EnumerateIncompatibleTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameSaveMetadata older = new("Older", GameSaveType.Manual, "scenario-1", "Scenario One", 4, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.0.9+1c2b3a4");
        GameSaveMetadata newer = new("Newer", GameSaveType.Turn, "scenario-2", "Scenario Two", 7, new DateTimeOffset(2026, 1, 3, 3, 4, 5, TimeSpan.Zero), "0.2.0");
        GameSaveStore store = new(sb.GetPath("saves"), 10);

        sb.CreateDirectory("saves");

        await using (FileStream stream = File.Create(sb.GetPath(Path.Combine("saves", "save-1.json.gz"))))
        await using (GZipStream compressStream = new(stream, CompressionLevel.Optimal))
        {
            compressStream.Write("""{"formatVersion":34,"metadata":{"name":"Older","type":"Manual","scenarioId":"scenario-1","scenarioName":"Scenario One","turnNumber":4,"savedAt":"2026-01-02T03:04:05+00:00","savedBy":"0.0.9+1c2b3a4"},"worldState":{}}"""u8);
        }

        await using (FileStream stream = File.Create(sb.GetPath(Path.Combine("saves", "save-2.json.gz"))))
        await using (GZipStream compressStream = new(stream, CompressionLevel.Optimal))
        {
            compressStream.Write("""{"formatVersion":9000,"metadata":{"name":"Newer","type":"Turn","scenarioId":"scenario-2","scenarioName":"Scenario Two","turnNumber":7,"savedAt":"2026-01-03T03:04:05+00:00","savedBy":"0.2.0","campaign":"next"},"worldState":{}}"""u8);
        }

        string savesPath = sb.GetPath("saves");
        IReadOnlyList<GameSaveEntry> entries = await store.EnumerateAsync();

        Assert.That(
            FormatEntries(entries),
            Is.EqualTo([
                $"1 | GameSaveUnsupportedFormatException | Unable to load the saved game from '{Path.Combine(savesPath, "save-1.json.gz")}': Unsupported save format '34'.",
                $"2 | GameSaveUnsupportedFormatException | Unable to load the saved game from '{Path.Combine(savesPath, "save-2.json.gz")}': Unsupported save format '9000'."
            ]));
        Assert.That(GetIncompatibleMetadata(entries), Is.EqualTo([older, newer]));
    }

    [Test]
    public async Task AutoSaveIncompatibleTest()
    {
        using Sandbox sb = Sandbox.Create();

        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();
        WorldState state = session.World.State;
        GameSaveMetadata replacement = new("Replacement", GameSaveType.Turn, "scenario-1", "Scenario One", 10, new DateTimeOffset(2026, 1, 6, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        GameSaveStore store = new(sb.GetPath("saves"), 1);

        sb.CreateDirectory("saves");

        await using (FileStream stream = File.Create(sb.GetPath(Path.Combine("saves", "save-1.json.gz"))))
        await using (GZipStream compressStream = new(stream, CompressionLevel.Optimal))
        {
            compressStream.Write("""{"formatVersion":34,"metadata":{"name":"Turn","type":"Turn","scenarioId":"scenario-1","scenarioName":"Scenario One","turnNumber":5,"savedAt":"2026-01-03T03:04:05+00:00","savedBy":"0.0.9+1c2b3a4"},"worldState":{}}"""u8);
        }

        Assert.That(
            await store.SaveAsync(null, replacement, state),
            Is.EqualTo(new GameSaveEntry(new GameSaveId(2), Result<GameSaveMetadata>.FromValue(replacement))));
        Assert.That(
            FormatEntries(await store.EnumerateAsync()),
            Is.EqualTo([
                $"1 | GameSaveUnsupportedFormatException | Unable to load the saved game from '{sb.GetPath(Path.Combine("saves", "save-1.json.gz"))}': Unsupported save format '34'.",
                "2 | Replacement"
            ]));
    }

    [Test]
    public async Task EnumerateWithoutVersionTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameSaveMetadata metadata = new("First", GameSaveType.Manual, "scenario-1", "Scenario One", 1, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero));
        GameSaveStore store = new(sb.GetPath("saves"), 10);

        sb.CreateDirectory("saves");

        await using (FileStream stream = File.Create(sb.GetPath(Path.Combine("saves", "save-1.json.gz"))))
        await using (GZipStream compressStream = new(stream, CompressionLevel.Optimal))
        {
            compressStream.Write(Encoding.UTF8.GetBytes(
                $$$"""{"formatVersion":{{{GameSave.CurrentFormatVersion}}},"metadata":{"name":"First","type":"Manual","scenarioId":"scenario-1","scenarioName":"Scenario One","turnNumber":1,"savedAt":"2026-01-02T03:04:05+00:00"},"worldState":{}}"""));
        }

        Assert.That(
            await store.EnumerateAsync(),
            Is.EqualTo([new GameSaveEntry(new GameSaveId(1), Result<GameSaveMetadata>.FromValue(metadata))]));
    }

    [Test]
    public async Task AutoSaveTurnTest()
    {
        using Sandbox sb = Sandbox.Create();

        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();
        WorldState state = session.World.State;
        GameSaveMetadata manual = new("Manual", GameSaveType.Manual, "scenario-1", "Scenario One", 4, new DateTimeOffset(2026, 1, 1, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        GameSaveMetadata oldest = new("Oldest", GameSaveType.Turn, "scenario-1", "Scenario One", 3, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        GameSaveMetadata victory = new("Victory", GameSaveType.Victory, "scenario-1", "Scenario One", 4, new DateTimeOffset(2026, 1, 3, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        GameSaveMetadata older = new("Older", GameSaveType.Turn, "scenario-2", "Scenario Two", 6, new DateTimeOffset(2026, 1, 4, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        GameSaveMetadata newer = new("Newer", GameSaveType.Turn, "scenario-1", "Scenario One", 9, new DateTimeOffset(2026, 1, 5, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        GameSaveMetadata newest = new("Newest", GameSaveType.Turn, "scenario-2", "Scenario Two", 12, new DateTimeOffset(2026, 1, 6, 3, 4, 5, TimeSpan.Zero), "0.1.0+2d40300");
        GameSaveStore store = new(sb.GetPath("saves"), 3);

        await store.SaveAsync(null, manual, state);
        await store.SaveAsync(null, oldest, state);
        await store.SaveAsync(null, victory, state);
        await store.SaveAsync(null, older, state);

        Assert.That(
            await store.SaveAsync(null, newer, state),
            Is.EqualTo(new GameSaveEntry(new GameSaveId(5), Result<GameSaveMetadata>.FromValue(newer))));
        Assert.That(
            FormatEntries(await store.EnumerateAsync()),
            Is.EqualTo(["1 | Manual", "2 | Oldest", "3 | Victory", "4 | Older", "5 | Newer"]));
        store.AutoSaveLimit = 2;

        Assert.That(
            await store.SaveAsync(null, newest, state),
            Is.EqualTo(new GameSaveEntry(new GameSaveId(6), Result<GameSaveMetadata>.FromValue(newest))));
        Assert.That(
            FormatEntries(await store.EnumerateAsync()),
            Is.EqualTo(["1 | Manual", "3 | Victory", "5 | Newer", "6 | Newest"]));
    }
}
