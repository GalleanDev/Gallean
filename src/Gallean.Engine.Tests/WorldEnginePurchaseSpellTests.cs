namespace Gallean.Engine.Tests;

public sealed class WorldEnginePurchaseSpellTests
{
    private static WorldStack Stack(WorldUnitId leaderId, WorldPlayerId ownerId, WorldPosition position, UnitDefinition leader)
    {
        return TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            ownerId,
            position,
            new UnitGrid<WorldUnit>.Builder()
                .Place(TestHelpers.SimpleUnit(leaderId, leader), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);
    }

    private static WorldSpellDefinition Spell(string id, WorldResourceSet purchaseCost)
    {
        return TestHelpers.SimpleSpellDefinition(new WorldSpellDefinitionId(id), new WorldFogSpellEffect()) with
        {
            CastingCost = new WorldResourceSet(10, 0, 0, 0, 0, 0),
            PurchaseCost = purchaseCost
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

    [Test]
    public void SuccessTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);
        WorldSpellDefinition spell = Spell("spell", new WorldResourceSet(200, 0, 0, 0, 0, 0));

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 4);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(300, 0, 0, 300, 0, 0)
        };

        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(4, 4), leaderDefinition);
        WorldMageSite site = Site([spell.Id]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(spell)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithResearchableSpell(new WorldSpellResearchCost(WorldRace.Undead, spell.Id, new WorldResourceSet(0, 0, 0, 200, 0, 0)))
            .WithStack(stack)
            .WithSite(site)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldPurchaseSpellCommand(stack.Id, site.Id, spell.Id),
            [
                new WorldSpellPurchasedEvent(
                    site.Id,
                    stack.Id,
                    player.Id,
                    spell.Id,
                    new WorldResourceSet(200, 0, 0, 0, 0, 0))
            ]
        );

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = new WorldResourceSet(100, 0, 0, 300, 0, 0),
                    Spellbook = player.Spellbook with { Acquired = [spell.Id] }
                })
            }));
    }

    [Test]
    public void OutsideRacePoolTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);
        WorldSpellDefinition spell = Spell("spell", new WorldResourceSet(200, 0, 0, 0, 0, 0));

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 4);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(300, 0, 0, 0, 0, 0)
        };

        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(4, 4), leaderDefinition);
        WorldMageSite site = Site([spell.Id]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(spell)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithSite(site)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommand(
            new WorldPurchaseSpellCommand(stack.Id, site.Id, spell.Id),
            [
                new WorldSpellPurchasedEvent(
                    site.Id,
                    stack.Id,
                    player.Id,
                    spell.Id,
                    new WorldResourceSet(200, 0, 0, 0, 0, 0))
            ]
        );

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = new WorldResourceSet(100, 0, 0, 0, 0, 0),
                    Spellbook = player.Spellbook with { Acquired = [spell.Id] }
                })
            }));
    }

    [Test]
    public void SiteNotFoundTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);
        WorldSpellDefinition spell = Spell("spell", new WorldResourceSet(200, 0, 0, 0, 0, 0));

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 4);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(300, 0, 0, 0, 0, 0)
        };

        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(4, 4), leaderDefinition);
        WorldMageSite site = Site([spell.Id]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(spell)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithSite(site)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldPurchaseSpellCommand(stack.Id, new WorldSiteId("missing"), spell.Id),
            WorldCommandError.SiteNotFound
        );
    }

    [Test]
    public void NotAtSiteTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);
        WorldSpellDefinition spell = Spell("spell", new WorldResourceSet(200, 0, 0, 0, 0, 0));

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 4);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(300, 0, 0, 0, 0, 0)
        };

        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(6, 6), leaderDefinition);
        WorldMageSite site = Site([spell.Id]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(spell)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithSite(site)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldPurchaseSpellCommand(stack.Id, site.Id, spell.Id),
            WorldCommandError.StackNotAtSite
        );
    }

    [Test]
    public void SpellNotAvailableTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);
        WorldSpellDefinition spell = Spell("spell", new WorldResourceSet(200, 0, 0, 0, 0, 0));
        WorldSpellDefinition unlistedSpell = Spell("unlisted", new WorldResourceSet(200, 0, 0, 0, 0, 0));

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 4);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(300, 0, 0, 0, 0, 0)
        };

        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(4, 4), leaderDefinition);
        WorldMageSite site = Site([spell.Id]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(spell)
            .WithDefinition(unlistedSpell)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithSite(site)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldPurchaseSpellCommand(stack.Id, site.Id, unlistedSpell.Id),
            WorldCommandError.SpellNotAvailable
        );
    }

    [Test]
    public void AlreadyKnownTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);
        WorldSpellDefinition spell = Spell("spell", new WorldResourceSet(200, 0, 0, 0, 0, 0));

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 4);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(300, 0, 0, 0, 0, 0),
            Spellbook = new WorldPlayerSpellbook([], [spell.Id])
        };

        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(4, 4), leaderDefinition);
        WorldMageSite site = Site([spell.Id]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(spell)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithSite(site)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldPurchaseSpellCommand(stack.Id, site.Id, spell.Id),
            WorldCommandError.SpellAlreadyKnown
        );
    }

    [Test]
    public void InsufficientResourcesTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);
        WorldSpellDefinition spell = Spell("spell", new WorldResourceSet(200, 0, 0, 0, 0, 0));

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 4);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(50, 0, 0, 0, 0, 0)
        };

        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(4, 4), leaderDefinition);
        WorldMageSite site = Site([spell.Id]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(spell)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithSite(site)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        engine.CheckCommandError(
            new WorldPurchaseSpellCommand(stack.Id, site.Id, spell.Id),
            WorldCommandError.InsufficientResources
        );
    }
}
