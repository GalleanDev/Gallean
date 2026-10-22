using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Gallean.Client;

internal sealed partial class DevToolsViewModel(GameViewModel game) : ObservableObject
{
    private ConfirmPromptViewModel? activationPrompt;
    private bool activationConfirmed;

    public static IReadOnlyList<bool> Switches { get; } = [false, true];

    public DiagnosticsRecorder Diagnostics { get; } = new();

    [ObservableProperty]
    public partial bool IsActive { get; private set; }

    [ObservableProperty]
    public partial bool IsHitboxOverlayEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsLightOverlayEnabled { get; set; }

    [ObservableProperty]
    public partial bool IsFogEnabled { get; set; } = true;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenEventLogCommand))]
    public partial EventLogViewModel? EventLog { get; private set; }

    partial void OnIsActiveChanged(bool value)
    {
        Diagnostics.IsEnabled = value;
    }

    public void StartEventLog(GameModel gameModel)
    {
        StopEventLog();

        EventLog = new EventLogViewModel(gameModel);
    }

    public void LogBattle(BattleModel battle)
    {
        EventLog?.LogBattle(battle);
    }

    public void StopEventLog()
    {
        EventLog?.Dispose();
        EventLog = null;
    }

    [RelayCommand]
    private void Toggle()
    {
        if (IsActive)
        {
            Deactivate();
        }
        else if (activationConfirmed)
        {
            IsActive = true;
        }
        else if (activationPrompt is not null && activationPrompt.IsOpen)
        {
            game.CloseScreen(activationPrompt);
        }
        else
        {
            activationPrompt = new ConfirmPromptViewModel(
                "Developer tools",
                new TextSpans([
                    new TextSpan("Developer tools are meant ", TextStyle.None),
                    new TextSpan("for development only", TextStyle.Highlight),
                    new TextSpan(". They reveal hidden information and change how the game looks and plays, which spoils the game experience.", TextStyle.None)
                ]),
                "Show tools",
                ConfirmPromptType.Primary,
                async () =>
                {
                    activationConfirmed = true;
                    Activate();
                });
            game.OpenScreen(activationPrompt);
        }
    }

    [RelayCommand(CanExecute = nameof(CanOpenEventLog))]
    private void OpenEventLog()
    {
        if (EventLog is null)
        {
            return;
        }

        game.OpenScreen(new EventLogPopupViewModel(EventLog.Entries));
    }

    private bool CanOpenEventLog()
    {
        return EventLog is not null;
    }

    private void Activate()
    {
        IsActive = true;
    }

    private void Deactivate()
    {
        IsActive = false;
        IsHitboxOverlayEnabled = false;
        IsLightOverlayEnabled = false;
        IsFogEnabled = true;
    }
}
