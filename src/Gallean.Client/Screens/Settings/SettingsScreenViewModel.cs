using System.ComponentModel;
using Gallean.Configuration;

namespace Gallean.Client;

internal sealed class SettingsScreenViewModel : AbstractScreenViewModel
{
    private readonly GameViewModel gameViewModel;
    private int soundVolume;
    private int musicVolume;
    private int renderResolution;
    private int autoSaveInterval;
    private int autoSaveLimit;
    private BattleSpeed battleSpeed;
    private MovementSpeed movementSpeed;
    private int? uiScale;
    private bool lockCursor;

    public SettingsScreenViewModel(GameViewModel gameViewModel)
    {
        this.gameViewModel = gameViewModel;

        soundVolume = gameViewModel.Settings.SoundVolume;
        musicVolume = gameViewModel.Settings.MusicVolume;
        renderResolution = gameViewModel.Settings.RenderResolution;
        autoSaveInterval = gameViewModel.Settings.AutoSaveInterval;
        autoSaveLimit = gameViewModel.Settings.AutoSaveLimit;
        battleSpeed = gameViewModel.Settings.BattleSpeed;
        movementSpeed = gameViewModel.Settings.MovementSpeed;
        uiScale = gameViewModel.Settings.UiScale;
        lockCursor = gameViewModel.Settings.LockCursor;

        gameViewModel.PropertyChanged += OnGamePropertyChanged;
    }

    public int MinVolume => GameSettings.MinVolume;
    public int MaxVolume => GameSettings.MaxVolume;

    public IReadOnlyList<int> RenderResolutions { get; } = [50, 75, 100];
    public IReadOnlyList<UiScaleOption> UiScales { get; } = [new(null), new(100), new(125), new(150), new(175), new(200), new(250), new(300)];
    public IReadOnlyList<int> AutoSaveIntervals { get; } = [1, 3, 5, 10, 0];
    public IReadOnlyList<int> AutoSaveLimits { get; } = [1, 3, 5, 10, 20];
    public IReadOnlyList<BattleSpeed> BattleSpeeds { get; } = Enum.GetValues<BattleSpeed>();
    public IReadOnlyList<MovementSpeed> MovementSpeeds { get; } = Enum.GetValues<MovementSpeed>();

    public int SoundVolume
    {
        get => soundVolume;
        set
        {
            if (SetProperty(ref soundVolume, value))
            {
                Apply();
            }
        }
    }

    public int MusicVolume
    {
        get => musicVolume;
        set
        {
            if (SetProperty(ref musicVolume, value))
            {
                Apply();
            }
        }
    }

    public int RenderResolution
    {
        get => renderResolution;
        set
        {
            if (SetProperty(ref renderResolution, value))
            {
                Apply();
            }
        }
    }

    public UiScaleOption UiScale
    {
        get => new(uiScale);
        set
        {
            if (SetProperty(ref uiScale, value.Scale))
            {
                Apply();
            }
        }
    }

    public int AutoSaveInterval
    {
        get => autoSaveInterval;
        set
        {
            if (SetProperty(ref autoSaveInterval, value))
            {
                Apply();
            }
        }
    }

    public int AutoSaveLimit
    {
        get => autoSaveLimit;
        set
        {
            if (SetProperty(ref autoSaveLimit, value))
            {
                Apply();
            }
        }
    }

    public BattleSpeed BattleSpeed
    {
        get => battleSpeed;
        set
        {
            if (SetProperty(ref battleSpeed, value))
            {
                Apply();
            }
        }
    }

    public MovementSpeed MovementSpeed
    {
        get => movementSpeed;
        set
        {
            if (SetProperty(ref movementSpeed, value))
            {
                Apply();
            }
        }
    }

    public bool LockCursor
    {
        get => lockCursor;
        set
        {
            if (SetProperty(ref lockCursor, value))
            {
                Apply();
            }
        }
    }

    public bool Windowed
    {
        get => gameViewModel.IsWindowed;
        set => gameViewModel.IsWindowed = value;
    }

    public override void Dispose()
    {
        gameViewModel.PropertyChanged -= OnGamePropertyChanged;

        base.Dispose();
    }

    protected override bool TryClose()
    {
        CloseSelf();

        return true;
    }

    private void Apply()
    {
        gameViewModel.UpdateSettings(GameSettings.Create(soundVolume, musicVolume, gameViewModel.IsWindowed, renderResolution, autoSaveInterval, autoSaveLimit, battleSpeed, uiScale, lockCursor, movementSpeed));
    }

    private void OnGamePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GameViewModel.IsWindowed))
        {
            OnPropertyChanged(nameof(Windowed));
        }
    }
}
