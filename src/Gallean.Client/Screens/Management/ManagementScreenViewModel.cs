using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class ManagementScreenViewModel : AbstractGameScreenViewModel
{
    public ManagementScreenViewModel(
        GameModel gameModel,
        GameViewModel gameViewModel,
        ScreenMusic music,
        AssetSoundKey? openSound,
        Func<ManagementScreenViewModel, IReadOnlyList<ManagementTabViewModel>> createTabs)
        : base(gameModel, gameViewModel)
    {
        Music = music;
        OpenSound = openSound;
        Tabs = createTabs(this);
        SelectedTab = Tabs[0];
        SelectedTab.IsSelected = true;
        PlayerResources = new ResourceSetViewModel(WorldState.Players[ViewPlayerId].Resources, SelectedTab.ResourceProjection);
    }

    public IReadOnlyList<ManagementTabViewModel> Tabs { get; }

    public override bool BlurBackdrop => true;

    public override ScreenMusic Music { get; }
    public override AssetSoundKey? OpenSound { get; }
    public override AssetSoundKey? CloseSound => SelectedTab.CloseSound;

    [ObservableProperty]
    public partial ManagementTabViewModel SelectedTab { get; private set; }

    [ObservableProperty]
    public partial ResourceSetViewModel PlayerResources { get; private set; }

    public static ManagementScreenViewModel CreateForFortification(GameModel gameModel, GameViewModel gameViewModel, WorldFortificationId fortificationId)
    {
        ScreenMusic music = ResolveFortificationMusic(gameModel, fortificationId);
        AssetSoundKey? openSound = ResolveFortificationOpenSound(gameModel, fortificationId);

        return new ManagementScreenViewModel(gameModel, gameViewModel, music, openSound, screen =>
        {
            WorldState world = gameModel.Snapshot.WorldState;

            List<ManagementTabViewModel> tabs =
            [
                new CityTabViewModel(gameModel, gameViewModel, screen, fortificationId),
                new VisitingStackTabViewModel(gameModel, gameViewModel, screen, fortificationId)
            ];

            WorldFortification? fortification = world.Fortifications.GetValueOrDefault(fortificationId);

            if (fortification is WorldCapital)
            {
                tabs.Add(new BuildingTabViewModel(gameModel, gameViewModel, screen));
                tabs.Add(new SpellbookTabViewModel(gameModel, gameViewModel, screen));
                tabs.Add(new DiplomacyTabViewModel(gameModel, gameViewModel));
            }

            return tabs;
        });
    }

    public static ManagementScreenViewModel CreateForStack(GameModel gameModel, GameViewModel gameViewModel, WorldStackId stackId)
    {
        WorldState world = gameModel.Snapshot.WorldState;
        WorldPlan plan = WorldPlan.For(world);
        WorldStack stack = world.Stacks[stackId];
        WorldFortification? fortification = plan.GetFortificationAt(stack.Position);
        ManagementScreenViewModel screen;

        if (fortification is not null && fortification.Owner == stack.OwnerId)
        {
            screen = CreateForFortification(gameModel, gameViewModel, fortification.Id);
        }
        else
        {
            screen = CreateAtStack(gameModel, gameViewModel, stackId);
        }

        StackTabViewModel stackTab = screen.Tabs.OfType<StackTabViewModel>().First();

        screen.ChangeSelectedTab(stackTab);

        return screen;
    }

    public static ManagementScreenViewModel CreateForSite(GameModel gameModel, GameViewModel gameViewModel, WorldStackId stackId)
    {
        return CreateAtStack(gameModel, gameViewModel, stackId);
    }

    public static ManagementScreenViewModel CreateForExchange(GameModel gameModel, GameViewModel gameViewModel, WorldStackId stackId)
    {
        ManagementScreenViewModel screen = CreateAtStack(gameModel, gameViewModel, stackId);
        ExchangeTabViewModel exchange = screen.Tabs.OfType<ExchangeTabViewModel>().First();

        screen.ChangeSelectedTab(exchange);

        return screen;
    }

    private static ManagementScreenViewModel CreateAtStack(GameModel gameModel, GameViewModel gameViewModel, WorldStackId stackId)
    {
        return new ManagementScreenViewModel(gameModel, gameViewModel, ScreenMusic.Keep, null, screen =>
        {
            List<ManagementTabViewModel> tabs = [];

            WorldState world = gameModel.Snapshot.WorldState;
            WorldStack stack = world.Stacks[stackId];
            WorldPlan plan = WorldPlan.For(world);
            WorldSite? site = plan.GetSiteAt(stack.Position);

            if (site is not null)
            {
                tabs.Add(site.Accept(new CreateSiteTabVisitor(gameModel, gameViewModel, screen), stackId));
            }

            tabs.Add(new MapStackTabViewModel(gameModel, gameViewModel, screen, stackId));

            foreach (WorldStack other in world.Stacks.Values)
            {
                if (other.Id != stackId && other.OwnerId == stack.OwnerId && stack.Position.IsAdjacentTo(other.Position))
                {
                    tabs.Add(new ExchangeTabViewModel(gameModel, gameViewModel, screen, stackId, other.Id));
                }
            }

            return tabs;
        });
    }

    public static ManagementScreenViewModel CreateForSpellbook(GameModel gameModel, GameViewModel gameViewModel)
    {
        return new ManagementScreenViewModel(
            gameModel,
            gameViewModel,
            ScreenMusic.Keep,
            new AssetSoundKey.BookOpen(),
            screen => [new SpellbookTabViewModel(gameModel, gameViewModel, screen)]);
    }

    private static ScreenMusic ResolveFortificationMusic(GameModel gameModel, WorldFortificationId fortificationId)
    {
        WorldState world = gameModel.Snapshot.WorldState;

        if (world.Fortifications.GetValueOrDefault(fortificationId) is WorldCapital capital)
        {
            WorldRace race = world.Players[capital.OwnerId].Race;
            List<AssetSoundKey> tracks = [];

            for (int variant = 0; variant < AssetSoundKey.CapitalMusic.VariantCount; variant++)
            {
                tracks.Add(new AssetSoundKey.CapitalMusic(race, variant));
            }

            return ScreenMusic.Tracks(MusicQueue.Create(tracks, isLooping: true));
        }
        else
        {
            return ScreenMusic.Keep;
        }
    }

    private static AssetSoundKey? ResolveFortificationOpenSound(GameModel gameModel, WorldFortificationId fortificationId)
    {
        WorldFortification? fortification = gameModel.Snapshot.WorldState.Fortifications.GetValueOrDefault(fortificationId);

        if (fortification is WorldCapital)
        {
            return new AssetSoundKey.InterfaceOpen();
        }
        else
        {
            return null;
        }
    }

    protected override bool TryClose()
    {
        if (!SelectedTab.TryCancel())
        {
            CloseSelf();
        }

        return true;
    }

    protected override void OnSnapshotChanged(GameSnapshot current)
    {
        PlayerResources = PlayerResources.Update(current.WorldState.Players[current.ViewPlayerId].Resources);
    }

    protected override void OnDispose()
    {
        foreach (ManagementTabViewModel tab in Tabs)
        {
            tab.Dispose();
        }
    }

    [RelayCommand]
    private void SelectTab(ManagementTabViewModel tab)
    {
        if (ReferenceEquals(SelectedTab, tab))
        {
            return;
        }

        ManagementTabViewModel previous = SelectedTab;

        ChangeSelectedTab(tab);

        if (previous.CloseSound is not null)
        {
            Game.PlaySound(previous.CloseSound);
        }

        if (tab.OpenSound is not null)
        {
            Game.PlaySound(tab.OpenSound);
        }
    }

    private void ChangeSelectedTab(ManagementTabViewModel tab)
    {
        SelectedTab.IsSelected = false;
        SelectedTab = tab;
        tab.IsSelected = true;
        PlayerResources = new ResourceSetViewModel(WorldState.Players[ViewPlayerId].Resources, tab.ResourceProjection);
    }

    [RelayCommand(CanExecute = nameof(CanSelectPreviousTab))]
    private void SelectPreviousTab()
    {
        SelectTab(Tabs[(SelectedTabIndex() - 1 + Tabs.Count) % Tabs.Count]);
    }

    private bool CanSelectPreviousTab()
    {
        return Tabs.Count > 1;
    }

    [RelayCommand(CanExecute = nameof(CanSelectNextTab))]
    private void SelectNextTab()
    {
        SelectTab(Tabs[(SelectedTabIndex() + 1) % Tabs.Count]);
    }

    private bool CanSelectNextTab()
    {
        return Tabs.Count > 1;
    }

    private int SelectedTabIndex()
    {
        for (int index = 0; index < Tabs.Count; index++)
        {
            if (ReferenceEquals(Tabs[index], SelectedTab))
            {
                return index;
            }
        }

        return 0;
    }
}
