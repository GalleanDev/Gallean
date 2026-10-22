using Gallean.Common;
using Gallean.Engine;
using Gallean.Engine.Tests;

namespace Gallean.Client.Tests;

public sealed class EventLogBattleFormatterTests
{
    [Test]
    public void FormatBattleEventsTest()
    {
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 20, initiative: 50, damage: 5) with
        {
            NameTextId = new GlobalTextId("Warrior")
        };

        UnitDefinition scoutDefinition = TestHelpers.SimpleUnitDefinition("scout", maxHitpoints: 10, initiative: 40, damage: 5) with
        {
            NameTextId = new GlobalTextId("Scout")
        };

        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: warriorDefinition.MaxHitpoints);

        BattleUnit scout = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, scoutDefinition.Id, BattleSide.Defender, hitpoints: scoutDefinition.MaxHitpoints);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(warriorDefinition)
            .AddUnit(scoutDefinition)
            .Build();

        BattleState state = new BattleStateBuilder(definitions)
            .Add(warrior)
            .Add(scout)
            .Build(BattleModifiers.None);

        EventLogBattleFormatter formatter = new(definitions, TestGameTextProvider.Instance);

        Assert.That(formatter.Format(
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("warrior"), 7),
                state),
            Is.EqualTo(
            [
                new EventLogEntry(
                    EventLogEntryType.Battle,
                    [
                        new EventLogSpan("Warrior", EventLogSpanType.Actor),
                        new EventLogSpan(" hit ", EventLogSpanType.Text),
                        new EventLogSpan("Scout", EventLogSpanType.Actor),
                        new EventLogSpan(" for ", EventLogSpanType.Text),
                        new EventLogSpan("7", EventLogSpanType.Damage),
                        new EventLogSpan(" damage.", EventLogSpanType.Text)
                    ],
                    null)
            ]));
    }

    [Test]
    public void FormatWardEventsTest()
    {
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 20, initiative: 50, damage: 5) with
        {
            NameTextId = new GlobalTextId("Warrior")
        };

        UnitDefinition scoutDefinition = TestHelpers.SimpleUnitDefinition("scout", maxHitpoints: 10, initiative: 40, damage: 5) with
        {
            NameTextId = new GlobalTextId("Scout")
        };

        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: warriorDefinition.MaxHitpoints);

        BattleUnit scout = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, scoutDefinition.Id, BattleSide.Defender, hitpoints: scoutDefinition.MaxHitpoints);

        UnitWard ward = new UnitAttackSourceWard(UnitAttackSource.Fire, UnitWardCategory.Once);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(warriorDefinition)
            .AddUnit(scoutDefinition)
            .Build();

        BattleState state = new BattleStateBuilder(definitions)
            .Add(warrior)
            .Add(scout)
            .Build(BattleModifiers.None);

        EventLogBattleFormatter formatter = new(definitions, TestGameTextProvider.Instance);

        Assert.That(formatter.Format(
                new BattleUnitAttackBlockedEvent(
                    AttackerId: new BattleUnitId(1),
                    TargetId: new BattleUnitId(2),
                    AttackId: new UnitAttackDefinitionId("warrior"),
                    Ward: ward),
                state),
            Is.EqualTo(
            [
                new EventLogEntry(
                    EventLogEntryType.Battle,
                    [
                        new EventLogSpan("Warrior", EventLogSpanType.Actor),
                        new EventLogSpan(" was blocked by ", EventLogSpanType.Text),
                        new EventLogSpan("Scout", EventLogSpanType.Actor),
                        new EventLogSpan("'s ", EventLogSpanType.Text),
                        new EventLogSpan("Fire", EventLogSpanType.Object),
                        new EventLogSpan(" ward.", EventLogSpanType.Text)
                    ],
                    null)
            ]));

        Assert.That(formatter.Format(
                new BattleUnitWardConsumedEvent(
                    UnitId: new BattleUnitId(2),
                    Ward: ward),
                state),
            Is.EqualTo(
            [
                new EventLogEntry(
                    EventLogEntryType.Battle,
                    [
                        new EventLogSpan("Scout", EventLogSpanType.Actor),
                        new EventLogSpan(" consumed ", EventLogSpanType.Text),
                        new EventLogSpan("Fire", EventLogSpanType.Object),
                        new EventLogSpan(" ward.", EventLogSpanType.Text)
                    ],
                    null)
            ]));
    }

    [Test]
    public void FormatSummonedUnitEventsTest()
    {
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 20, initiative: 50, damage: 5) with
        {
            NameTextId = new GlobalTextId("Warrior")
        };

        UnitDefinition impDefinition = TestHelpers.SimpleUnitDefinition("imp", maxHitpoints: 10, initiative: 40, damage: 5) with
        {
            NameTextId = new GlobalTextId("Imp")
        };

        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: warriorDefinition.MaxHitpoints);

        BattleUnit imp = new(
            Id: new BattleUnitId(2),
            WorldUnitId: null,
            DefinitionId: impDefinition.Id,
            Side: BattleSide.Attacker,
            Hitpoints: impDefinition.MaxHitpoints,
            Experience: 0,
            ExperienceGained: 0,
            Level: 1,
            Modifiers: [],
            StatusEffects: [],
            SummonerId: new BattleUnitId(1),
            EquipmentAttackIds: [],
            Name: null,
            IsTypeLocked: false);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(warriorDefinition)
            .AddUnit(impDefinition)
            .Build();

        BattleState stateBeforeSummon = new BattleStateBuilder(definitions)
            .Add(warrior)
            .Build(BattleModifiers.None);

        BattleState stateAfterSummon = new BattleStateBuilder(definitions)
            .Add(warrior)
            .Add(imp)
            .Build(BattleModifiers.None);

        EventLogBattleFormatter formatter = new(definitions, TestGameTextProvider.Instance);

        Assert.That(formatter.Format(
                new BattleUnitSummonedEvent(
                    UnitId: new BattleUnitId(2),
                    DefinitionId: impDefinition.Id,
                    Side: BattleSide.Attacker,
                    Position: GridPosition.FrontCenter,
                    Hitpoints: impDefinition.MaxHitpoints,
                    Level: 1,
                    SummonerId: new BattleUnitId(1)),
                stateBeforeSummon),
            Is.EqualTo(
            [
                new EventLogEntry(
                    EventLogEntryType.Battle,
                    [
                        new EventLogSpan("Warrior", EventLogSpanType.Actor),
                        new EventLogSpan(" summoned ", EventLogSpanType.Text),
                        new EventLogSpan("Imp", EventLogSpanType.Actor),
                        new EventLogSpan(".", EventLogSpanType.Text)
                    ],
                    null)
            ]));

        Assert.That(formatter.Format(
                new BattleUnitAttackHitEvent(new BattleUnitId(2), new BattleUnitId(1), new UnitAttackDefinitionId("imp"), 4),
                stateAfterSummon),
            Is.EqualTo(
            [
                new EventLogEntry(
                    EventLogEntryType.Battle,
                    [
                        new EventLogSpan("Imp", EventLogSpanType.Actor),
                        new EventLogSpan(" hit ", EventLogSpanType.Text),
                        new EventLogSpan("Warrior", EventLogSpanType.Actor),
                        new EventLogSpan(" for ", EventLogSpanType.Text),
                        new EventLogSpan("4", EventLogSpanType.Damage),
                        new EventLogSpan(" damage.", EventLogSpanType.Text)
                    ],
                    null)
            ]));

        Assert.That(formatter.Format(
                new BattleUnitDiedEvent(new BattleUnitId(2)),
                stateAfterSummon),
            Is.EqualTo(
            [
                new EventLogEntry(
                    EventLogEntryType.Battle,
                    [
                        new EventLogSpan("Imp", EventLogSpanType.Actor),
                        new EventLogSpan(" died.", EventLogSpanType.Text)
                    ],
                    null)
            ]));
    }

    [Test]
    public void FormatBattleEndExperienceTest()
    {
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 20, initiative: 50, damage: 5) with
        {
            NameTextId = new GlobalTextId("Warrior")
        };

        UnitDefinition scoutDefinition = TestHelpers.SimpleUnitDefinition("scout", maxHitpoints: 10, initiative: 40, damage: 5) with
        {
            NameTextId = new GlobalTextId("Scout")
        };

        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: warriorDefinition.MaxHitpoints);

        BattleUnit scout = TestHelpers.SimpleBattleUnit(new BattleUnitId(2), null, scoutDefinition.Id, BattleSide.Defender, hitpoints: 0);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(warriorDefinition)
            .AddUnit(scoutDefinition)
            .Build();

        BattleState state = new BattleStateBuilder(definitions)
            .Add(warrior)
            .Add(scout)
            .Build(BattleModifiers.None);

        EventLogBattleFormatter formatter = new(definitions, TestGameTextProvider.Instance);

        Assert.That(formatter.Format(new BattleEndedEvent(BattleSide.Attacker), state),
            Is.EqualTo(
            [
                new EventLogEntry(
                    EventLogEntryType.Battle,
                    [
                        new EventLogSpan("Battle ended. ", EventLogSpanType.Text),
                        new EventLogSpan("Attacker", EventLogSpanType.Actor),
                        new EventLogSpan(" won.", EventLogSpanType.Text)
                    ],
                    null)
            ]));

        Assert.That(formatter.Format(new BattleUnitExperienceGainedEvent(new BattleUnitId(1), 40), state),
            Is.EqualTo(
            [
                new EventLogEntry(
                    EventLogEntryType.Battle,
                    [
                        new EventLogSpan("Warrior", EventLogSpanType.Actor),
                        new EventLogSpan(" has gained ", EventLogSpanType.Text),
                        new EventLogSpan("40", EventLogSpanType.Value),
                        new EventLogSpan(" XP this battle.", EventLogSpanType.Text)
                    ],
                    null)
            ]));

        Assert.That(formatter.Format(new BattleUnitExperienceGainedEvent(new BattleUnitId(2), 0), state),
            Is.Empty);
    }
}
