using Gallean.Common;
using Gallean.Engine;
using Gallean.Engine.Tests;
using Gallean.Tests.Common;

namespace Gallean.Client.Tests;

public sealed class MageTabViewModelTests
{
    private static WorldStack Stack(WorldUnitId leaderId, WorldPlayerId ownerId, WorldPosition position, UnitDefinition leader)
    {
        return TestHelpers.SimpleStack(
            new WorldStackId("Virgile"),
            ownerId,
            position,
            new UnitGrid<WorldUnit>.Builder()
                .Place(new WorldUnit(leaderId, leader.Id, leader.MaxHitpoints, 0, leader.Level, [], null, false), GridPosition.FrontBottom, false)
                .Build(),
            movementPoints: 20);
    }

    private static WorldSpellDefinition Spell(string id, int level, WorldResourceSet purchaseCost)
    {
        return new WorldSpellDefinition(
            new WorldSpellDefinitionId(id),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            new GlobalTextId("effect"),
            WorldRace.Human,
            level,
            new WorldResourceSet(10, 0, 0, 0, 0, 0),
            purchaseCost,
            AreaRadius: 0,
            Effect: new WorldFogSpellEffect(),
            AiType: WorldAiSpellType.All);
    }

    private static WorldMageSite Site(IReadOnlyList<WorldSpellDefinitionId> spells)
    {
        return new WorldMageSite(
            new WorldSiteId("tower"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Vogren's Tower"),
            GreetingTextId: new ScenarioTextId("I think my magical library can be of some help to you."),
            [.. spells],
            VisitorIds: [],
            AiPriority: 3);
    }

    private static IReadOnlyList<string> FormatSpells(IReadOnlyList<MageSpellItemViewModel> spells)
    {
        List<string> rows = [];

        foreach (MageSpellItemViewModel spell in spells)
        {
            rows.Add($"{spell.SpellId.Value} | {spell.Name} | {spell.Description} | {spell.LevelText} | Cost: {FormatPrice(Assert.NotNull(spell.Status.Price))} | {FormatAvailability(spell.Status)} | {FormatSelection(spell.IsSelected)}");
        }

        return rows;
    }

    private static string FormatOffer(SiteOfferViewModel offer)
    {
        return $"{offer.Title} | {offer.Subtitle.Text} | {offer.Description} | {FormatPrice(Assert.NotNull(offer.Status.Price))} | {offer.ActionText} | {FormatAvailability(offer.Status)} | {FormatEnabled(offer.ActCommand.CanExecute(null))}";
    }

    private static string FormatEnabled(bool isEnabled)
    {
        if (isEnabled)
        {
            return "enabled";
        }
        else
        {
            return "disabled";
        }
    }

    private static string FormatPrice(OfferPriceViewModel price)
    {
        List<string> items = [];

        foreach (OfferPriceItemViewModel item in price.Items)
        {
            if (item.IsShort)
            {
                items.Add($"{item.Resource.Value} {item.Resource.Name.ToLowerInvariant()} (short)");
            }
            else
            {
                items.Add($"{item.Resource.Value} {item.Resource.Name.ToLowerInvariant()}");
            }
        }

        return string.Join(", ", items);
    }

    private static string FormatAvailability(OfferStatusViewModel status)
    {
        if (status.IsAvailable)
        {
            return "available";
        }
        else if (status.UnavailableReason is null)
        {
            return "unavailable";
        }
        else
        {
            return $"unavailable: {status.UnavailableReason}";
        }
    }

    private static string FormatSelection(bool isSelected)
    {
        if (isSelected)
        {
            return "selected";
        }
        else
        {
            return "not selected";
        }
    }

    private static MageTabViewModel BuildScreen(GameSession session, IEngineContext engine, WorldSiteId siteId, WorldStackId stackId)
    {
        GameModel gameModel = new(session, TestGameTextProvider.Instance, NullLog.Instance, SynchronousTaskScheduler.Instance);
        GameViewModel gameViewModel = new TestGameViewModelBuilder().Build();
        ManagementScreenViewModel management = new(gameModel, gameViewModel, ScreenMusic.Keep, null, owner => [new MageTabViewModel(gameModel, gameViewModel, owner, siteId, stackId)]);

        return Assert.IsOfType<MageTabViewModel>(management.Tabs[0]);
    }

    [Test]
    public async Task LearnTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(300, 0, 0, 0, 0, 0)
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 4);
        WorldSpellDefinition spellDefinition = Spell("spell", level: 2, new WorldResourceSet(200, 0, 0, 0, 0, 0));

        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(4, 4), leaderDefinition);
        WorldMageSite site = Site([spellDefinition.Id]);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Human, new GlobalTextId("Empire"), WorldMapTerrain.Human))
            .WithDefinition(leaderDefinition)
            .WithDefinition(spellDefinition)
            .WithStack(stack)
            .WithSite(site)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        MageTabViewModel screen = BuildScreen(session, engine, site.Id, stack.Id);

        Assert.That(FormatSpells(screen.Spells), Is.EqualTo([
            "spell | name | desc | Level 2 | Cost: 200 gold | available | not selected"
        ]));

        Assert.That(screen.Offer, Is.Null);

        screen.Spells[0].SelectCommand.Execute(null);

        Assert.That(FormatOffer(Assert.NotNull(screen.Offer)), Is.EqualTo("name | Level 2 | desc | 200 gold | Learn | available | enabled"));
        Assert.That(FormatSpells(screen.Spells), Is.EqualTo([
            "spell | name | desc | Level 2 | Cost: 200 gold | available | selected"
        ]));

        await Assert.NotNull(screen.Offer).ActCommand.ExecuteAsync(null);

        Assert.That(session.World.State.Players[player.Id].Spellbook.Acquired, Is.EqualTo([spellDefinition.Id]));
        Assert.That(session.World.State.Players[player.Id].Resources, Is.EqualTo(new WorldResourceSet(100, 0, 0, 0, 0, 0)));
        Assert.That(screen.Offer, Is.Null);
        Assert.That(FormatSpells(screen.Spells), Is.Empty);
        Assert.That(screen.Game.Alert.Current, Is.EqualTo(new Alert(AlertType.Info, IconMoniker.Spellbook, "Spell learned", "name")));
    }

    [Test]
    public void InsufficientFundsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(50, 0, 0, 0, 0, 0)
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 4);
        WorldSpellDefinition spellDefinition = Spell("spell", level: 2, new WorldResourceSet(200, 0, 0, 0, 0, 0));

        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(4, 4), leaderDefinition);
        WorldMageSite site = Site([spellDefinition.Id]);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Human, new GlobalTextId("Empire"), WorldMapTerrain.Human))
            .WithDefinition(leaderDefinition)
            .WithDefinition(spellDefinition)
            .WithStack(stack)
            .WithSite(site)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        MageTabViewModel screen = BuildScreen(session, engine, site.Id, stack.Id);

        Assert.That(FormatSpells(screen.Spells), Is.EqualTo([
            "spell | name | desc | Level 2 | Cost: 200 gold (short) | unavailable | not selected"
        ]));

        screen.Spells[0].SelectCommand.Execute(null);

        Assert.That(FormatOffer(Assert.NotNull(screen.Offer)), Is.EqualTo("name | Level 2 | desc | 200 gold (short) | Learn | unavailable: Not enough resources | disabled"));
    }

    [Test]
    public void DiscountedCostTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map) with
        {
            Resources = new WorldResourceSet(300, 0, 0, 0, 0, 0)
        };

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 4);
        UnitModifierDefinition discount = new(
            new UnitModifierDefinitionId("discount"),
            DescriptionTextId: new GlobalTextId("description"),
            MoveAbility: UnitMoveAbility.None,
            MaxMovement: default,
            Leadership: default,
            VisionRadius: default,
            Hitpoints: default,
            RegenerationPercent: default,
            Armor: default,
            Initiative: default,
            Damage: default,
            Accuracy: default,
            DrainPercent: 0,
            FastRetreat: false,
            PurchaseDiscountPercent: 10,
            LeaderAbilities: LeaderAbility.None,
            Wards: []);

        WorldSpellDefinition spellDefinition = Spell("spell", level: 2, new WorldResourceSet(200, 0, 0, 0, 0, 0));

        WorldStack stack = TestHelpers.SimpleStack(
            new WorldStackId("Virgile"),
            player.Id,
            new WorldPosition(4, 4),
            new UnitGrid<WorldUnit>.Builder()
                .Place(
                    new WorldUnit(new WorldUnitId("leader"), leaderDefinition.Id, leaderDefinition.MaxHitpoints, 0, leaderDefinition.Level, [UnitModifier.Permanent(discount.Id)], null, false),
                    GridPosition.FrontBottom,
                    false)
                .Build(),
            movementPoints: 20);

        WorldMageSite site = Site([spellDefinition.Id]);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Human, new GlobalTextId("Empire"), WorldMapTerrain.Human))
            .WithDefinition(leaderDefinition)
            .WithDefinition(discount)
            .WithDefinition(spellDefinition)
            .WithStack(stack)
            .WithSite(site)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        MageTabViewModel screen = BuildScreen(session, engine, site.Id, stack.Id);

        Assert.That(FormatSpells(screen.Spells), Is.EqualTo([
            "spell | name | desc | Level 2 | Cost: 180 gold | available | not selected"
        ]));

        screen.Spells[0].SelectCommand.Execute(null);

        Assert.That(FormatOffer(Assert.NotNull(screen.Offer)), Is.EqualTo("name | Level 2 | desc | 180 gold | Learn | available | enabled"));
    }

    [Test]
    public void DetailsTest()
    {
        WorldMap map = TestHelpers.SimpleMap(8, 8);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Human, map);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 20, leadership: 4);
        WorldSpellDefinition spellDefinition = Spell("spell", level: 2, new WorldResourceSet(200, 0, 0, 0, 0, 0));

        WorldStack stack = Stack(new WorldUnitId("leader"), player.Id, new WorldPosition(4, 4), leaderDefinition);
        WorldMageSite site = Site([spellDefinition.Id]);

        (GameSession session, IEngineContext engine) = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Human, new GlobalTextId("Empire"), WorldMapTerrain.Human))
            .WithDefinition(leaderDefinition)
            .WithDefinition(spellDefinition)
            .WithStack(stack)
            .WithSite(site)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .BuildSession();

        session.World.DispatchCommand(new WorldStartGameCommand());

        MageTabViewModel screen = BuildScreen(session, engine, site.Id, stack.Id);

        Assert.That(screen.Spells[0].Details, Is.EqualTo(new SpellDetailsViewModel(
            new IconViewModel.Spell(new AssetSpriteKey.SpellIcon(spellDefinition.Id), IconViewModel.Spell.BackgroundType.Light),
            "name",
            "Level 2",
            "desc",
            "effect")));
    }
}
