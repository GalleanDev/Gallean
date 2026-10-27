using Gallean.Configuration;
using Gallean.Tests.Common;

namespace Gallean.Client.Tests;

public sealed class SettingsScreenViewModelTests
{
    private sealed class TestAudioPlayer : IAudioPlayer
    {
        public float SoundVolume { get; set; }
        public float MusicVolume { get; set; }

        public void PlaySound(AssetSoundKey key)
        {
        }

        public IMusicPlayback? PlayMusic(MusicQueue queue, MusicPosition position)
        {
            return null;
        }

        public IAudioPlayback? PlayVoice(AssetSoundKey key)
        {
            return null;
        }

        public IAmbientAudioPlayback? PlayAmbientSound(AssetSoundKey key, float volume, float pan)
        {
            return null;
        }
    }

    private static async Task<GameViewModel> CreateGameAsync(TestAudioPlayer audioPlayer, Sandbox sb)
    {
        GameSettingsStore settingsStore = new(sb.GetPath("config"));

        return new TestGameViewModelBuilder()
            .WithAudioPlayer(audioPlayer)
            .WithSaveStore(new GameSaveStore(sb.GetPath("saves"), 10))
            .WithSettingsStore(settingsStore)
            .WithSettings(await settingsStore.TryLoadAsync() ?? GameSettings.Default)
            .Build();
    }

    [Test]
    public async Task DefaultSettingsTest()
    {
        using Sandbox sb = Sandbox.Create();

        using GameViewModel gameViewModel = await CreateGameAsync(new TestAudioPlayer(), sb);

        Assert.That(gameViewModel.Settings, Is.EqualTo(GameSettings.Create(100, 100, false, 100, 5, 10, BattleSpeed.Normal, UiSize.Normal, true, MovementSpeed.Normal)));
    }

    [Test]
    public async Task ChangeVolumeTest()
    {
        using Sandbox sb = Sandbox.Create();

        TestAudioPlayer audioPlayer = new();

        using GameViewModel gameViewModel = await CreateGameAsync(audioPlayer, sb);

        gameViewModel.OpenSettingsCommand.Execute(null);

        SettingsScreenViewModel screen = Assert.IsOfType<SettingsScreenViewModel>(gameViewModel.TopScreen);
        screen.SoundVolume = 25;
        screen.MusicVolume = 60;

        Assert.That(gameViewModel.Settings, Is.EqualTo(GameSettings.Create(25, 60, false, 100, 5, 10, BattleSpeed.Normal, UiSize.Normal, true, MovementSpeed.Normal)));
        Assert.That(audioPlayer.SoundVolume, Is.EqualTo(0.25f));
        Assert.That(audioPlayer.MusicVolume, Is.EqualTo(0.6f));
        Assert.That(
            await new GameSettingsStore(sb.GetPath("config")).TryLoadAsync(),
            Is.EqualTo(GameSettings.Create(25, 60, false, 100, 5, 10, BattleSpeed.Normal, UiSize.Normal, true, MovementSpeed.Normal)));
    }

    [Test]
    public async Task ToggleWindowedTest()
    {
        using Sandbox sb = Sandbox.Create();

        using GameViewModel gameViewModel = await CreateGameAsync(new TestAudioPlayer(), sb);

        gameViewModel.OpenSettingsCommand.Execute(null);

        SettingsScreenViewModel screen = Assert.IsOfType<SettingsScreenViewModel>(gameViewModel.TopScreen);
        screen.SoundVolume = 25;

        gameViewModel.ToggleWindowedCommand.Execute(null);
        screen.MusicVolume = 60;

        Assert.That(gameViewModel.IsWindowed, Is.True);
        Assert.That(screen.Windowed, Is.True);
        Assert.That(gameViewModel.Settings, Is.EqualTo(GameSettings.Create(25, 60, true, 100, 5, 10, BattleSpeed.Normal, UiSize.Normal, true, MovementSpeed.Normal)));
        Assert.That(
            await new GameSettingsStore(sb.GetPath("config")).TryLoadAsync(),
            Is.EqualTo(GameSettings.Create(25, 60, true, 100, 5, 10, BattleSpeed.Normal, UiSize.Normal, true, MovementSpeed.Normal)));
    }

