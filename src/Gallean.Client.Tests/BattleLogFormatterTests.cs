using Gallean.Common;
using Gallean.Engine;
using Gallean.Engine.Tests;

namespace Gallean.Client.Tests;

public sealed class BattleLogFormatterTests
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

        BattleLogFormatter formatter = new(definitions, TestGameTextProvider.Instance);

        Assert.That(formatter.Format(
                new BattleUnitAttackHitEvent(new BattleUnitId(1), new BattleUnitId(2), new UnitAttackDefinitionId("warrior"), 7),
                state),
            Is.EqualTo(new BattleLogEventEntry(new TextSpans(
            [
                new TextSpan("Warrior", TextStyle.Highlight),
                new TextSpan(" hit ", TextStyle.None),
                new TextSpan("Scout", TextStyle.Highlight),
                new TextSpan(" for 7 damage.", TextStyle.None)
            ]))));
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

        BattleLogFormatter formatter = new(definitions, TestGameTextProvider.Instance);

        Assert.That(formatter.Format(
                new BattleUnitAttackBlockedEvent(
                    AttackerId: new BattleUnitId(1),
                    TargetId: new BattleUnitId(2),
                    AttackId: new UnitAttackDefinitionId("warrior"),
                    Ward: ward),
                state),
            Is.EqualTo(new BattleLogEventEntry(new TextSpans(
            [
                new TextSpan("Warrior", TextStyle.Highlight),
                new TextSpan(" was blocked by the ", TextStyle.None),
                new TextSpan("Fire", TextStyle.Italic),
                new TextSpan(" ward of ", TextStyle.None),
                new TextSpan("Scout", TextStyle.Highlight),
                new TextSpan(".", TextStyle.None)
            ]))));

        Assert.That(formatter.Format(
                new BattleUnitWardConsumedEvent(
                    UnitId: new BattleUnitId(2),
                    Ward: ward),
                state),
            Is.EqualTo(new BattleLogEventEntry(new TextSpans(
            [
                new TextSpan("Scout", TextStyle.Highlight),
                new TextSpan(" used the ", TextStyle.None),
                new TextSpan("Fire", TextStyle.Italic),
                new TextSpan(" ward.", TextStyle.None)
            ]))));
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

        BattleLogFormatter formatter = new(definitions, TestGameTextProvider.Instance);

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
            Is.EqualTo(new BattleLogEventEntry(new TextSpans(
            [
                new TextSpan("Warrior", TextStyle.Highlight),
                new TextSpan(" summoned ", TextStyle.None),
                new TextSpan("Imp", TextStyle.Highlight),
                new TextSpan(".", TextStyle.None)
            ]))));

        Assert.That(formatter.Format(
                new BattleUnitAttackHitEvent(new BattleUnitId(2), new BattleUnitId(1), new UnitAttackDefinitionId("imp"), 4),
                stateAfterSummon),
            Is.EqualTo(new BattleLogEventEntry(new TextSpans(
            [
                new TextSpan("Imp", TextStyle.Highlight),
                new TextSpan(" hit ", TextStyle.None),
                new TextSpan("Warrior", TextStyle.Highlight),
                new TextSpan(" for 4 damage.", TextStyle.None)
            ]))));

        Assert.That(formatter.Format(
                new BattleUnitDiedEvent(new BattleUnitId(2)),
                stateAfterSummon),
            Is.EqualTo(new BattleLogEventEntry(new TextSpans(
            [
                new TextSpan("Imp", TextStyle.Highlight),
                new TextSpan(" died.", TextStyle.None)
            ]))));
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

        BattleLogFormatter formatter = new(definitions, TestGameTextProvider.Instance);

        Assert.That(formatter.Format(new BattleEndedEvent(BattleSide.Attacker), state),
            Is.EqualTo(new BattleLogEventEntry(new TextSpans(
            [
                new TextSpan("The attacker won.", TextStyle.None)
            ]))));

        Assert.That(formatter.Format(new BattleUnitExperienceGainedEvent(new BattleUnitId(1), 40), state),
            Is.EqualTo(new BattleLogEventEntry(new TextSpans(
            [
                new TextSpan("Warrior", TextStyle.Highlight),
                new TextSpan(" gained 40 experience.", TextStyle.None)
            ]))));

        Assert.That(formatter.Format(new BattleUnitExperienceGainedEvent(new BattleUnitId(2), 0), state),
            Is.Null);
    }

    [Test]
    public void FormatRoundTest()
    {
        UnitDefinition warriorDefinition = TestHelpers.SimpleUnitDefinition("warrior", maxHitpoints: 20, initiative: 50, damage: 5);

        BattleUnit warrior = TestHelpers.SimpleBattleUnit(new BattleUnitId(1), null, warriorDefinition.Id, BattleSide.Attacker, hitpoints: warriorDefinition.MaxHitpoints);

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddUnit(warriorDefinition)
            .Build();

        BattleState state = new BattleStateBuilder(definitions)
            .Add(warrior)
            .Build(BattleModifiers.None);

        BattleLogFormatter formatter = new(definitions, TestGameTextProvider.Instance);

        Assert.That(formatter.Format(new BattleRoundStartedEvent(BattleRound.Normal(2), []), state),
            Is.EqualTo(new BattleLogRoundEntry("Round 2")));
    }
}
