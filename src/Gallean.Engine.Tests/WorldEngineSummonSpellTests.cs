using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineSummonSpellTests
{
    private static readonly WorldSpellDefinitionId SummonSpellId = new("summon");

    private static WorldSpellDefinition SummonSpell(UnitDefinitionId unit, WorldResourceSet castingCost)
    {
        return TestHelpers.SimpleSpellDefinition(SummonSpellId, new WorldSummonStackSpellEffect(unit)) with
        {
            CastingCost = castingCost
        };
    }

    private static UnitDefinition SummonedUnitDefinition(string id, int lifetime)
    {
        return TestHelpers.SimpleUnitDefinition(id, maxHitpoints: 100, initiative: 50, damage: 10) with
        {
            Category = UnitCategory.Summon,
            Leadership = 1,
            MovementPoints = 20,
            Lifetime = lifetime
        };
    }

    private static WorldStack SummonedStack(WorldUnitId unitId, WorldStackId id, WorldPlayerId ownerId, UnitDefinition unit, WorldPosition position, int expirationTurn)
    {
        return TestHelpers.SimpleStack(
                id,
                ownerId,
                position,
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(unitId, unit), GridPosition.FrontCenter, unit.IsLarge)
                    .Build(),
                movementPoints: 20) with
            {
                ExpirationTurn = expirationTurn
            };
    }

    [Test]
    public void SuccessTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition summon = SummonedUnitDefinition("summon", lifetime: 1);
        WorldSpellDefinition spell = SummonSpell(summon.Id, castingCost: new WorldResourceSet(0, 0, 0, 40, 0, 0));

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(0, 0, 0, 100, 0, 0),
            Spellbook = new WorldPlayerSpellbook([], [SummonSpellId])
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(summon)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(SummonSpellId, new WorldSpellTarget(new WorldPosition(1, 1), Radius: 0)),
            [
                new WorldSpellCastEvent(player.Id, SummonSpellId, new WorldSpellTarget(new WorldPosition(1, 1), Radius: 0), new WorldResourceSet(0, 0, 0, 40, 0, 0), WorldSpellCastSource.Player),
                new WorldStackCreatedEvent(
                    SummonedStack(new WorldUnitId("Unit0001"), new WorldStackId("Stack0001"), player.Id, summon, new WorldPosition(1, 1), expirationTurn: 2),
                    WorldResourceSet.Empty)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = new WorldResourceSet(0, 0, 0, 60, 0, 0),
                    TurnActions = new WorldPlayerTurnActions
                    {
                        SpellCasts = ImmutableSortedDictionary<WorldSpellDefinitionId, int>.Empty.Add(SummonSpellId, 1)
                    }
                }),
                Stacks = engine.State.Stacks.SetItem(
                    new WorldStackId("Stack0001"),
                    SummonedStack(new WorldUnitId("Unit0001"), new WorldStackId("Stack0001"), player.Id, summon, new WorldPosition(1, 1), expirationTurn: 2))
            }));
    }

    [Test]
    public void ExpirationTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition summon = SummonedUnitDefinition("summon", lifetime: 1);
        WorldSpellDefinition spell = SummonSpell(summon.Id, castingCost: WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [SummonSpellId])
        };

        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(summon)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .Build();

        WorldStackId stackId = new("Stack0001");

        engine.CheckCommand(
            new WorldCastSpellCommand(SummonSpellId, new WorldSpellTarget(new WorldPosition(1, 1), Radius: 0)),
            [
                new WorldSpellCastEvent(player1.Id, SummonSpellId, new WorldSpellTarget(new WorldPosition(1, 1), Radius: 0), WorldResourceSet.Empty, WorldSpellCastSource.Player),
                new WorldStackCreatedEvent(
                    SummonedStack(new WorldUnitId("Unit0001"), stackId, player1.Id, summon, new WorldPosition(1, 1), expirationTurn: 2),
                    WorldResourceSet.Empty)
            ]);

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(player1.Id),
            new WorldTurnStartingEvent(player2.Id),
            new WorldTurnStartedEvent(player2.Id),
            new WorldTurnResourcesGeneratedEvent(player2.Id, new WorldResourceSet(100, 0, 0, 0, 25, 0))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(
                    stackId,
                    SummonedStack(new WorldUnitId("Unit0001"), stackId, player1.Id, summon, new WorldPosition(1, 1), expirationTurn: 2))
            }));

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(player2.Id),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(player1.Id),
            new WorldStackMovementRestoredEvent(stackId, 20),
            new WorldStackDestroyedEvent(stackId, Destroyer: null, DestroyerPlayerId: null),
            new WorldTurnStartedEvent(player1.Id),
            new WorldTurnResourcesGeneratedEvent(player1.Id, new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.Remove(stackId)
            }));
    }

    [Test]
    public void LifetimeTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition summon = SummonedUnitDefinition("summon", lifetime: 3);
        WorldSpellDefinition spell = SummonSpell(summon.Id, castingCost: WorldResourceSet.Empty);

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [SummonSpellId])
        };

        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(summon)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .Build();

        WorldStackId stackId = new("Stack0001");

        engine.CheckCommand(
            new WorldCastSpellCommand(SummonSpellId, new WorldSpellTarget(new WorldPosition(1, 1), Radius: 0)),
            [
                new WorldSpellCastEvent(player1.Id, SummonSpellId, new WorldSpellTarget(new WorldPosition(1, 1), Radius: 0), WorldResourceSet.Empty, WorldSpellCastSource.Player),
                new WorldStackCreatedEvent(
                    SummonedStack(new WorldUnitId("Unit0001"), stackId, player1.Id, summon, new WorldPosition(1, 1), expirationTurn: 4),
                    WorldResourceSet.Empty)
            ]);

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(player1.Id),
            new WorldTurnStartingEvent(player2.Id),
            new WorldTurnStartedEvent(player2.Id),
            new WorldTurnResourcesGeneratedEvent(player2.Id, new WorldResourceSet(100, 0, 0, 0, 25, 0))
        ]);

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(player2.Id),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(player1.Id),
            new WorldStackMovementRestoredEvent(stackId, 20),
            new WorldTurnStartedEvent(player1.Id),
            new WorldTurnResourcesGeneratedEvent(player1.Id, new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(
                    stackId,
                    SummonedStack(new WorldUnitId("Unit0001"), stackId, player1.Id, summon, new WorldPosition(1, 1), expirationTurn: 4))
            }));
    }

    [Test]
    public void LargeSummonTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition summon = SummonedUnitDefinition("summon", lifetime: 1) with { IsLarge = true };
        WorldSpellDefinition spell = SummonSpell(summon.Id, castingCost: WorldResourceSet.Empty);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [SummonSpellId])
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(summon)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .Build();

        engine.CheckCommand(
            new WorldCastSpellCommand(SummonSpellId, new WorldSpellTarget(new WorldPosition(1, 1), Radius: 0)),
            [
                new WorldSpellCastEvent(player.Id, SummonSpellId, new WorldSpellTarget(new WorldPosition(1, 1), Radius: 0), WorldResourceSet.Empty, WorldSpellCastSource.Player),
                new WorldStackCreatedEvent(
                    SummonedStack(new WorldUnitId("Unit0001"), new WorldStackId("Stack0001"), player.Id, summon, new WorldPosition(1, 1), expirationTurn: 2),
                    WorldResourceSet.Empty)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(
                    new WorldStackId("Stack0001"),
                    SummonedStack(new WorldUnitId("Unit0001"), new WorldStackId("Stack0001"), player.Id, summon, new WorldPosition(1, 1), expirationTurn: 2))
            }));
    }

    [Test]
    public void TransferForbiddenTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "^^^^^^",
            "^^^^^^",
            "^^^^^^",
            "^^^..."
        ]);

        UnitDefinition summon = SummonedUnitDefinition("summon", lifetime: 1);
        WorldSpellDefinition spell = SummonSpell(summon.Id, castingCost: WorldResourceSet.Empty);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [SummonSpellId])
        };

        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(
            new WorldCityDefinitionId(WorldRace.Undead, 1),
            WorldResourceSet.Empty);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city1"),
            cityDefinition,
            player.Id,
            new WorldPosition(3, 3),
            UnitGrid<WorldUnit>.Empty);

        WorldStack stack = SummonedStack(new WorldUnitId("stack1Unit"), new WorldStackId("Stack1"), player.Id, summon, new WorldPosition(3, 3), expirationTurn: 2);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(summon)
            .WithDefinition(spell)
            .WithDefinition(cityDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithCity(city)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .Build();

        engine.CheckCommandError(
            new WorldTransferUnitCommand(
                new WorldUnitGroup.Stack(stack.Id),
                new WorldUnitGroup.Fortification(city.Id),
                new WorldUnitId("stack1Unit"),
                GridPosition.FrontBottom),
            WorldCommandError.LeaderTransferForbidden);
    }

    [Test]
    public void DisbandForbiddenTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition summon = SummonedUnitDefinition("summon", lifetime: 1);
        WorldSpellDefinition spell = SummonSpell(summon.Id, castingCost: WorldResourceSet.Empty);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [SummonSpellId])
        };

        WorldStack stack = SummonedStack(new WorldUnitId("stack1Unit"), new WorldStackId("Stack1"), player.Id, summon, new WorldPosition(1, 1), expirationTurn: 2);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(summon)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .Build();

        engine.CheckCommandError(
            new WorldDisbandUnitCommand(new WorldUnitGroup.Stack(stack.Id), new WorldUnitId("stack1Unit")),
            WorldCommandError.LeaderDisbandForbidden);
    }

    [Test]
    public void TargetOccupiedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition summon = SummonedUnitDefinition("summon", lifetime: 1);
        WorldSpellDefinition spell = SummonSpell(summon.Id, castingCost: WorldResourceSet.Empty);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [SummonSpellId])
        };

        WorldStack stack = SummonedStack(new WorldUnitId("stack1Unit"), new WorldStackId("Stack1"), player.Id, summon, new WorldPosition(1, 1), expirationTurn: 2);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(summon)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .Build();

        engine.CheckCommandError(
            new WorldCastSpellCommand(SummonSpellId, new WorldSpellTarget(new WorldPosition(1, 1), Radius: 0)),
            WorldCommandError.TargetOccupied);
    }

    [Test]
    public void TargetImpassableTest()
    {
        WorldMap map = TestHelpers.BuildMap([
            "..",
            ".^"
        ]);

        UnitDefinition summon = SummonedUnitDefinition("summon", lifetime: 1);
        WorldSpellDefinition spell = SummonSpell(summon.Id, castingCost: WorldResourceSet.Empty);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [SummonSpellId])
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(summon)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .Build();

        engine.CheckCommandError(
            new WorldCastSpellCommand(SummonSpellId, new WorldSpellTarget(new WorldPosition(1, 1), Radius: 0)),
            WorldCommandError.SpellTargetInvalid);
    }

    [Test]
    public void TargetFoggedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition summon = SummonedUnitDefinition("summon", lifetime: 1);
        WorldSpellDefinition spell = SummonSpell(summon.Id, castingCost: WorldResourceSet.Empty);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [SummonSpellId]),
            FogOfWar = ImmutableBitmap.Create(map.Width, map.Height)
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(summon)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .Build();

        engine.CheckCommandError(
            new WorldCastSpellCommand(SummonSpellId, new WorldSpellTarget(new WorldPosition(1, 1), Radius: 0)),
            WorldCommandError.SpellTargetInvalid);
    }

    [Test]
    public void ExpiredLifetimeWithUnitModifierTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition summon = SummonedUnitDefinition("summon", lifetime: 1);

        UnitModifierDefinition modifier = TestHelpers.SimpleModifier("boost") with
        {
            Damage = new UnitStatModifier(0, 50)
        };

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStackId stackId = new("Stack0001");

        WorldStack stack = TestHelpers.SimpleStack(
                stackId,
                player.Id,
                new WorldPosition(1, 1),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("summon"), summon) with { Modifiers = [new UnitModifier(modifier.Id, 1)] }, GridPosition.FrontCenter, summon.IsLarge)
                    .Build(),
                movementPoints: 20) with
            {
                ExpirationTurn = 1
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(summon)
            .WithDefinition(modifier)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(stackId, 20),
            new WorldUnitModifierExpiredEvent(new WorldUnitGroup.Stack(stackId), new WorldUnitId("summon"), modifier.Id),
            new WorldStackDestroyedEvent(stackId, null, null),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.Remove(stackId)
            }));
    }

    [Test]
    public void SummonAfterExpirationTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition summon = SummonedUnitDefinition("summon", lifetime: 1);
        WorldSpellDefinition spell = SummonSpell(summon.Id, castingCost: WorldResourceSet.Empty);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [SummonSpellId])
        };

        WorldStack expired = SummonedStack(new WorldUnitId("stack0001Unit"), new WorldStackId("Stack0001"), player.Id, summon, new WorldPosition(1, 1), expirationTurn: 1);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(summon)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(expired)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .Build();

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldStackMovementRestoredEvent(expired.Id, 20),
            new WorldStackDestroyedEvent(expired.Id, Destroyer: null, DestroyerPlayerId: null),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);

        WorldStack summoned = SummonedStack(new WorldUnitId("Unit0001"), new WorldStackId("Stack0002"), player.Id, summon, new WorldPosition(1, 1), expirationTurn: 2);

        engine.CheckCommand(
            new WorldCastSpellCommand(SummonSpellId, new WorldSpellTarget(new WorldPosition(1, 1), Radius: 0)),
            [
                new WorldSpellCastEvent(player.Id, SummonSpellId, new WorldSpellTarget(new WorldPosition(1, 1), Radius: 0), WorldResourceSet.Empty, WorldSpellCastSource.Player),
                new WorldStackCreatedEvent(summoned, WorldResourceSet.Empty)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = ImmutableSortedDictionary<WorldStackId, WorldStack>.Empty.Add(summoned.Id, summoned),
                ScenarioState = WorldScenarioState.Empty with
                {
                    DestroyedStacks = [new ScenarioDestroyedStack(expired.Id, DestroyerPlayerId: null, SourceTemplateId: null)]
                }
            }));
    }

    [Test]
    public void TargetBagTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition summon = SummonedUnitDefinition("summon", lifetime: 1);
        WorldSpellDefinition spell = SummonSpell(summon.Id, castingCost: WorldResourceSet.Empty);

        ItemDefinition itemDefinition = TestHelpers.SimpleItemDefinition(new ItemDefinitionId("Item"), ItemCategory.Valuable);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [SummonSpellId])
        };

        WorldBag bag = new(
            new WorldBagId("Bag1"),
            new WorldPosition(1, 1),
            new WorldObjectVariant(0),
            new WorldInventory([new WorldItem(new WorldItemId("Item1"), itemDefinition.Id, Charges: null)]),
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(summon)
            .WithDefinition(spell)
            .WithDefinition(itemDefinition)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithBag(bag)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .Build();

        engine.CheckCommandError(
            new WorldCastSpellCommand(SummonSpellId, new WorldSpellTarget(new WorldPosition(1, 1), Radius: 0)),
            WorldCommandError.TargetOccupied);
    }

    [Test]
    public void TargetRodTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition summon = SummonedUnitDefinition("summon", lifetime: 1);
        WorldSpellDefinition spell = SummonSpell(summon.Id, castingCost: WorldResourceSet.Empty);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [SummonSpellId])
        };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(summon)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithRod(new WorldRod(new WorldRodId("Rod1"), player.Id, new WorldPosition(1, 1)))
            .WithSubrace(TestHelpers.NeutralSubrace)
            .Build();

        engine.CheckCommandError(
            new WorldCastSpellCommand(SummonSpellId, new WorldSpellTarget(new WorldPosition(1, 1), Radius: 0)),
            WorldCommandError.TargetOccupied);
    }

    [Test]
    public void ScenarioEventTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition summon = SummonedUnitDefinition("summon", lifetime: 1);
        WorldSpellDefinition spell = SummonSpell(summon.Id, castingCost: WorldResourceSet.Empty);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [SummonSpellId])
        };

        ScenarioLocationDefinition circle = new(new ScenarioLocationId("Circle"), new ScenarioTextId("Circle"), new WorldPosition(1, 1), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Undead],
            [WorldRace.Undead],
            [new ScenarioEventEnteringZoneCondition(circle.Id)],
            [new ScenarioEventPopupEffect(new ScenarioTextId("Something stirs in the circle."), ScenarioEventPopupAudience.Triggerer, null, IsPortraitLeft: true, IsPortraitTriggerer: false)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(summon)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithScenarioLocation(circle)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldStackId stackId = new("Stack0001");

        engine.CheckCommand(
            new WorldCastSpellCommand(SummonSpellId, new WorldSpellTarget(new WorldPosition(1, 1), Radius: 0)),
            [
                new WorldSpellCastEvent(player.Id, SummonSpellId, new WorldSpellTarget(new WorldPosition(1, 1), Radius: 0), WorldResourceSet.Empty, WorldSpellCastSource.Player),
                new WorldStackCreatedEvent(
                    SummonedStack(new WorldUnitId("Unit0001"), stackId, player.Id, summon, new WorldPosition(1, 1), expirationTurn: 2),
                    WorldResourceSet.Empty),
                new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackCreatedTrigger(stackId)),
                new WorldScenarioPopupEvent(scenarioEvent.Id, new ScenarioTextId("Something stirs in the circle."), PortraitId: null, PortraitName: null, IsPortraitLeft: true, player.Id, TriggererPosition: new WorldPosition(1, 1), [player.Id]),
                new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventStackCreatedTrigger(stackId))
            ]);

        Assert.That(engine.State.ScenarioState, Is.EqualTo(
            WorldScenarioState.Empty with
            {
                EventStates = ImmutableSortedDictionary<ScenarioEventId, bool>.Empty.Add(scenarioEvent.Id, false)
            }));
    }

    [Test]
    public void ScenarioCastOnOtherPlayerTurnTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition summon = SummonedUnitDefinition("summon", lifetime: 1);
        WorldSpellDefinition spell = SummonSpell(summon.Id, castingCost: WorldResourceSet.Empty);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Heretic, map);
        WorldPlayer enemy = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Undead, map);

        WorldSubrace enemySubrace = new(
            new WorldSubraceId("undead"),
            enemy.Id,
            WorldSubraceType.Undead,
            BannerVariant: new WorldObjectVariant(1));

        ScenarioLocationDefinition grave = new(new ScenarioLocationId("Grave"), new ScenarioTextId("Grave"), new WorldPosition(1, 1), 0);

        ScenarioEventDefinition scenarioEvent = new(
            new ScenarioEventId("Event1"),
            [WorldRace.Heretic],
            [WorldRace.Heretic],
            [new ScenarioEventFrequencyCondition(1)],
            [new ScenarioEventCastSpellMapEffect(spell.Id, grave.Id, enemy.Id)],
            Enabled: true,
            OccurOnce: true,
            Chance: 100,
            Order: 0);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(summon)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithDefinition(TestHelpers.SimpleLord(enemy.LordId, enemy.Race, LordCategory.Warrior))
            .WithPlayer(enemy)
            .WithSubrace(enemySubrace)
            .WithScenarioLocation(grave)
            .WithScenarioEvent(scenarioEvent)
            .Build();

        WorldStackId stackId = new("Stack0001");
        WorldStack summoned = SummonedStack(new WorldUnitId("Unit0001"), stackId, enemy.Id, summon, new WorldPosition(1, 1), expirationTurn: 2) with
        {
            SubraceId = enemySubrace.Id
        };

        engine.CheckCommand(new WorldStartGameCommand(), [
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 0, 25, 0)),
            new WorldScenarioEventTriggeringEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id)),
            new WorldSpellCastEvent(enemy.Id, spell.Id, new WorldSpellTarget(new WorldPosition(1, 1), Radius: 0), WorldResourceSet.Empty, WorldSpellCastSource.Scenario),
            new WorldStackCreatedEvent(summoned, WorldResourceSet.Empty),
            new WorldScenarioEventTriggeredEvent(scenarioEvent.Id, player.Id, new ScenarioEventTurnStartedTrigger(player.Id))
        ]);

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(player.Id),
            new WorldTurnStartingEvent(enemy.Id),
            new WorldStackMovementRestoredEvent(stackId, 20),
            new WorldTurnStartedEvent(enemy.Id),
            new WorldTurnResourcesGeneratedEvent(enemy.Id, new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);

        Assert.That(engine.State.Stacks, Is.EqualTo(
            ImmutableSortedDictionary<WorldStackId, WorldStack>.Empty.Add(summoned.Id, summoned)));

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(enemy.Id),
            new WorldRoundEndedEvent(),
            new WorldRoundStartedEvent(),
            new WorldTurnStartingEvent(player.Id),
            new WorldTurnStartedEvent(player.Id),
            new WorldTurnResourcesGeneratedEvent(player.Id, new WorldResourceSet(100, 0, 0, 0, 25, 0))
        ]);

        engine.CheckCommand(new WorldEndTurnCommand(), [
            new WorldTurnEndedEvent(player.Id),
            new WorldTurnStartingEvent(enemy.Id),
            new WorldStackMovementRestoredEvent(stackId, 20),
            new WorldStackDestroyedEvent(stackId, Destroyer: null, DestroyerPlayerId: null),
            new WorldTurnStartedEvent(enemy.Id),
            new WorldTurnResourcesGeneratedEvent(enemy.Id, new WorldResourceSet(100, 25, 0, 0, 0, 0))
        ]);

        Assert.That(engine.State.Stacks, Is.Empty);
    }
}
