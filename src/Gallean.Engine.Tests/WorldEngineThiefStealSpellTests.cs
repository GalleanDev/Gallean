namespace Gallean.Engine.Tests;

public sealed class WorldEngineThiefStealSpellTests
{
    private static WorldSpellDefinition Spell(string id)
    {
        return TestHelpers.SimpleSpellDefinition(new WorldSpellDefinitionId(id), new WorldFogSpellEffect()) with
        {
            CastingCost = new WorldResourceSet(10, 0, 0, 0, 0, 0),
            PurchaseCost = new WorldResourceSet(200, 0, 0, 0, 0, 0)
        };
    }

    private static WorldMageSite Site(IReadOnlyList<WorldSpellDefinitionId> spells)
    {
        return new WorldMageSite(
            new WorldSiteId("site"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            [.. spells],
            VisitorIds: [],
            AiPriority: 3);
    }

    private static WorldStack ThiefStack(WorldUnitId thiefId, WorldPlayerId ownerId, WorldPosition position, UnitDefinition thief)
    {
        return TestHelpers.SimpleStack(
            new WorldStackId("thief"),
            ownerId,
            position,
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(thiefId, thief), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);
    }

    [Test]
    public void CityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);
        WorldSpellDefinition spell = Spell("spell");

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [spell.Id])
        };

        WorldStack thiefStack = ThiefStack(new WorldUnitId("thief"), player1.Id, new WorldPosition(4, 3), thief);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player2.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier), GridPosition.FrontBottom, false)
                .Build());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(0, 0))
            .WithDefinition(thief)
            .WithDefinition(soldier)
            .WithDefinition(cityDefinition)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealSpell, 80))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithCity(city)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldThiefActionCommand(
                thiefStack.Id,
                new WorldThiefStealSpellAction(new WorldPosition(3, 3), spell.Id)),
            [
                new WorldThiefActionSucceededEvent(thiefStack.Id, WorldThiefActionType.StealSpell, player2.Id),
                new WorldSpellStolenEvent(thiefStack.Id, player2.Id, spell.Id),
                new WorldStackMovementConsumedEvent(thiefStack.Id, 10)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Players = before.Players.SetItem(player1.Id, player1 with
                {
                    Spellbook = new WorldPlayerSpellbook([], [spell.Id])
                }),
                Stacks = before.Stacks.SetItem(thiefStack.Id, thiefStack with { MovementPoints = 10 })
            }));
    }

    [Test]
    public void MageTowerTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        WorldSpellDefinition spell = Spell("spell");

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack thiefStack = ThiefStack(new WorldUnitId("thief"), player1.Id, new WorldPosition(5, 4), thief);
        WorldMageSite site = Site([spell.Id]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(0, 0))
            .WithDefinition(thief)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealMage, 60))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithStack(thiefStack)
            .WithSite(site)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldThiefActionCommand(
                thiefStack.Id,
                new WorldThiefStealSpellAction(new WorldPosition(4, 4), spell.Id)),
            [
                new WorldThiefActionSucceededEvent(thiefStack.Id, WorldThiefActionType.StealMage, null),
                new WorldSpellStolenEvent(thiefStack.Id, null, spell.Id),
                new WorldStackMovementConsumedEvent(thiefStack.Id, 10)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Players = before.Players.SetItem(player1.Id, player1 with
                {
                    Spellbook = new WorldPlayerSpellbook([], [spell.Id])
                }),
                Stacks = before.Stacks.SetItem(thiefStack.Id, thiefStack with { MovementPoints = 10 })
            }));
    }

    [Test]
    public void CityProtectionTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 3), WorldResourceSet.Empty);
        WorldSpellDefinition spell = Spell("spell");

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [spell.Id])
        };

        WorldStack thiefStack = ThiefStack(new WorldUnitId("thief"), player1.Id, new WorldPosition(4, 3), thief);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player2.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier), GridPosition.FrontBottom, false)
                .Build());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithRandom(new TestSequenceRandomProvider(50, 52))
            .WithDefinition(thief)
            .WithDefinition(soldier)
            .WithDefinition(cityDefinition)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealSpell, 80))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithCity(city)
            .Build();

        WorldState before = engine.State;

        engine.CheckCommand(
            new WorldThiefActionCommand(
                thiefStack.Id,
                new WorldThiefStealSpellAction(new WorldPosition(3, 3), spell.Id)),
            [
                new WorldThiefActionFailedEvent(thiefStack.Id, WorldThiefActionType.StealSpell, player2.Id),
                new WorldStackDestroyedEvent(thiefStack.Id, null, null)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            before with
            {
                Stacks = before.Stacks.Remove(thiefStack.Id),
                ScenarioState = before.ScenarioState with
                {
                    DestroyedStacks = before.ScenarioState.DestroyedStacks.Add(new ScenarioDestroyedStack(thiefStack.Id, DestroyerPlayerId: null, SourceTemplateId: null))
                }
            }));
    }

    [Test]
    public void SpellNotOwnedByVictimTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);
        WorldSpellDefinition spell = Spell("spell");

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map);

        WorldStack thiefStack = ThiefStack(new WorldUnitId("thief"), player1.Id, new WorldPosition(4, 3), thief);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player2.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier), GridPosition.FrontBottom, false)
                .Build());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(soldier)
            .WithDefinition(cityDefinition)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealSpell, 80))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithCity(city)
            .Build();

        engine.CheckCommandError(
            new WorldThiefActionCommand(
                thiefStack.Id,
                new WorldThiefStealSpellAction(new WorldPosition(3, 3), spell.Id)),
            WorldCommandError.SpellNotKnown);
    }

    [Test]
    public void SpellAlreadyKnownTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        WorldSpellDefinition spell = Spell("spell");

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [spell.Id])
        };

        WorldStack thiefStack = ThiefStack(new WorldUnitId("thief"), player1.Id, new WorldPosition(5, 4), thief);
        WorldMageSite site = Site([spell.Id]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealMage, 60))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithStack(thiefStack)
            .WithSite(site)
            .Build();

        engine.CheckCommandError(
            new WorldThiefActionCommand(
                thiefStack.Id,
                new WorldThiefStealSpellAction(new WorldPosition(4, 4), spell.Id)),
            WorldCommandError.SpellAlreadyKnown);
    }

    [Test]
    public void SpellNotStockedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        WorldSpellDefinition spell = Spell("spell");
        WorldSpellDefinition otherSpell = Spell("other");

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack thiefStack = ThiefStack(new WorldUnitId("thief"), player1.Id, new WorldPosition(5, 4), thief);
        WorldMageSite site = Site([otherSpell.Id]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(spell)
            .WithDefinition(otherSpell)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealMage, 60))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithStack(thiefStack)
            .WithSite(site)
            .Build();

        engine.CheckCommandError(
            new WorldThiefActionCommand(
                thiefStack.Id,
                new WorldThiefStealSpellAction(new WorldPosition(4, 4), spell.Id)),
            WorldCommandError.SpellNotAvailable);
    }

    [Test]
    public void OwnCityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);
        WorldSpellDefinition spell = Spell("spell");

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [spell.Id])
        };

        WorldStack thiefStack = ThiefStack(new WorldUnitId("thief"), player1.Id, new WorldPosition(4, 3), thief);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player1.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier), GridPosition.FrontBottom, false)
                .Build());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(soldier)
            .WithDefinition(cityDefinition)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealSpell, 80))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithStack(thiefStack)
            .WithCity(city)
            .Build();

        engine.CheckCommandError(
            new WorldThiefActionCommand(
                thiefStack.Id,
                new WorldThiefStealSpellAction(new WorldPosition(3, 3), spell.Id)),
            WorldCommandError.ThiefTargetInvalid);
    }

    [Test]
    public void NotAdjacentTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        WorldSpellDefinition spell = Spell("spell");

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack thiefStack = ThiefStack(new WorldUnitId("thief"), player1.Id, new WorldPosition(6, 6), thief);
        WorldMageSite site = Site([spell.Id]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealMage, 60))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Warrior))
            .WithPlayer(player1)
            .WithStack(thiefStack)
            .WithSite(site)
            .Build();

        engine.CheckCommandError(
            new WorldThiefActionCommand(
                thiefStack.Id,
                new WorldThiefStealSpellAction(new WorldPosition(4, 4), spell.Id)),
            WorldCommandError.ThiefTargetInvalid);
    }

    [Test]
    public void SummonSpellTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition thief = TestHelpers.SimpleThiefDefinition();
        UnitDefinition soldier = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 5);
        WorldCityDefinition cityDefinition = TestHelpers.SimpleCityDefinition(new WorldCityDefinitionId(WorldRace.Human, 1), WorldResourceSet.Empty);
        WorldSpellDefinition spell = Spell("summon") with
        {
            Effect = new WorldSummonStackSpellEffect(soldier.Id)
        };

        WorldPlayer player1 = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);
        WorldPlayer player2 = TestHelpers.SimplePlayer(new WorldPlayerId(2), WorldRace.Heretic, map) with
        {
            Spellbook = new WorldPlayerSpellbook([], [spell.Id])
        };

        WorldStack thiefStack = ThiefStack(new WorldUnitId("thief"), player1.Id, new WorldPosition(4, 3), thief);

        WorldCity city = TestHelpers.SimpleCity(
            new WorldFortificationId("city"),
            cityDefinition,
            player2.Id,
            new WorldPosition(3, 3),
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldier), GridPosition.FrontBottom, false)
                .Build());

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(thief)
            .WithDefinition(soldier)
            .WithDefinition(cityDefinition)
            .WithDefinition(spell)
            .WithDefinition(TestHelpers.SimpleThiefActionDefinition(WorldThiefActionType.StealSpell, 80))
            .WithDefinition(TestHelpers.SimpleLord(player1.LordId, player1.Race, LordCategory.Guildmaster))
            .WithPlayer(player1)
            .WithDefinition(TestHelpers.SimpleLord(player2.LordId, player2.Race, LordCategory.Warrior))
            .WithPlayer(player2)
            .WithStack(thiefStack)
            .WithCity(city)
            .Build();

        engine.CheckCommandError(
            new WorldThiefActionCommand(
                thiefStack.Id,
                new WorldThiefStealSpellAction(new WorldPosition(3, 3), spell.Id)),
            WorldCommandError.SpellNotAvailable);
    }
}