    [Test]
    public async Task ChangeRenderResolutionTest()
    {
        using Sandbox sb = Sandbox.Create();

        using GameViewModel gameViewModel = await CreateGameAsync(new TestAudioPlayer(), sb);

        gameViewModel.OpenSettingsCommand.Execute(null);

        SettingsScreenViewModel screen = Assert.IsOfType<SettingsScreenViewModel>(gameViewModel.TopScreen);
        screen.RenderResolution = 50;

        gameViewModel.ToggleWindowedCommand.Execute(null);

        Assert.That(gameViewModel.Settings, Is.EqualTo(GameSettings.Create(100, 100, true, 50, 5, 10, BattleSpeed.Normal, UiSize.Normal, true, MovementSpeed.Normal)));
        Assert.That(
            await new GameSettingsStore(sb.GetPath("config")).TryLoadAsync(),
            Is.EqualTo(GameSettings.Create(100, 100, true, 50, 5, 10, BattleSpeed.Normal, UiSize.Normal, true, MovementSpeed.Normal)));
    }

    [Test]
    public async Task ReopenTest()
    {
        using Sandbox sb = Sandbox.Create();

        new GameSettingsStore(sb.GetPath("config")).Save(GameSettings.Create(15, 45, false, 60, 10, 10, BattleSpeed.Normal, UiSize.Normal, true, MovementSpeed.Normal));

        using GameViewModel gameViewModel = await CreateGameAsync(new TestAudioPlayer(), sb);

        gameViewModel.OpenSettingsCommand.Execute(null);

        SettingsScreenViewModel screen = Assert.IsOfType<SettingsScreenViewModel>(gameViewModel.TopScreen);

        Assert.That(gameViewModel.Settings, Is.EqualTo(GameSettings.Create(15, 45, false, 60, 10, 10, BattleSpeed.Normal, UiSize.Normal, true, MovementSpeed.Normal)));
        Assert.That(screen.SoundVolume, Is.EqualTo(15));
        Assert.That(screen.MusicVolume, Is.EqualTo(45));
        Assert.That(screen.RenderResolution, Is.EqualTo(60));
        Assert.That(screen.AutoSaveInterval, Is.EqualTo(10));
    }

    [Test]
    public async Task ChangeAutoSaveIntervalTest()
    {
        using Sandbox sb = Sandbox.Create();

        using GameViewModel gameViewModel = await CreateGameAsync(new TestAudioPlayer(), sb);

        gameViewModel.OpenSettingsCommand.Execute(null);

        SettingsScreenViewModel screen = Assert.IsOfType<SettingsScreenViewModel>(gameViewModel.TopScreen);
        screen.AutoSaveInterval = 0;

        gameViewModel.ToggleWindowedCommand.Execute(null);

        Assert.That(screen.AutoSaveIntervals, Is.EqualTo([1, 3, 5, 10, 0]));
        Assert.That(gameViewModel.Settings, Is.EqualTo(GameSettings.Create(100, 100, true, 100, 0, 10, BattleSpeed.Normal, UiSize.Normal, true, MovementSpeed.Normal)));
        Assert.That(
            await new GameSettingsStore(sb.GetPath("config")).TryLoadAsync(),
            Is.EqualTo(GameSettings.Create(100, 100, true, 100, 0, 10, BattleSpeed.Normal, UiSize.Normal, true, MovementSpeed.Normal)));
    }

    [Test]
    public async Task ChangeLockCursorTest()
    {
        using Sandbox sb = Sandbox.Create();

        using GameViewModel gameViewModel = await CreateGameAsync(new TestAudioPlayer(), sb);

        gameViewModel.OpenSettingsCommand.Execute(null);

        SettingsScreenViewModel screen = Assert.IsOfType<SettingsScreenViewModel>(gameViewModel.TopScreen);
        screen.LockCursor = false;

        gameViewModel.ToggleWindowedCommand.Execute(null);

        Assert.That(gameViewModel.Settings, Is.EqualTo(GameSettings.Create(100, 100, true, 100, 5, 10, BattleSpeed.Normal, UiSize.Normal, false, MovementSpeed.Normal)));
        Assert.That(
            await new GameSettingsStore(sb.GetPath("config")).TryLoadAsync(),
            Is.EqualTo(GameSettings.Create(100, 100, true, 100, 5, 10, BattleSpeed.Normal, UiSize.Normal, false, MovementSpeed.Normal)));
    }

    [Test]
    public async Task ChangeAutoSaveLimitTest()
    {
        using Sandbox sb = Sandbox.Create();

        using GameViewModel gameViewModel = await CreateGameAsync(new TestAudioPlayer(), sb);

        gameViewModel.OpenSettingsCommand.Execute(null);

        SettingsScreenViewModel screen = Assert.IsOfType<SettingsScreenViewModel>(gameViewModel.TopScreen);
        screen.AutoSaveLimit = 3;

        gameViewModel.ToggleWindowedCommand.Execute(null);

        Assert.That(screen.AutoSaveLimits, Is.EqualTo([1, 3, 5, 10, 20]));
        Assert.That(gameViewModel.Settings, Is.EqualTo(GameSettings.Create(100, 100, true, 100, 5, 3, BattleSpeed.Normal, UiSize.Normal, true, MovementSpeed.Normal)));
        Assert.That(
            await new GameSettingsStore(sb.GetPath("config")).TryLoadAsync(),
            Is.EqualTo(GameSettings.Create(100, 100, true, 100, 5, 3, BattleSpeed.Normal, UiSize.Normal, true, MovementSpeed.Normal)));
    }

