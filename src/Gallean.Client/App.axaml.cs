using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Gallean.Common;
using Gallean.Configuration;

namespace Gallean.Client;

internal sealed class App(ILog log) : Application
{
    private readonly CleanupList cleanupList = new();
    private GameViewModel? currentGame;
    private Exception? currentGameException;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        ThemeAccentResources.Register(Resources, Theme.Accent.Default);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Dispatcher.UIThread.UnhandledException += (_, ev) => OnUnhandledException(desktop, ev);
            desktop.Exit += (_, _) => cleanupList.Dispose();
            _ = TryStartGameAsync(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task TryStartGameAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        try
        {
            await StartGameAsync(desktop);
        }
        catch (Exception ex)
        {
            log.Error("Failed to start the game.", ex);
            cleanupList.Dispose();
            HandleStartupException(desktop, ex);
        }
    }

    private async Task StartGameAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        GameSettingsStore gameSettingsStore = new(AppDirectories.Root);
        GameSettings gameSettings = await LoadSettingsAsync(gameSettingsStore);

        Task<AudioOutput> audioOutputTask = Task.Run(() => AudioOutput.CreateAsync(log));
        Task<AssetProvider> assetProviderTask = Task.Run(AssetProvider.LoadDefault);
        AudioOutput audioOutput = cleanupList.Add(await audioOutputTask);
        AssetProvider assetProvider = cleanupList.Add(await assetProviderTask);

        CheckAssetPack(assetProvider);

        ClientVersion clientVersion = ClientVersion.Get();
        FreeThreadedRandomProvider random = new();
        GameLoader gameLoader = new(random);
        GameSaveStore gameSaveStore = new(AppDirectories.Saves, gameSettings.AutoSaveLimit);
        AssetPreloader assetPreloader = new(assetProvider);
        AudioPlayer audioPlayer = new(assetProvider, audioOutput, log, gameSettings.SoundVolumeRatio, gameSettings.MusicVolumeRatio);
        GameViewModel gameViewModel = cleanupList.Add(new GameViewModel(
            desktop,
            assetProvider,
            assetPreloader,
            audioPlayer,
            gameLoader,
            gameSaveStore,
            gameSettingsStore,
            gameSettings,
            clientVersion,
            log,
            random,
            TaskScheduler.Default));

        GameView gameView = new()
        {
            WindowState = gameSettings.Windowed ? WindowState.Normal : WindowState.FullScreen,
            DataContext = gameViewModel
        };

        AssetScope.SetAssets(gameView, assetProvider);
        AudioScope.SetAudio(gameView, audioPlayer);
        desktop.MainWindow = gameView;
        gameView.Show();

        currentGame = gameViewModel;
        gameViewModel.ShowMainMenuCommand.Execute(null);
    }

    private static void CheckAssetPack(IAssetMetadataProvider assets)
    {
        AssetSpriteMetadata metadata = assets.GetRequiredSpriteMetadata(new AssetSpriteKey.MainMenuBackground());

        if (metadata.PixelScale == 1)
        {
            throw new InstalledAssetPackNotEnhancedException();
        }
    }

    private async Task<GameSettings> LoadSettingsAsync(GameSettingsStore gameSettingsStore)
    {
        try
        {
            return await gameSettingsStore.TryLoadAsync() ?? GameSettings.Default;
        }
        catch (Exception ex) when (ex is GameSettingsInvalidException or GameSettingsUnsupportedFormatException)
        {
            log.Error("Failed to load the settings.", ex);

            return GameSettings.Default;
        }
    }

    private void OnUnhandledException(IClassicDesktopStyleApplicationLifetime desktop, DispatcherUnhandledExceptionEventArgs e)
    {
        e.Handled = true;

        log.Error("Unhandled exception.", e.Exception);

        if (currentGameException is null)
        {
            currentGameException = e.Exception;

            try
            {
                if (currentGame is not null)
                {
                    ErrorScreenViewModel errorScreen = new(
                        Tr("An unexpected error occurred.\n{0}", e.Exception.Message),
                        Tr("Exit"),
                        () => desktop.Shutdown(),
                        log.Location);

                    currentGame.OpenScreen(errorScreen);
                }
                else
                {
                    FatalErrorWindow errorWindow = new(Tr("An unexpected error occurred"), e.Exception.Message, log.Location);

                    SetMainWindow(desktop, errorWindow);
                }
            }
            catch (Exception ex)
            {
                log.Error("Failed to show error details.", ex);

                desktop.Shutdown();
            }
        }
    }

    private void HandleStartupException(IClassicDesktopStyleApplicationLifetime desktop, Exception ex)
    {
        Window errorWindow;

        if (ex is InstalledAssetPackException assetPackException)
        {
            AssetPackErrorWindow window = new();
            string gameDirectory = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);
            AssetPackErrorViewModel errorViewModel = new(assetPackException, gameDirectory, desktop);
            window.DataContext = errorViewModel;

            errorWindow = window;
        }
        else
        {
            errorWindow = new FatalErrorWindow(Tr("Failed to start the game"), ex.Message, log.Location);
        }

        SetMainWindow(desktop, errorWindow);
    }

    private static void SetMainWindow(IClassicDesktopStyleApplicationLifetime desktop, Window window)
    {
        Window? previousWindow = desktop.MainWindow;
        desktop.MainWindow = window;
        window.Show();
        previousWindow?.Close();
    }
}
