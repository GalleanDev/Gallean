using Gallean.Configuration;

namespace Gallean.Client;

internal sealed class MainMenuScreenViewModel : AbstractScreenViewModel
{
    private readonly MusicQueue musicQueue = MusicQueue.Create([new AssetSoundKey.MenuMusic()], isLooping: true);

    public MainMenuScreenViewModel(GameViewModel gameViewModel, IReadOnlyList<GameSaveEntry> saves)
    {
        GameSaveListViewModel saveList = new(saves);

        Game = gameViewModel;
        LatestSave = saveList.Saves.FirstOrDefault();
        TutorialConfiguration = new GameConfiguration(GameLoader.TutorialScenarioId, Configuration: null);
        Version = ClientVersionFormatter.Format(ClientVersion.Get());
        SupportDeveloperUri = new Uri("https://gallean.org/go/2");
        Background = MainMenuBackgroundViewModel.Create();
    }

    public GameViewModel Game { get; }
    public GameSaveViewModel? LatestSave { get; }
    public GameConfiguration TutorialConfiguration { get; }
    public string Version { get; }
    public Uri SupportDeveloperUri { get; }
    public MainMenuBackgroundViewModel Background { get; }

    public override ScreenMusic Music => ScreenMusic.Tracks(musicQueue);
}
