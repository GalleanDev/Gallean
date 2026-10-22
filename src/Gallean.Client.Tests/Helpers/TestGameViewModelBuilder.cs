using Gallean.Common;
using Gallean.Configuration;

namespace Gallean.Client.Tests;

internal sealed class TestGameViewModelBuilder
{
    private IAssetProvider assets = NullAssetProvider.Instance;
    private IAssetPreloader assetPreloader = NullAssetPreloader.Instance;
    private IAudioPlayer audioPlayer = new NullAudioPlayer();
    private GameLoader gameLoader = new(new RandomProvider(0));
    private GameSaveStore saveStore = new(Path.Combine(TestContext.CurrentContext.WorkDirectory, "saves"), GameSettings.DefaultAutoSaveLimit);
    private GameSettingsStore settingsStore = new(Path.Combine(TestContext.CurrentContext.WorkDirectory, "settings"));
    private GameSettings settings = GameSettings.Default;
    private ClientVersion clientVersion = new("0.1.0", "2d40300");
    private IRandomProvider random = new RandomProvider(0);

    public TestGameViewModelBuilder WithAssets(IAssetProvider newAssets)
    {
        assets = newAssets;

        return this;
    }

    public TestGameViewModelBuilder WithAssetPreloader(IAssetPreloader newAssetPreloader)
    {
        assetPreloader = newAssetPreloader;

        return this;
    }

    public TestGameViewModelBuilder WithAudioPlayer(IAudioPlayer newAudioPlayer)
    {
        audioPlayer = newAudioPlayer;

        return this;
    }

    public TestGameViewModelBuilder WithGameLoader(GameLoader newGameLoader)
    {
        gameLoader = newGameLoader;

        return this;
    }

    public TestGameViewModelBuilder WithSaveStore(GameSaveStore newSaveStore)
    {
        saveStore = newSaveStore;

        return this;
    }

    public TestGameViewModelBuilder WithSettingsStore(GameSettingsStore newSettingsStore)
    {
        settingsStore = newSettingsStore;

        return this;
    }

    public TestGameViewModelBuilder WithSettings(GameSettings newSettings)
    {
        settings = newSettings;

        return this;
    }

    public TestGameViewModelBuilder WithClientVersion(ClientVersion newClientVersion)
    {
        clientVersion = newClientVersion;

        return this;
    }

    public TestGameViewModelBuilder WithRandom(IRandomProvider newRandom)
    {
        random = newRandom;

        return this;
    }

    public GameViewModel Build()
    {
        return new GameViewModel(
            lifetime: null,
            assets,
            assetPreloader,
            audioPlayer,
            gameLoader,
            saveStore,
            settingsStore,
            settings,
            clientVersion,
            NullLog.Instance,
            random,
            SynchronousTaskScheduler.Instance);
    }
}
