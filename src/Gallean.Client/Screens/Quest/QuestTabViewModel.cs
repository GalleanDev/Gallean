using System.Collections.Immutable;
using CommunityToolkit.Mvvm.ComponentModel;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class QuestTabViewModel : ManagementTabViewModel
{
    private readonly ScenarioTextFormatter formatter;

    public QuestTabViewModel(GameModel gameModel, GameViewModel gameViewModel, ManagementScreenViewModel screen)
        : base(gameModel, gameViewModel)
    {
        ScenarioInfoTexts infoTexts = gameModel.Scenario.Definition.InfoTexts;
        WorldPlayer player = WorldState.Players[ViewPlayerId];

        Screen = screen;
        formatter = new ScenarioTextFormatter(gameModel.Scenario.Names);
        LordPortraitKey = new AssetSpriteKey.LordFace(player.Race, player.FaceVariant.Value, IsLarge: true);
        Title = gameModel.Texts.GetScenarioText(infoTexts.Name);
        Briefing = FormatBriefing(infoTexts.Briefing);

        RefreshView();
    }

    public override string TabHeader => Tr("Quest");
    public override AssetSoundKey CloseSound => new AssetSoundKey.BookClose();

    public ManagementScreenViewModel Screen { get; }
    public AssetSpriteKey.LordFace LordPortraitKey { get; }
    public string Title { get; }
    public ImmutableArray<ScenarioTextSpan> Briefing { get; }
    public bool HasBriefing => !Briefing.IsEmpty;

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

    private ImmutableArray<ScenarioTextSpan> FormatBriefing(ScenarioTextId? textId)
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
        Objective = FormatObjective(WorldState.ScenarioState.ObjectiveTextId);
        TurnText = Tr("Turn {0}", WorldState.TurnInfo.TurnNumber);
        Dialogs = BuildDialogs();
    }

    private IReadOnlyList<QuestDialogViewModel> BuildDialogs()
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