    [Test]
    public async Task ChangeBattleSpeedTest()
    {
        using Sandbox sb = Sandbox.Create();

        using GameViewModel gameViewModel = await CreateGameAsync(new TestAudioPlayer(), sb);

        gameViewModel.OpenSettingsCommand.Execute(null);

        SettingsScreenViewModel screen = Assert.IsOfType<SettingsScreenViewModel>(gameViewModel.TopScreen);
        screen.BattleSpeed = BattleSpeed.Fast;

        gameViewModel.ToggleWindowedCommand.Execute(null);

        Assert.That(screen.BattleSpeeds, Is.EqualTo([BattleSpeed.Normal, BattleSpeed.Fast]));
        Assert.That(gameViewModel.Settings, Is.EqualTo(GameSettings.Create(100, 100, true, 100, 5, 10, BattleSpeed.Fast, UiSize.Normal, true, MovementSpeed.Normal)));
        Assert.That(
            await new GameSettingsStore(sb.GetPath("config")).TryLoadAsync(),
            Is.EqualTo(GameSettings.Create(100, 100, true, 100, 5, 10, BattleSpeed.Fast, UiSize.Normal, true, MovementSpeed.Normal)));
    }

    [Test]
    public async Task ChangeMovementSpeedTest()
    {
        using Sandbox sb = Sandbox.Create();

        using GameViewModel gameViewModel = await CreateGameAsync(new TestAudioPlayer(), sb);

        gameViewModel.OpenSettingsCommand.Execute(null);

        SettingsScreenViewModel screen = Assert.IsOfType<SettingsScreenViewModel>(gameViewModel.TopScreen);
        screen.MovementSpeed = MovementSpeed.Fast;

        gameViewModel.ToggleWindowedCommand.Execute(null);

        Assert.That(screen.MovementSpeeds, Is.EqualTo([MovementSpeed.Normal, MovementSpeed.Fast]));
        Assert.That(gameViewModel.Settings, Is.EqualTo(GameSettings.Create(100, 100, true, 100, 5, 10, BattleSpeed.Normal, UiSize.Normal, true, MovementSpeed.Fast)));
        Assert.That(
            await new GameSettingsStore(sb.GetPath("config")).TryLoadAsync(),
            Is.EqualTo(GameSettings.Create(100, 100, true, 100, 5, 10, BattleSpeed.Normal, UiSize.Normal, true, MovementSpeed.Fast)));
    }

    [Test]
    public async Task ChangeUiSizeTest()
    {
        using Sandbox sb = Sandbox.Create();

        using GameViewModel gameViewModel = await CreateGameAsync(new TestAudioPlayer(), sb);

        gameViewModel.OpenSettingsCommand.Execute(null);

        SettingsScreenViewModel screen = Assert.IsOfType<SettingsScreenViewModel>(gameViewModel.TopScreen);
        screen.UiSize = UiSize.Large;

        gameViewModel.ToggleWindowedCommand.Execute(null);

        Assert.That(screen.UiSizes, Is.EqualTo([UiSize.Small, UiSize.Normal, UiSize.Large]));
        Assert.That(gameViewModel.Settings, Is.EqualTo(GameSettings.Create(100, 100, true, 100, 5, 10, BattleSpeed.Normal, UiSize.Large, true, MovementSpeed.Normal)));
        Assert.That(
            await new GameSettingsStore(sb.GetPath("config")).TryLoadAsync(),
            Is.EqualTo(GameSettings.Create(100, 100, true, 100, 5, 10, BattleSpeed.Normal, UiSize.Large, true, MovementSpeed.Normal)));
    }

    [Test]
    public async Task CloseTest()
    {
        using Sandbox sb = Sandbox.Create();

        using GameViewModel gameViewModel = await CreateGameAsync(new TestAudioPlayer(), sb);

        await gameViewModel.ShowMainMenuCommand.ExecuteAsync(null);
        gameViewModel.OpenSettingsCommand.Execute(null);

        SettingsScreenViewModel screen = Assert.IsOfType<SettingsScreenViewModel>(gameViewModel.TopScreen);

        screen.CloseCommand.Execute(null);

        Assert.That(gameViewModel.Screens, Is.EqualTo([gameViewModel.RootScreen]));
        Assert.That(gameViewModel.RootScreen, Is.InstanceOf<MainMenuScreenViewModel>());
    }
}
