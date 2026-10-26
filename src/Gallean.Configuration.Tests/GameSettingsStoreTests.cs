using Gallean.Tests.Common;

namespace Gallean.Configuration.Tests;

public sealed class GameSettingsStoreTests
{
    [Test]
    public async Task SaveReplaceTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameSettingsStore store = new(sb.GetPath("config"));

        store.Save(GameSettings.Create(40, 25, false, 100, 3, 10, BattleSpeed.Normal, null, true, MovementSpeed.Normal));
        store.Save(GameSettings.Create(70, 0, false, 100, 0, 10, BattleSpeed.Normal, null, true, MovementSpeed.Normal));

        Assert.That(await store.TryLoadAsync(), Is.EqualTo(GameSettings.Create(70, 0, false, 100, 0, 10, BattleSpeed.Normal, null, true, MovementSpeed.Normal)));
        Assert.That(sb.GetTree(), Is.EquivalentTo(["config/", "config/settings.json"]));
    }

    [Test]
    public void SaveFormatTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameSettingsStore store = new(sb.RootPath);

        store.Save(GameSettings.Create(40, 25, false, 50, 10, 20, BattleSpeed.Fast, 150, true, MovementSpeed.Fast));

        Assert.That(
            sb.ReadTextFile("settings.json").ReplaceLineEndings("\n"),
            Is.EqualTo(
                "{\n  \"formatVersion\": 6,\n  \"windowed\": false,\n  \"battleSpeed\": \"Fast\",\n  \"lockCursor\": true,\n  \"movementSpeed\": \"Fast\",\n  \"soundVolume\": 40,\n  \"musicVolume\": 25,\n  \"renderResolution\": 50,\n  \"autoSaveInterval\": 10,\n  \"autoSaveLimit\": 20,\n  \"uiScale\": 150\n}"));
    }

    [Test]
    public async Task LoadMissingTest()
    {
        using Sandbox sb = Sandbox.Create();

        GameSettingsStore store = new(sb.GetPath("config"));

        Assert.That(await store.TryLoadAsync(), Is.Null);
    }

    [Test]
    public void LoadUnreadableTest()
    {
        using Sandbox sb = Sandbox.Create();

        sb.WriteTextFile("settings.json", "not json");

        GameSettingsStore store = new(sb.RootPath);

        Assert.That(
            async () => await store.TryLoadAsync(),
            Throws.TypeOf<GameSettingsInvalidException>().With.Message.EqualTo(
                $"Unable to load the settings from '{sb.GetPath("settings.json")}': " +
                "'not json' is an invalid JSON literal. Expected the literal 'null'. " +
                "LineNumber: 0 | BytePositionInLine: 1."));
    }

    [Test]
    public void LoadEmptyTest()
    {
        using Sandbox sb = Sandbox.Create();

        sb.WriteTextFile("settings.json", string.Empty);

        GameSettingsStore store = new(sb.RootPath);

        Assert.That(
            async () => await store.TryLoadAsync(),
            Throws.TypeOf<GameSettingsInvalidException>().With.Message.EqualTo(
                $"Unable to load the settings from '{sb.GetPath("settings.json")}': " +
                "The input does not contain any JSON tokens. " +
                "Expected the input to start with a valid JSON token, when isFinalBlock is true. " +
                "LineNumber: 0 | BytePositionInLine: 0."));
    }

    [Test]
    public void LoadUnversionedTest()
    {
        using Sandbox sb = Sandbox.Create();

        sb.WriteTextFile("settings.json", "{\"soundVolume\": 40, \"musicVolume\": 25}");

        GameSettingsStore store = new(sb.RootPath);

        Assert.That(
            async () => await store.TryLoadAsync(),
            Throws.TypeOf<GameSettingsInvalidException>().With.Message.EqualTo(
                $"Unable to load the settings from '{sb.GetPath("settings.json")}': " +
                "The file does not start with a format version."));
    }

    [Test]
    public void LoadFutureFormatTest()
    {
        using Sandbox sb = Sandbox.Create();

        sb.WriteTextFile(
            "settings.json",
            "{\"formatVersion\": 7, \"soundVolume\": 40, \"musicVolume\": 25, \"windowed\": false, \"renderResolution\": 100, \"autoSaveInterval\": 5}");

        GameSettingsStore store = new(sb.RootPath);

        Assert.That(
            async () => await store.TryLoadAsync(),
            Throws.TypeOf<GameSettingsUnsupportedFormatException>().With.Message.EqualTo(
                $"Unable to load the settings from '{sb.GetPath("settings.json")}': " +
                "Unsupported settings format '7'."));
    }

    [Test]
    public void LoadPreviousFormatTest()
    {
        using Sandbox sb = Sandbox.Create();

        sb.WriteTextFile(
            "settings.json",
            "{\"formatVersion\": 5, \"soundVolume\": 40, \"musicVolume\": 25, \"windowed\": true, \"renderResolution\": 100}");

        GameSettingsStore store = new(sb.RootPath);

        Assert.That(
            async () => await store.TryLoadAsync(),
            Throws.TypeOf<GameSettingsUnsupportedFormatException>().With.Message.EqualTo(
                $"Unable to load the settings from '{sb.GetPath("settings.json")}': " +
                "Unsupported settings format '5'."));
    }

    [Test]
    public async Task LoadOutOfRangeTest()
    {
        using Sandbox sb = Sandbox.Create();

        sb.WriteTextFile(
            "settings.json",
            "{\"formatVersion\": 6, \"soundVolume\": 900, \"musicVolume\": -3, \"windowed\": true, \"renderResolution\": 0, \"autoSaveInterval\": -2}");

        GameSettingsStore store = new(sb.RootPath);

        Assert.That(await store.TryLoadAsync(), Is.EqualTo(GameSettings.Create(100, 0, true, 50, 0, 10, BattleSpeed.Normal, null, true, MovementSpeed.Normal)));
    }

    [Test]
    public void LoadLockedTest()
    {
        using Sandbox sb = Sandbox.Create();

        sb.WriteTextFile("settings.json", "{\"formatVersion\": 6, \"soundVolume\": 40, \"musicVolume\": 25, \"windowed\": false, \"renderResolution\": 100, \"autoSaveInterval\": 5}");

        GameSettingsStore store = new(sb.RootPath);

        using FileStream lockingStream = new(
            sb.GetPath("settings.json"),
            FileMode.Open,
            FileAccess.ReadWrite,
            FileShare.None);

        Assert.That(
            async () => await store.TryLoadAsync(),
            Throws.TypeOf<IOException>().With.Message.EqualTo(
                $"The process cannot access the file '{sb.GetPath("settings.json")}' " +
                "because it is being used by another process."));
    }

    [Test]
    public async Task LoadMissingPropertyTest()
    {
        using Sandbox sb = Sandbox.Create();

        sb.WriteTextFile("settings.json", "{\"formatVersion\": 6, \"soundVolume\": 40, \"musicVolume\": 25, \"windowed\": true, \"renderResolution\": 75}");

        GameSettingsStore store = new(sb.RootPath);

        Assert.That(await store.TryLoadAsync(), Is.EqualTo(GameSettings.Create(40, 25, true, 75, 5, 10, BattleSpeed.Normal, null, true, MovementSpeed.Normal)));
    }

    [Test]
    public async Task LoadUnknownPropertyTest()
    {
        using Sandbox sb = Sandbox.Create();

        sb.WriteTextFile(
            "settings.json",
            "{\"formatVersion\": 6, \"soundVolume\": 40, \"language\": \"de\", \"musicVolume\": 25, \"windowed\": true, \"renderResolution\": 75, \"autoSaveInterval\": 3, \"keys\": {\"endTurn\": \"Enter\"}}");

        GameSettingsStore store = new(sb.RootPath);

        Assert.That(await store.TryLoadAsync(), Is.EqualTo(GameSettings.Create(40, 25, true, 75, 3, 10, BattleSpeed.Normal, null, true, MovementSpeed.Normal)));
    }

    [Test]
    public void LoadMissingRequiredPropertyTest()
    {
        using Sandbox sb = Sandbox.Create();

        sb.WriteTextFile("settings.json", "{\"formatVersion\": 6, \"soundVolume\": 40, \"musicVolume\": 25, \"windowed\": true, \"autoSaveInterval\": 3}");

        GameSettingsStore store = new(sb.RootPath);

        Assert.That(
            async () => await store.TryLoadAsync(),
            Throws.TypeOf<GameSettingsInvalidException>().With.Message.EqualTo(
                $"Unable to load the settings from '{sb.GetPath("settings.json")}': " +
                "JSON deserialization for type 'Gallean.Configuration.GameSettings' was missing required properties including: 'renderResolution'."));
    }

    [Test]
    public async Task LoadBattleSpeedTest()
    {
        using Sandbox sb = Sandbox.Create();

        sb.WriteTextFile(
            "settings.json",
            "{\"formatVersion\": 6, \"soundVolume\": 40, \"musicVolume\": 25, \"windowed\": true, \"renderResolution\": 75, \"autoSaveInterval\": 3, \"battleSpeed\": \"Fast\"}");

        GameSettingsStore store = new(sb.RootPath);

        Assert.That(await store.TryLoadAsync(), Is.EqualTo(GameSettings.Create(40, 25, true, 75, 3, 10, BattleSpeed.Fast, null, true, MovementSpeed.Normal)));
    }

    [Test]
    public async Task LoadMovementSpeedTest()
    {
        using Sandbox sb = Sandbox.Create();

        sb.WriteTextFile(
            "settings.json",
            "{\"formatVersion\": 6, \"soundVolume\": 40, \"musicVolume\": 25, \"windowed\": true, \"renderResolution\": 75, \"autoSaveInterval\": 3, \"movementSpeed\": \"Fast\"}");

        GameSettingsStore store = new(sb.RootPath);

        Assert.That(await store.TryLoadAsync(), Is.EqualTo(GameSettings.Create(40, 25, true, 75, 3, 10, BattleSpeed.Normal, null, true, MovementSpeed.Fast)));
    }

    [Test]
    public async Task LoadUiScaleTest()
    {
        using Sandbox sb = Sandbox.Create();

        sb.WriteTextFile(
            "settings.json",
            "{\"formatVersion\": 6, \"soundVolume\": 40, \"musicVolume\": 25, \"windowed\": true, \"renderResolution\": 75, \"autoSaveInterval\": 3, \"uiScale\": 150}");

        GameSettingsStore store = new(sb.RootPath);

        Assert.That(await store.TryLoadAsync(), Is.EqualTo(GameSettings.Create(40, 25, true, 75, 3, 10, BattleSpeed.Normal, 150, true, MovementSpeed.Normal)));
    }
}
