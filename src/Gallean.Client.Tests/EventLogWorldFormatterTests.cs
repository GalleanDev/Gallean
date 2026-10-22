using Gallean.Common;
using Gallean.Engine;
using Gallean.Engine.Tests;

namespace Gallean.Client.Tests;

public sealed class EventLogWorldFormatterTests
{
    [Test]
    public void FormatScenarioPopupTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .BuildSession();

        EventLogWorldFormatter formatter = new(engine.Definitions, TestGameTextProvider.Instance);

        Assert.That(formatter.Format(new WorldScenarioEventTriggeredEvent(
                new ScenarioEventId("intro"), player.Id, new ScenarioEventTurnStartedTrigger(player.Id)), session.World.State),
            Is.EqualTo(
            [
                new EventLogEntry(
                    EventLogEntryType.World,
                    [
                        new EventLogSpan("Scenario event ", EventLogSpanType.Text),
                        new EventLogSpan("intro", EventLogSpanType.Reference),
                        new EventLogSpan(" triggered.", EventLogSpanType.Text)
                    ],
                    new ScenarioEventId("intro"))
            ]));

        Assert.That(formatter.Format(new WorldScenarioPopupEvent(
                new ScenarioEventId("intro"), new ScenarioTextId("Welcome to Nevendaar."), PortraitId: null, PortraitName: null, IsPortraitLeft: true, player.Id, TriggererPosition: null, [player.Id]), session.World.State),
            Is.EqualTo(
            [
                new EventLogEntry(
                    EventLogEntryType.ScenarioPopup,
                    [
                        new EventLogSpan("Welcome to Nevendaar.", EventLogSpanType.Text)
                    ],
                    new ScenarioEventId("intro"))
            ]));
    }

    [Test]
    public void FormatRuinEventsTest()
    {
        ItemDefinition itemDefinition = new(
            new ItemDefinitionId("Item1"),
            new GlobalTextId("Talisman"),
            new GlobalTextId("desc"),
            ItemCategory.Valuable,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            Effect: null,
            AnimationUnitId: null);

        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 6);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(itemDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        EventLogWorldFormatter formatter = new(engine.Definitions, TestGameTextProvider.Instance);

        Assert.That(formatter.Format(new WorldFortificationEnteredEvent(new WorldStackId("Stack1"), new WorldFortificationId("Ruin1")), session.World.State),
            Is.EqualTo(
            [
                new EventLogEntry(
                    EventLogEntryType.World,
                    [
                        new EventLogSpan("Stack ", EventLogSpanType.Text),
                        new EventLogSpan("Stack1", EventLogSpanType.Reference),
                        new EventLogSpan(" entered ", EventLogSpanType.Text),
                        new EventLogSpan("Ruin1", EventLogSpanType.Reference),
                        new EventLogSpan(".", EventLogSpanType.Text)
                    ],
                    null)
            ]));

        Assert.That(formatter.Format(new WorldFortificationExitedEvent(new WorldStackId("Stack1"), new WorldFortificationId("Ruin1")), session.World.State),
            Is.EqualTo(
            [
                new EventLogEntry(
                    EventLogEntryType.World,
                    [
                        new EventLogSpan("Stack ", EventLogSpanType.Text),
                        new EventLogSpan("Stack1", EventLogSpanType.Reference),
                        new EventLogSpan(" exited ", EventLogSpanType.Text),
                        new EventLogSpan("Ruin1", EventLogSpanType.Reference),
                        new EventLogSpan(".", EventLogSpanType.Text)
                    ],
                    null)
            ]));

        Assert.That(formatter.Format(new WorldRuinLootedEvent(
                new WorldFortificationId("Ruin1"), new WorldStackId("Stack1"), new WorldResourceSet(200, 0, 0, 0, 0, 0), new WorldItem(new WorldItemId("Item0001"), new ItemDefinitionId("Item1"), Charges: null)), session.World.State),
            Is.EqualTo(
            [
                new EventLogEntry(
                    EventLogEntryType.World,
                    [
                        new EventLogSpan("Stack ", EventLogSpanType.Text),
                        new EventLogSpan("Stack1", EventLogSpanType.Reference),
                        new EventLogSpan(" looted ", EventLogSpanType.Text),
                        new EventLogSpan("200 gold", EventLogSpanType.Value),
                        new EventLogSpan(" and ", EventLogSpanType.Text),
                        new EventLogSpan("Talisman", EventLogSpanType.Object),
                        new EventLogSpan(" from ruin ", EventLogSpanType.Text),
                        new EventLogSpan("Ruin1", EventLogSpanType.Reference),
                        new EventLogSpan(".", EventLogSpanType.Text)
                    ],
                    null)
            ]));

        Assert.That(formatter.Format(new WorldRuinLootedEvent(
                new WorldFortificationId("Ruin1"), new WorldStackId("Stack1"), WorldResourceSet.Empty, new WorldItem(new WorldItemId("Item0001"), new ItemDefinitionId("Item1"), Charges: null)), session.World.State),
            Is.EqualTo(
            [
                new EventLogEntry(
                    EventLogEntryType.World,
                    [
                        new EventLogSpan("Stack ", EventLogSpanType.Text),
                        new EventLogSpan("Stack1", EventLogSpanType.Reference),
                        new EventLogSpan(" looted ", EventLogSpanType.Text),
                        new EventLogSpan("Talisman", EventLogSpanType.Object),
                        new EventLogSpan(" from ruin ", EventLogSpanType.Text),
                        new EventLogSpan("Ruin1", EventLogSpanType.Reference),
                        new EventLogSpan(".", EventLogSpanType.Text)
                    ],
                    null)
            ]));

        Assert.That(formatter.Format(new WorldRuinLootedEvent(
                new WorldFortificationId("Ruin1"), new WorldStackId("Stack1"), new WorldResourceSet(200, 0, 0, 0, 0, 0), null), session.World.State),
            Is.EqualTo(
            [
                new EventLogEntry(
                    EventLogEntryType.World,
                    [
                        new EventLogSpan("Stack ", EventLogSpanType.Text),
                        new EventLogSpan("Stack1", EventLogSpanType.Reference),
                        new EventLogSpan(" looted ", EventLogSpanType.Text),
                        new EventLogSpan("200 gold", EventLogSpanType.Value),
                        new EventLogSpan(" from ruin ", EventLogSpanType.Text),
                        new EventLogSpan("Ruin1", EventLogSpanType.Reference),
                        new EventLogSpan(".", EventLogSpanType.Text)
                    ],
                    null)
            ]));

        Assert.That(formatter.Format(new WorldRuinLootedEvent(
                new WorldFortificationId("Ruin1"), new WorldStackId("Stack1"), WorldResourceSet.Empty, null), session.World.State),
            Is.EqualTo(
            [
                new EventLogEntry(
                    EventLogEntryType.World,
                    [
                        new EventLogSpan("Stack ", EventLogSpanType.Text),
                        new EventLogSpan("Stack1", EventLogSpanType.Reference),
                        new EventLogSpan(" looted ", EventLogSpanType.Text),
                        new EventLogSpan("nothing", EventLogSpanType.Text),
                        new EventLogSpan(" from ruin ", EventLogSpanType.Text),
                        new EventLogSpan("Ruin1", EventLogSpanType.Reference),
                        new EventLogSpan(".", EventLogSpanType.Text)
                    ],
                    null)
            ]));
    }

    [Test]
    public void FormatTwoPlayerEventTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);
        WorldPlayer player3 = TestHelpers.SimplePlayer(new WorldPlayerId(3), WorldRace.Dwarf, map);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithDefinition(TestHelpers.SimpleLord(player3.LordId, player3.Race, LordCategory.Warrior))
            .WithPlayer(player3)
            .BuildSession();

        EventLogWorldFormatter formatter = new(engine.Definitions, TestGameTextProvider.Instance);

        Assert.That(formatter.Format(new WorldGoldGivenEvent(player1.Id, player2.Id, 100), session.World.State),
            Is.EqualTo(
            [
                new EventLogEntry(
                    EventLogEntryType.World,
                    [
                        new EventLogSpan("Player 1", EventLogSpanType.Actor),
                        new EventLogSpan(" gave ", EventLogSpanType.Text),
                        new EventLogSpan("100 gold", EventLogSpanType.Value),
                        new EventLogSpan(" to ", EventLogSpanType.Text),
                        new EventLogSpan("Player 2", EventLogSpanType.Actor),
                        new EventLogSpan(".", EventLogSpanType.Text)
                    ],
                    null)
            ]));

        Assert.That(formatter.Format(new WorldTurnEndedEvent(player1.Id), session.World.State),
            Is.EqualTo(
            [
                new EventLogEntry(
                    EventLogEntryType.World,
                    [
                        new EventLogSpan("Player 1", EventLogSpanType.Actor),
                        new EventLogSpan(" ended the turn.", EventLogSpanType.Text)
                    ],
                    null)
            ]));
    }

    [Test]
    public void FormatStackEventsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(1, 1);
        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        UnitDefinition leader = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("Leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 6);
        IGameDefinitionProvider stackDefinitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(leader)
            .Build();

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Stack1"),
            player.Id,
            new WorldPosition(0, 0),
            TestHelpers.StackUnitGrid(
                stackDefinitions,
                [(new WorldUnit(new WorldUnitId("leader"), leader.Id, 100, 0, 1, [], null, false), GridPosition.FrontBottom)]),
            movementPoints: 6);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leader)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .BuildSession();

        EventLogWorldFormatter formatter = new(engine.Definitions, TestGameTextProvider.Instance);

        Assert.That(formatter.Format(new WorldUnitDamagedEvent(new WorldUnitGroup.Stack(new WorldStackId("Stack1")), new WorldUnitId("leader"), 40, SourcePlayerId: null), session.World.State),
            Is.EqualTo(
            [
                new EventLogEntry(
                    EventLogEntryType.World,
                    [
                        new EventLogSpan("name", EventLogSpanType.Actor),
                        new EventLogSpan(" in ", EventLogSpanType.Text),
                        new EventLogSpan("stack ", EventLogSpanType.Text),
                        new EventLogSpan("Stack1", EventLogSpanType.Reference),
                        new EventLogSpan(" damaged to ", EventLogSpanType.Text),
                        new EventLogSpan("40", EventLogSpanType.Damage),
                        new EventLogSpan(" HP.", EventLogSpanType.Text)
                    ],
                    null)
            ]));

        Assert.That(formatter.Format(new WorldStackOrderChangedEvent(new WorldStackId("Stack1"), new WorldStackOrder.AttackStack(new WorldStackId("Stack2"))), session.World.State),
            Is.EqualTo(
            [
                new EventLogEntry(
                    EventLogEntryType.World,
                    [
                        new EventLogSpan("Stack ", EventLogSpanType.Text),
                        new EventLogSpan("Stack1", EventLogSpanType.Reference),
                        new EventLogSpan(" changed its order.", EventLogSpanType.Text)
                    ],
                    null)
            ]));

        Assert.That(formatter.Format(new WorldItemUnequippedEvent(new WorldStackId("Stack1"), LeaderEquipmentSlot.Battle1), session.World.State),
            Is.EqualTo(
            [
                new EventLogEntry(
                    EventLogEntryType.World,
                    [
                        new EventLogSpan("Stack ", EventLogSpanType.Text),
                        new EventLogSpan("Stack1", EventLogSpanType.Reference),
                        new EventLogSpan(" unequipped ", EventLogSpanType.Text),
                        new EventLogSpan("Battle item I", EventLogSpanType.Object),
                        new EventLogSpan(".", EventLogSpanType.Text)
                    ],
                    null)
            ]));
    }
}
