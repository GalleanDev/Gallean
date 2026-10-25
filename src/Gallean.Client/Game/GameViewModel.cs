using System.ComponentModel;
using System.Diagnostics;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gallean.Common;
using Gallean.Configuration;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class GameViewModel : ObservableObject, IDisposable
{
    private readonly IControlledApplicationLifetime? lifetime;
    private readonly IAssetProvider assets;
    private readonly IAudioPlayer audioPlayer;
    private readonly GameLoader gameLoader;
    private readonly GameSaveStore gameSaveStore;
    private readonly GameSettingsStore gameSettingsStore;
    private readonly ClientVersion clientVersion;
    private readonly ILog log;
    private readonly IRandomProvider random;
    private readonly TaskScheduler scheduler;
    private readonly ScreenStack screens;
    private GameModel? activeGame;
    private WorldState? savedState;

    public GameViewModel(
        IControlledApplicationLifetime? lifetime,
        IAssetProvider assets,
        IAssetPreloader assetPreloader,
        IAudioPlayer audioPlayer,
        GameLoader gameLoader,
        GameSaveStore gameSaveStore,
        GameSettingsStore gameSettingsStore,
        GameSettings settings,
        ClientVersion clientVersion,
        ILog log,
        IRandomProvider random,
        TaskScheduler scheduler)
    {
        this.lifetime = lifetime;
        this.assets = assets;
        this.audioPlayer = audioPlayer;
        this.gameLoader = gameLoader;
        this.gameSaveStore = gameSaveStore;
        this.gameSettingsStore = gameSettingsStore;
        this.clientVersion = clientVersion;
        this.log = log;
        this.random = random;
        this.scheduler = scheduler;

        AssetPreloader = assetPreloader;
        Settings = settings;

        screens = new ScreenStack(audioPlayer);
        screens.Changed += OnScreensChanged;

        Alert = new AlertViewModel();
        DevTools = new DevToolsViewModel(this);
    }

    public IAssetPreloader AssetPreloader { get; }
    public IReadOnlyList<AbstractScreenViewModel> Screens => screens.Screens;
    public AbstractScreenViewModel? RootScreen => screens.Root;
    public AbstractScreenViewModel? TopScreen => screens.Top;
    public bool PaintsOwnBackground => screens.PaintsOwnBackground;

    public AlertViewModel Alert { get; }
    public DevToolsViewModel DevTools { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWindowed))]
    public partial GameSettings Settings { get; private set; }

    public bool IsWindowed
    {
        get => Settings.Windowed;
        set
        {
            if (value != Settings.Windowed)
            {
                UpdateSettings(GameSettings.Create(
                    Settings.SoundVolume,
                    Settings.MusicVolume,
                    value,
                    Settings.RenderResolution,
                    Settings.AutoSaveInterval,
                    Settings.AutoSaveLimit,
                    Settings.BattleSpeed,
                    Settings.UiScale,
                    Settings.LockCursor));
            }
        }
    }

    public void PlaySound(AssetSoundKey key)
    {
        audioPlayer.PlaySound(key);
    }

    public void ShowInfo(IconMoniker icon, string caption, string? details)
    {
        Alert.Show(new Alert(AlertType.Info, icon, caption, details));
    }

    public void ShowError(string caption)
    {
        audioPlayer.PlaySound(new AssetSoundKey.Beep());
        Alert.Show(new Alert(AlertType.Error, IconMoniker.Warning, caption, Details: null));
    }

    public void Dispose()
    {
        screens.Changed -= OnScreensChanged;
        screens.Dispose();
        StopGame();
    }

    public void ShowScreen(AbstractScreenViewModel screen)
    {
        screens.Reset(screen);
    }

    public void OpenScreen(AbstractScreenViewModel screen)
    {
        screens.Open(screen);
    }

    public void CloseScreen(AbstractScreenViewModel screen)
    {
        screens.Close(screen);
    }

    public void ShowDetails(object content)
    {
        HideDetails();
        OpenScreen(new DetailsViewModel(content));
    }

    public void HideDetails()
    {
        if (TopScreen is DetailsViewModel details)
        {
            CloseScreen(details);
        }
    }

    public async Task<BattleScreenViewModel?> ShowBattleAsync(WorldScreenViewModel worldScreen)
    {
        GameModel gameModel = worldScreen.GameModel;

        Debug.Assert(gameModel.ActiveBattle is not null);

        BattleScreenViewModel? screen = null;
        try
        {
            screen = new BattleScreenViewModel(gameModel, gameModel.ActiveBattle, this, assets, AssetPreloader, random);

            await screen.LoadAsync();

            if (ReferenceEquals(RootScreen, worldScreen))
            {
                OpenScreen(screen);

                return screen;
            }
            else
            {
                CloseScreen(screen);

                return null;
            }
        }
        catch (Exception ex)
        {
            log.Error("Failed to load battle.", ex);

            if (screen is not null)
            {
                CloseScreen(screen);
            }

            gameModel.DismissBattle();
            StopGame();
            ShowScreen(new ErrorScreenViewModel(Tr("Failed to load battle.\n{0}", ex.Message), Tr("Back"), ShowMainMenuAsync));

            return null;
        }
    }

    public async Task ExecuteAiTurnsAsync(WorldScreenViewModel worldScreen)
    {
        GameModel gameModel = worldScreen.GameModel;

        while (ReferenceEquals(RootScreen, worldScreen))
        {
            GameSnapshot snapshot = gameModel.Snapshot;

            if (snapshot.IsGameEnded || snapshot.IsViewPlayerTurn)
            {
                break;
            }

            await gameModel.ExecuteAiActionAsync();
            await WaitForBattleAsync(gameModel);
        }
    }

    private static async Task WaitForBattleAsync(GameModel gameModel)
    {
        TaskCompletionSource completionSource = new();

        gameModel.PropertyChanged += OnBattleDismissed;
        try
        {
            if (gameModel.Snapshot.BattleState is not null)
            {
                await completionSource.Task;
            }
        }
        finally
        {
            gameModel.PropertyChanged -= OnBattleDismissed;
        }

        return;

        void OnBattleDismissed(object? sender, PropertyChangedEventArgs e)
        {
            if (gameModel.Snapshot.BattleState is null)
            {
                completionSource.TrySetResult();
            }
        }
    }

    [RelayCommand]
    private async Task ShowMainMenuAsync()
    {
        IReadOnlyList<GameSaveEntry> saves;

        try
        {
            saves = await gameSaveStore.EnumerateAsync();
        }
        catch (Exception ex)
        {
            log.Error("Failed to list saved games.", ex);
            saves = [];
        }

        StopGame();
        ShowScreen(new MainMenuScreenViewModel(this, saves));
    }

    [RelayCommand]
    private void OpenSettings()
    {
        OpenScreen(new SettingsScreenViewModel(this));
    }

    [RelayCommand]
    private void ToggleWindowed()
    {
        IsWindowed = !IsWindowed;
    }

    public void UpdateSettings(GameSettings settings)
    {
        Settings = settings;

        audioPlayer.SoundVolume = settings.SoundVolumeRatio;
        audioPlayer.MusicVolume = settings.MusicVolumeRatio;
        gameSaveStore.AutoSaveLimit = settings.AutoSaveLimit;
        gameSettingsStore.Save(settings);
    }

    [RelayCommand]
    private async Task ShowCampaignSelectAsync()
    {
        (IReadOnlyList<GameCampaign> Campaigns, IGameTextProvider Texts, IGameDefinitionProvider Definitions)? data = await TryLoadCampaignsAsync();

        if (data is not null)
        {
            (IReadOnlyList<GameCampaign> campaigns, IGameTextProvider texts, IGameDefinitionProvider definitions) = data.Value;

            ShowScreen(new CampaignSelectScreenViewModel(this, AssetPreloader, texts, definitions, campaigns));
        }
    }

    [RelayCommand]
    private async Task ShowScenarioSelectAsync()
    {
        (IReadOnlyList<GameCampaign> Campaigns, IGameTextProvider Texts, IGameDefinitionProvider Definitions)? data = await TryLoadCampaignsAsync();

        if (data is not null)
        {
            (IReadOnlyList<GameCampaign> campaigns, IGameTextProvider texts, IGameDefinitionProvider definitions) = data.Value;

            ShowScreen(new ScenarioSelectScreenViewModel(this, AssetPreloader, texts, definitions, campaigns));
        }
    }

    [RelayCommand(CanExecute = nameof(CanStartGame))]
    private async Task StartGameAsync(GameConfiguration? configuration)
    {
        if (configuration is null)
        {
            return;
        }

        OpenScreen(new LoadingScreenViewModel(Tr("Loading quest…")));

        GameModel gameModel;

        try
        {
            (GameSession session, IGameTextProvider texts) = await RunAsync(() => StartScenario(configuration));

            gameModel = new GameModel(session, texts, log, scheduler);
        }
        catch (Exception ex)
        {
            log.Error("Failed to load scenario.", ex);
            ShowScreen(new ErrorScreenViewModel(Tr("Failed to load quest.\n{0}", ex.Message), Tr("Exit"), Exit));

            return;
        }

        WorldScreenViewModel worldScreen = StartGame(gameModel);
        (ScenarioTextId nameId, _, ScenarioTextId objectiveId, ScenarioTextId? briefingId, _, _, _) = gameModel.Scenario.Definition.InfoTexts;

        if (briefingId is not null)
        {
            BriefingScreenViewModel briefingViewModel = new(
                gameModel.Texts.GetScenarioText(nameId),
                gameModel.Texts.GetScenarioText(briefingId.Value),
                new ScenarioTextFormatter(gameModel.Scenario.Names),
                gameModel.Texts.GetScenarioText(objectiveId),
                MenuBackgroundViewModel.TryCreateRaceSelection(GetViewPlayerRace(gameModel)),
                TryGetBriefingVoice(gameModel),
                audioPlayer,
                () => BeginGameAsync(gameModel, worldScreen));

            ShowScreen(briefingViewModel);
        }
        else
        {
            await BeginGameAsync(gameModel, worldScreen);
        }
    }

    private (GameSession Session, IGameTextProvider Texts) StartScenario(GameConfiguration configuration)
    {
        if (configuration.Configuration is not null)
        {
            return gameLoader.CreateScenario(configuration.ScenarioId, configuration.Configuration);
        }
        else
        {
            return gameLoader.LoadScenario(configuration.ScenarioId, worldState: null);
        }
    }

    [RelayCommand]
    private async Task ContinueCampaignAsync()
    {
        if (activeGame is null)
        {
            return;
        }

        ScenarioId? nextScenarioId = activeGame.Scenario.Definition.NextScenarioId;

        if (nextScenarioId is null)
        {
            return;
        }

        if (LeaderExportScreenViewModel.CanExportLeader(activeGame))
        {
            OpenScreen(new LeaderExportScreenViewModel(activeGame, this));
        }
        else
        {
            await StartNextScenarioAsync(activeGame, nextScenarioId.Value, scenarioLeader: null);
        }
    }

    public async Task StartNextScenarioAsync(GameModel gameModel, ScenarioId scenarioId, ScenarioLeader? scenarioLeader)
    {
        GameSnapshot snapshot = gameModel.Snapshot;
        WorldPlayer player = snapshot.WorldState.Players[snapshot.ViewPlayerId];
        LordDefinition lord = gameModel.Definitions.GetLord(player.LordId);
        ScenarioConfiguration configuration = new(
            [new ScenarioPlayerConfiguration(player.Race, lord.Category, player.FaceVariant)],
            snapshot.WorldState.Difficulty,
            scenarioLeader);

        await StartGameCommand.ExecuteAsync(new GameConfiguration(scenarioId.Value, configuration));
    }

    private AssetSoundKey? TryGetBriefingVoice(GameModel gameModel)
    {
        AssetSoundKey key = new AssetSoundKey.BriefingVoice(new ScenarioId(gameModel.ScenarioId));

        if (assets.GetOptionalStream(key) is not null)
        {
            return key;
        }
        else
        {
            return null;
        }
    }

    private static WorldRace GetViewPlayerRace(GameModel gameModel)
    {
        GameSnapshot snapshot = gameModel.Snapshot;
        WorldPlayer player = snapshot.WorldState.Players[snapshot.ViewPlayerId];

        return player.Race;
    }

    private async Task BeginGameAsync(GameModel gameModel, WorldScreenViewModel worldScreen)
    {
        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());
        await PlayAsync(gameModel, worldScreen);
    }

    private async Task PlayAsync(GameModel gameModel, WorldScreenViewModel worldScreen)
    {
        if (!await TryShowWorldAsync(gameModel, worldScreen))
        {
            return;
        }

        await ExecuteAiTurnsAsync(worldScreen);
    }

    private static bool CanStartGame(GameConfiguration? configuration)
    {
        return configuration is not null;
    }

    [RelayCommand]
    private async Task ShowLoadGameAsync()
    {
        IReadOnlyList<GameSaveEntry>? saves = await TryEnumerateSavesAsync();

        if (saves is not null)
        {
            OpenScreen(new LoadGameScreenViewModel(this, saves));
        }
    }

    [RelayCommand(CanExecute = nameof(CanLoadGame))]
    private async Task LoadGameAsync(GameSaveEntry? entry)
    {
        if (entry is null)
        {
            return;
        }

        if (activeGame is not null && activeGame.Snapshot.WorldState != savedState)
        {
            OpenScreen(new ConfirmPromptViewModel(
                Tr("Load game"),
                TrSpans("Do you want to load \"{0}\"? Unsaved progress will be lost.", entry.Metadata.Value.Name),
                Tr("Load"),
                ConfirmPromptType.Primary,
                () => LoadSaveAsync(entry)));
        }
        else
        {
            await LoadSaveAsync(entry);
        }
    }

    private static bool CanLoadGame(GameSaveEntry? entry)
    {
        return entry is not null;
    }

    private async Task LoadSaveAsync(GameSaveEntry entry)
    {
        OpenScreen(new LoadingScreenViewModel(Tr("Loading saved game…")));

        GameModel gameModel;

        try
        {
            GameSave? save = await gameSaveStore.TryLoadAsync(entry.Id);

            if (save is null)
            {
                await ShowMainMenuAsync();
                OpenScreen(new ErrorScreenViewModel(Tr("The saved game is no longer available."), Tr("Close")));

                return;
            }

            (GameSession session, IGameTextProvider texts) = await RunAsync(() => gameLoader.LoadScenario(save.Metadata.ScenarioId, save.WorldState));

            gameModel = new GameModel(session, texts, log, scheduler);
        }
        catch (Exception ex)
        {
            log.Error("Failed to load saved game.", ex);
            ShowScreen(new ErrorScreenViewModel(Tr("Failed to load saved game.\n{0}", ex.Message), Tr("Back"), ShowMainMenuAsync));

            return;
        }

        WorldScreenViewModel worldScreen = StartGame(gameModel);

        await gameModel.World.DispatchCommandAsync(new WorldResumeGameCommand());

        await PlayAsync(gameModel, worldScreen);
    }

    [RelayCommand]
    private void Exit()
    {
        lifetime?.Shutdown();
    }

    [RelayCommand]
    private void Escape()
    {
        TopScreen?.Close();
    }

    [RelayCommand]
    private async Task ExitGameAsync()
    {
        if (activeGame is not null && activeGame.Snapshot.IsSaveAllowed)
        {
            if (await TryAutoSaveAsync(activeGame, Tr("Exit save"), GameSaveType.Exit, activeGame.Snapshot.WorldState))
            {
                await ShowMainMenuAsync();
            }
        }
        else
        {
            await ShowMainMenuAsync();
        }
    }

    public async Task ShowSaveGameAsync(GameModel gameModel, Action onSaved)
    {
        IReadOnlyList<GameSaveEntry>? saves = await TryEnumerateSavesAsync();

        if (saves is not null)
        {
            OpenScreen(new SaveGameScreenViewModel(gameModel, this, saves, onSaved));
        }
    }

    public async Task<GameSaveEntry?> SaveGameAsync(GameModel gameModel, GameSaveId? id, string name)
    {
        WorldState state = gameModel.Snapshot.WorldState;
        GameSaveMetadata metadata = new(
            name,
            GameSaveType.Manual,
            gameModel.ScenarioId,
            gameModel.ScenarioName,
            state.TurnInfo.TurnNumber,
            DateTimeOffset.Now,
            clientVersion.Serialize());

        try
        {
            GameSaveEntry entry = await gameSaveStore.SaveAsync(id, metadata, state);
            UpdateSavedState(gameModel, state);

            return entry;
        }
        catch (Exception ex)
        {
            log.Error("Failed to save game.", ex);
            OpenScreen(new ErrorScreenViewModel(Tr("Failed to save game.\n{0}", ex.Message), Tr("Close")));

            return null;
        }
    }

    public async Task WriteExitSaveAsync()
    {
        if (activeGame is not null && activeGame.Snapshot.IsSaveAllowed)
        {
            await TryAutoSaveAsync(activeGame, Tr("Exit save"), GameSaveType.Exit, activeGame.Snapshot.WorldState);
        }
    }

    public async Task<bool> TryAutoSaveAsync(GameModel gameModel, string name, GameSaveType type, WorldState state)
    {
        GameSaveMetadata metadata = new(
            name,
            type,
            gameModel.ScenarioId,
            gameModel.ScenarioName,
            state.TurnInfo.TurnNumber,
            DateTimeOffset.Now,
            clientVersion.Serialize());

        try
        {
            await gameSaveStore.SaveAsync(null, metadata, state);
            UpdateSavedState(gameModel, state);

            return true;
        }
        catch (Exception ex)
        {
            log.Error("Failed to autosave the game.", ex);
            OpenScreen(new ErrorScreenViewModel(Tr("Failed to autosave the game.\n{0}", ex.Message), Tr("Close")));

            return false;
        }
    }

    private void UpdateSavedState(GameModel gameModel, WorldState state)
    {
        if (ReferenceEquals(gameModel, activeGame))
        {
            savedState = state;
        }
    }

    public async Task<bool> TryDeleteSaveAsync(GameSaveId id)
    {
        try
        {
            await gameSaveStore.DeleteAsync(id);

            return true;
        }
        catch (Exception ex)
        {
            log.Error("Failed to delete the saved game.", ex);
            OpenScreen(new ErrorScreenViewModel(Tr("Failed to delete the saved game.\n{0}", ex.Message), Tr("Close")));

            return false;
        }
    }

    private async Task<IReadOnlyList<GameSaveEntry>?> TryEnumerateSavesAsync()
    {
        try
        {
            return await gameSaveStore.EnumerateAsync();
        }
        catch (Exception ex)
        {
            log.Error("Failed to list saved games.", ex);
            OpenScreen(new ErrorScreenViewModel(Tr("Failed to list saved games.\n{0}", ex.Message), Tr("Close")));

            return null;
        }
    }

    private async Task<(IReadOnlyList<GameCampaign> Campaigns, IGameTextProvider Texts, IGameDefinitionProvider Definitions)?> TryLoadCampaignsAsync()
    {
        OpenScreen(new LoadingScreenViewModel(Tr("Loading data…")));

        try
        {
            return await RunAsync(gameLoader.LoadCampaigns);
        }
        catch (Exception ex)
        {
            log.Error("Failed to load scenarios.", ex);
            ShowScreen(new ErrorScreenViewModel(Tr("Failed to load quests.\n{0}", ex.Message), Tr("Exit"), Exit));

            return null;
        }
    }

    private async Task<bool> TryShowWorldAsync(GameModel gameModel, WorldScreenViewModel worldScreen)
    {
        try
        {
            AssetPreloadPlan worldPlan = await RunAsync(() =>
            {
                GameSnapshot snapshot = gameModel.Snapshot;
                WorldRenderRequest renderRequest = worldScreen.RenderRequest;
                WorldRenderComposer renderComposer = new(gameModel.Definitions, gameModel.Scenario, assets);
                WorldRenderState renderState = renderComposer.Compose(renderRequest);
                WorldPlayer viewPlayer = snapshot.WorldState.Players[snapshot.ViewPlayerId];
                CapitalSceneViewModel? capitalScene = CapitalSceneViewModel.Create(viewPlayer.Race, viewPlayer.Buildings.Acquired, constructedBuildingId: null);

                return WorldAssetPreloadPlan.BuildWorld(renderState, capitalScene);
            });

            using IDisposable leases = await AssetPreloader.PreloadAsync([.. worldPlan.InitialRequired, .. worldPlan.InitialOptional]);
        }
        catch (Exception ex)
        {
            log.Error("Failed to load world.", ex);

            CloseScreen(worldScreen);
            StopGame();
            ShowScreen(new ErrorScreenViewModel(Tr("Failed to load world.\n{0}", ex.Message), Tr("Back"), ShowMainMenuAsync));

            return false;
        }

        ShowScreen(worldScreen);

        return true;
    }

    private WorldScreenViewModel StartGame(GameModel gameModel)
    {
        StopGame();
        activeGame = gameModel;

        return new WorldScreenViewModel(gameModel, this, random);
    }

    private void StopGame()
    {
        GameModel? gameModel = activeGame;

        if (gameModel is null)
        {
            return;
        }

        activeGame = null;
        savedState = null;
        gameModel.Dispose();
    }

    private void OnScreensChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(RootScreen));
        OnPropertyChanged(nameof(TopScreen));
        OnPropertyChanged(nameof(PaintsOwnBackground));
    }

    private Task<TResult> RunAsync<TResult>(Func<TResult> work)
    {
        return Task.Factory.StartNew(work, CancellationToken.None, TaskCreationOptions.DenyChildAttach, scheduler);
    }
}
