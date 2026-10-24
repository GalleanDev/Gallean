using System.Collections.Immutable;
using Gallean.Common;
using Gallean.Engine;
using Gallean.Engine.Tests;
using Gallean.Tests.Common;

namespace Gallean.Client.Tests;

public sealed class QuestTabViewModelTests
{
    private static IReadOnlyList<string> FormatDialogs(IReadOnlyList<QuestDialogViewModel> dialogs)
    {
        List<string> rows = [];

        foreach (QuestDialogViewModel dialog in dialogs)
        {
            rows.Add(dialog.TurnText);

            foreach (QuestMessageViewModel message in dialog.Messages)
            {
                rows.Add($"  {FormatSide(message)} | {message.Message.PortraitName} | {string.Concat(message.Message.Spans.Select(span => span.Text))}");
            }
        }

        return rows;
    }

    private static string FormatSide(QuestMessageViewModel message)
    {
        if (message.IsNarration)
        {
            return "narration";
        }
        else if (message.HasLeftPortrait)
        {
            return "left";
        }
        else
        {
            return "right";
        }
    }

    [Test]
    public void RecordedPopupsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);
        WorldPlayer other = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);
        UnitDefinition speaker = TestHelpers.SimpleUnitDefinition("speaker", maxHitpoints: 100, initiative: 50, damage: 10);

        WorldScenarioState scenarioState = WorldScenarioState.Empty with
        {
            Messages =
            [
                new WorldScenarioMessage(
                    TurnNumber: 1,
                    new ScenarioEventId("Event1"),
                    new ScenarioTextId("The dead stir in the barrows to the north."),
                    ScenarioEventPortraitId.Unit(speaker.Id),
                    PortraitName: null,
                    IsPortraitLeft: true,
                    player.Id,
                    [player.Id]),
                new WorldScenarioMessage(
                    TurnNumber: 1,
                    new ScenarioEventId("Event1"),
                    new ScenarioTextId("Gorlak\n\"Your scouts serve a new master now.\""),
                    ScenarioEventPortraitId.Unit(speaker.Id),
                    PortraitName: null,
                    IsPortraitLeft: false,
                    player.Id,
                    [player.Id]),
                new WorldScenarioMessage(
                    TurnNumber: 2,
                    new ScenarioEventId("Event2"),
                    new ScenarioTextId("The Legions march on the capital."),
                    PortraitId: null,
                    PortraitName: null,
                    IsPortraitLeft: true,
                    other.Id,
                    [other.Id]),
                new WorldScenarioMessage(
                    TurnNumber: 3,
                    new ScenarioEventId("Event3"),
                    new ScenarioTextId("A cold wind sweeps across the plains."),
                    PortraitId: null,
                    PortraitName: null,
                    IsPortraitLeft: true,
                    player.Id,
                    [player.Id, other.Id])
            ],
            ObjectiveTextId = new ScenarioTextId("Objective: Hold the pass until the thaw.")
        };

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithScenarioId(new ScenarioId("quest-1"))
            .WithTurnInfo(new WorldTurnInfo(4, player.Id))
            .WithDefinition(speaker)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(other.LordId, other.Race, LordCategory.Warrior))
            .WithPlayer(other)
            .WithScenarioState(scenarioState)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel screen = ManagementScreenViewModel.CreateForQuest(gameModel, gameViewModel);
        QuestTabViewModel quest = Assert.IsOfType<QuestTabViewModel>(screen.Tabs[0]);

        Assert.That(quest.TabHeader, Is.EqualTo("Quest"));
        Assert.That(quest.Title, Is.EqualTo("quest-1:NAME:0"));
        Assert.That(quest.Objective, Is.EqualTo("Hold the pass until the thaw."));
        Assert.That(quest.TurnText, Is.EqualTo("Turn 4"));
        Assert.That(quest.HasBriefing, Is.True);
        Assert.That(quest.Briefing, Is.EqualTo([new ScenarioTextSpan("quest-1:BRIEFLONG:0", ScenarioTextSpanType.Text)]));
        Assert.That(FormatDialogs(quest.Dialogs), Is.EqualTo(
        [
            "Turn 1",
            "  left | name | The dead stir in the barrows to the north.",
            "  right | Gorlak | Your scouts serve a new master now.",
            "Turn 3",
            "  narration |  | A cold wind sweeps across the plains."
        ]));
    }

    [Test]
    public async Task PopupRefreshTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        ScenarioEventDefinition scenarioEvent = new(
            Id: new ScenarioEventId("Event1"),
            AffectedRaces: [WorldRace.Human],
            VerifiedRaces: [WorldRace.Human],
            Conditions: [new ScenarioEventFrequencyCondition(1)],
            Effects:
            [
                new ScenarioEventPopupEffect(new ScenarioTextId("Greetings Milord!"), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false),
                new ScenarioEventPopupEffect(new ScenarioTextId("The road south is open."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)
            ],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        (GameSession session, _) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithScenarioEvent(scenarioEvent)
            .BuildSession();

        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel screen = ManagementScreenViewModel.CreateForQuest(gameModel, gameViewModel);
        QuestTabViewModel quest = Assert.IsOfType<QuestTabViewModel>(screen.Tabs[0]);

        Assert.That(quest.Objective, Is.Null);
        Assert.That(quest.Dialogs, Is.Empty);

        await gameModel.World.DispatchCommandAsync(new WorldStartGameCommand());

        Assert.That(FormatDialogs(quest.Dialogs), Is.EqualTo(
        [
            "Turn 1",
            "  narration |  | Greetings Milord!",
            "  narration |  | The road south is open."
        ]));
    }
}
