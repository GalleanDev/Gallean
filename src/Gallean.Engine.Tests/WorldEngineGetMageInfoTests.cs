namespace Gallean.Engine.Tests;

public sealed class WorldEngineGetMageInfoTests
{
    private static UnitModifierDefinition DiscountModifier(string id, int percent)
    {
        return TestHelpers.SimpleModifier(id) with { PurchaseDiscountPercent = percent };
    }

    private static WorldSpellDefinition Spell(string id, WorldResourceSet purchaseCost)
    {
        return TestHelpers.SimpleSpellDefinition(new WorldSpellDefinitionId(id), new WorldFogSpellEffect()) with
        {
            CastingCost = new WorldResourceSet(10, 0, 0, 0, 0, 0),
            PurchaseCost = purchaseCost
        };
    }

    private static WorldStack StackWithModifiers(
        WorldUnitId leaderUnitId,
        WorldPlayerId ownerId,
        WorldPosition position,
        UnitDefinition leader,
        IReadOnlyList<UnitModifierDefinitionId> modifierIds)
    {
        WorldUnit leaderUnit = new(
            leaderUnitId,
            leader.Id,
            leader.MaxHitpoints,
            0,
            1,
            [.. modifierIds.Select(UnitModifier.Permanent)],
            Name: null,
            IsTypeLocked: false);

        return TestHelpers.SimpleStack(
            new WorldStackId("stack"),
            ownerId,
            position,
            new UnitGrid<WorldUnit>.Builder()
                .Place(leaderUnit, GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 6);
    }

    [Test]
    public void SpellsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 4);
        UnitModifierDefinition discount = DiscountModifier("discount", 10);
        WorldSpellDefinition fogSpell = Spell("fog", new WorldResourceSet(200, 0, 0, 0, 0, 0));
        WorldSpellDefinition knownSpell = Spell("known", new WorldResourceSet(100, 0, 0, 0, 0, 0));
        WorldSpellDefinition costlySpell = Spell("costly", new WorldResourceSet(1000, 0, 0, 0, 0, 0));

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(300, 0, 0, 0, 0, 0),
            Spellbook = new WorldPlayerSpellbook([], [knownSpell.Id])
        };

        WorldStack stack = StackWithModifiers(new WorldUnitId("leaderUnit"), player.Id, new WorldPosition(4, 4), leaderDefinition, [discount.Id]);

        WorldMageSite site = new(
            new WorldSiteId("site"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            [fogSpell.Id, knownSpell.Id, costlySpell.Id],
            VisitorIds: [],
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(discount)
            .WithDefinition(fogSpell)
            .WithDefinition(knownSpell)
            .WithDefinition(costlySpell)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithSite(site)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());

        WorldGetMageInfoCommand.Result result = engine.CheckCommand(
            new WorldGetMageInfoCommand(stack.Id, site.Id),
            []);

        Assert.That(result, Is.EqualTo(new WorldGetMageInfoCommand.Result(
        [
            new WorldGetMageInfoCommand.Spell(
                fogSpell.Id,
                new WorldResourceSet(180, 0, 0, 0, 0, 0),
                Error: null),
            new WorldGetMageInfoCommand.Spell(
                knownSpell.Id,
                new WorldResourceSet(90, 0, 0, 0, 0, 0),
                WorldCommandError.SpellAlreadyKnown),
            new WorldGetMageInfoCommand.Spell(
                costlySpell.Id,
                new WorldResourceSet(900, 0, 0, 0, 0, 0),
                WorldCommandError.InsufficientResources)
        ])));
    }
}
