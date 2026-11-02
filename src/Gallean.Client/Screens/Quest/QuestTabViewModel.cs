using System.Collections.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class QuestTabViewModel : ManagementTabViewModel
{
    public QuestTabViewModel(GameModel gameModel, GameViewModel gameViewModel, ManagementScreenViewModel screen)
        : base(gameModel, gameViewModel)
    {
        WorldPlayer player = WorldState.Players[ViewPlayerId];

        Screen = screen;
        LordPortraitKey = new AssetSpriteKey.LordFace(player.Race, player.FaceVariant.Value, IsLarge: true);

        RefreshView();
    }

    public override string TabHeader => Tr("Quest");
    public override AssetSoundKey CloseSound => new AssetSoundKey.BookClose();

    public ManagementScreenViewModel Screen { get; }
    public AssetSpriteKey.LordFace LordPortraitKey { get; }
    public bool HasBriefing => !Briefing.IsEmpty;

    [ObservableProperty]
    public partial string Title { get; private set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasBriefing))]
    public partial ImmutableArray<ScenarioTextSpan> Briefing { get; private set; } = [];

    [ObservableProperty]
    public partial string? Objective { get; private set; }

    [ObservableProperty]
    public partial string TurnText { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial IReadOnlyList<QuestDialogViewModel> Dialogs { get; private set; } = [];

    protected override void OnSnapshotChanged(GameSnapshot current)
    {
        RefreshView();
    }

    private static bool IsSameDialog(WorldScenarioMessage left, WorldScenarioMessage right)
    {
        return left.TurnNumber == right.TurnNumber &&
               left.EventId == right.EventId &&
               left.TriggererId == right.TriggererId &&
               left.Audience.SequenceEqual(right.Audience);
    }

    private ImmutableArray<ScenarioTextSpan> FormatBriefing(ScenarioTextFormatter formatter, ScenarioTextId? textId)
    {
        if (textId is not null)
        {
            string text = GameModel.Texts.GetScenarioText(textId.Value);

            return formatter.Format(ScenarioFormatter.FormatBriefing(text));
        }
        else
        {
            return [];
        }
    }

    private string? FormatObjective(ScenarioTextId? textId)
    {
        if (textId is not null)
        {
            string text = GameModel.Texts.GetScenarioText(textId.Value);

            return ScenarioFormatter.FormatObjective(text);
        }
        else
        {
            return null;
        }
    }

    private void RefreshView()
    {
        ScenarioInfoTexts infoTexts = GameModel.Scenario.Definition.InfoTexts;
        ScenarioTextFormatter formatter = new(GameModel.Texts.Names);

        Title = GameModel.Texts.GetScenarioText(infoTexts.Name);
        Briefing = FormatBriefing(formatter, infoTexts.Briefing);
        Objective = FormatObjective(WorldState.ScenarioState.ObjectiveTextId);
        TurnText = Tr("Turn {0}", WorldState.TurnInfo.TurnNumber);
        Dialogs = BuildDialogs(formatter);
    }

    private IReadOnlyList<QuestDialogViewModel> BuildDialogs(ScenarioTextFormatter formatter)
    {
        List<QuestDialogViewModel> dialogs = [];
        List<QuestMessageViewModel> messages = [];
        WorldScenarioMessage? previous = null;

        foreach (WorldScenarioMessage scenarioMessage in WorldState.ScenarioState.Messages)
        {
            if (!scenarioMessage.Audience.Contains(ViewPlayerId))
            {
                continue;
            }

            if (previous is not null && !IsSameDialog(previous, scenarioMessage))
            {
                dialogs.Add(new QuestDialogViewModel(previous.TurnNumber, messages));
                messages = [];
            }

            ScenarioEventPopupMessage message = ScenarioEventPopupMessage.Create(GameModel.Texts, GameModel.Definitions, formatter, scenarioMessage);

            messages.Add(new QuestMessageViewModel(message));
            previous = scenarioMessage;
        }

        if (previous is not null)
        {
            dialogs.Add(new QuestDialogViewModel(previous.TurnNumber, messages));
        }

        return dialogs;
    }
}
