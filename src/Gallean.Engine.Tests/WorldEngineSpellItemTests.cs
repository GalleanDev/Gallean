using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine.Tests;

public sealed class WorldEngineSpellItemTests
{
    private static readonly WorldSpellDefinitionId HealSpellId = new("heal");

    private static WorldSpellDefinition HealSpell(int? areaRadius)
    {
        return TestHelpers.SimpleSpellDefinition(HealSpellId, new WorldHealSpellEffect(30)) with
        {
            CastingCost = new WorldResourceSet(0, 0, 0, 500, 0, 0),
            AreaRadius = areaRadius
        };
    }

    private static ItemDefinition SpellItem(string id, ItemCategory category, WorldResourceSet castingCost)
    {
        return TestHelpers.SimpleItemDefinition(new ItemDefinitionId(id), category) with
        {
            Effect = new SpellItemEffect(HealSpellId, castingCost)
        };
    }

    private static UnitDefinition ScrollLeaderDefinition()
    {
        return TestHelpers.SimpleLeaderDefinition(
                TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
                movementPoints: 6, leadership: 3) with
            {
                LeaderAbilities = LeaderAbility.WandScrollUse
            };
    }

    private static WorldStack Stack(
        WorldUnitId leaderId,
        WorldStackId id,
        WorldPlayerId ownerId,
        UnitDefinition leaderDefinition,
        WorldPosition position,
        int leaderHitpoints,
        IEnumerable<WorldItem> inventory)
    {
        return TestHelpers.SimpleStack(
                id,
                ownerId,
                position,
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(leaderId, leaderDefinition) with { Hitpoints = leaderHitpoints }, GridPosition.BackBottom, false)
                    .Build(),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([.. inventory])
            };
    }

    [Test]
    public void ScrollTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leaderDefinition = ScrollLeaderDefinition();
        WorldSpellDefinition spell = HealSpell(areaRadius: 0);
        ItemDefinition scroll = SpellItem("scroll1", ItemCategory.Scroll, WorldResourceSet.Empty);
        WorldItem scrollItem = new(new WorldItemId("Scroll"), scroll.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = Stack(
            new WorldUnitId("stack1Leader"),
            new WorldStackId("Stack1"),
            player.Id,
            leaderDefinition,
            new WorldPosition(0, 0),
            leaderHitpoints: 40,
            [scrollItem]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(spell)
            .WithDefinition(scroll)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(
            new WorldUseItemCommand(
                stack.Id,
                scrollItem.Id,
                new WorldItemTarget.Map(new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0))),
            [
                new WorldItemUsedEvent(stack.Id, scrollItem, RemainingCharges: null, Consumed: true),
                new WorldSpellCastEvent(
                    player.Id,
                    HealSpellId,
                    new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Item),
                new WorldUnitHealedEvent(
                    new WorldUnitGroup.Stack(stack.Id),
                    new WorldUnitId("stack1Leader"),
                    70,
                    WorldResourceSet.Empty)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Inventory = WorldInventory.Empty,
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("stack1Leader"), leaderDefinition) with { Hitpoints = 70 }, GridPosition.BackBottom, false)
                        .Build()
                })
            }));
    }

    [Test]
    public void WandTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leaderDefinition = ScrollLeaderDefinition();
        WorldSpellDefinition spell = HealSpell(areaRadius: 0);
        ItemDefinition wand = SpellItem("wand1", ItemCategory.Wand, new WorldResourceSet(0, 0, 0, 100, 0, 0));
        WorldItem wandItem = new(new WorldItemId("Wand"), wand.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(0, 0, 0, 250, 0, 0)
        };

        WorldStack stack = Stack(
            new WorldUnitId("stack1Leader"),
            new WorldStackId("Stack1"),
            player.Id,
            leaderDefinition,
            new WorldPosition(0, 0),
            leaderHitpoints: 40,
            [wandItem]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(spell)
            .WithDefinition(wand)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(
            new WorldUseItemCommand(
                stack.Id,
                wandItem.Id,
                new WorldItemTarget.Map(new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0))),
            [
                new WorldItemUsedEvent(stack.Id, wandItem, RemainingCharges: null, Consumed: false),
                new WorldSpellCastEvent(
                    player.Id,
                    HealSpellId,
                    new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0),
                    new WorldResourceSet(0, 0, 0, 100, 0, 0),
                    WorldSpellCastSource.Item),
                new WorldUnitHealedEvent(
                    new WorldUnitGroup.Stack(stack.Id),
                    new WorldUnitId("stack1Leader"),
                    70,
                    WorldResourceSet.Empty)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = new WorldResourceSet(0, 0, 0, 150, 0, 0),
                    TurnActions = new WorldPlayerTurnActions
                    {
                        ItemUses = ImmutableSortedDictionary<WorldItemId, int>.Empty.Add(wandItem.Id, 1)
                    }
                }),
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("stack1Leader"), leaderDefinition) with { Hitpoints = 70 }, GridPosition.BackBottom, false)
                        .Build()
                })
            }));
    }

    [Test]
    public void GlobalSpellTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leaderDefinition = ScrollLeaderDefinition();
        WorldSpellDefinition spell = HealSpell(areaRadius: null);
        ItemDefinition scroll = SpellItem("scroll1", ItemCategory.Scroll, WorldResourceSet.Empty);
        WorldItem scrollItem = new(new WorldItemId("Scroll"), scroll.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = Stack(
            new WorldUnitId("stack1Leader"),
            new WorldStackId("Stack1"),
            player.Id,
            leaderDefinition,
            new WorldPosition(0, 0),
            leaderHitpoints: 40,
            [scrollItem]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(spell)
            .WithDefinition(scroll)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(
            new WorldUseItemCommand(stack.Id, scrollItem.Id, Target: null),
            [
                new WorldItemUsedEvent(stack.Id, scrollItem, RemainingCharges: null, Consumed: true),
                new WorldSpellCastEvent(
                    player.Id,
                    HealSpellId,
                    Target: null,
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Item),
                new WorldUnitHealedEvent(
                    new WorldUnitGroup.Stack(stack.Id),
                    new WorldUnitId("stack1Leader"),
                    70,
                    WorldResourceSet.Empty)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Inventory = WorldInventory.Empty,
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("stack1Leader"), leaderDefinition) with { Hitpoints = 70 }, GridPosition.BackBottom, false)
                        .Build()
                })
            }));
    }

    [Test]
    public void UnknownSpellCastTwiceTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leaderDefinition = ScrollLeaderDefinition();
        WorldSpellDefinition spell = HealSpell(areaRadius: 0);
        ItemDefinition scroll = SpellItem("scroll1", ItemCategory.Scroll, WorldResourceSet.Empty);
        WorldItem scrollItem = new(new WorldItemId("Scroll"), scroll.Id, Charges: null);
        WorldItem secondScrollItem = new(new WorldItemId("SecondScroll"), scroll.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = Stack(
            new WorldUnitId("stack1Leader"),
            new WorldStackId("Stack1"),
            player.Id,
            leaderDefinition,
            new WorldPosition(0, 0),
            leaderHitpoints: 10,
            [scrollItem, secondScrollItem]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(spell)
            .WithDefinition(scroll)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommand(
            new WorldUseItemCommand(
                stack.Id,
                scrollItem.Id,
                new WorldItemTarget.Map(new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0))),
            [
                new WorldItemUsedEvent(stack.Id, scrollItem, RemainingCharges: null, Consumed: true),
                new WorldSpellCastEvent(
                    player.Id,
                    HealSpellId,
                    new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Item),
                new WorldUnitHealedEvent(
                    new WorldUnitGroup.Stack(stack.Id),
                    new WorldUnitId("stack1Leader"),
                    40,
                    WorldResourceSet.Empty)
            ]);

        engine.CheckCommand(
            new WorldUseItemCommand(
                stack.Id,
                secondScrollItem.Id,
                new WorldItemTarget.Map(new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0))),
            [
                new WorldItemUsedEvent(stack.Id, secondScrollItem, RemainingCharges: null, Consumed: true),
                new WorldSpellCastEvent(
                    player.Id,
                    HealSpellId,
                    new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0),
                    WorldResourceSet.Empty,
                    WorldSpellCastSource.Item),
                new WorldUnitHealedEvent(
                    new WorldUnitGroup.Stack(stack.Id),
                    new WorldUnitId("stack1Leader"),
                    70,
                    WorldResourceSet.Empty)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Inventory = WorldInventory.Empty,
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("stack1Leader"), leaderDefinition) with { Hitpoints = 70 }, GridPosition.BackBottom, false)
                        .Build()
                })
            }));
    }

    [Test]
    public void MissingAbilityTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leaderDefinition = TestHelpers.SimpleLeaderDefinition(
            TestHelpers.SimpleUnitDefinition("leader", maxHitpoints: 100, initiative: 50, damage: 10),
            movementPoints: 6, leadership: 3);
        WorldSpellDefinition spell = HealSpell(areaRadius: 0);
        ItemDefinition scroll = SpellItem("scroll1", ItemCategory.Scroll, WorldResourceSet.Empty);
        WorldItem scrollItem = new(new WorldItemId("Scroll"), scroll.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = Stack(
            new WorldUnitId("stack1Leader"),
            new WorldStackId("Stack1"),
            player.Id,
            leaderDefinition,
            new WorldPosition(0, 0),
            leaderHitpoints: 40,
            [scrollItem]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(spell)
            .WithDefinition(scroll)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldUseItemCommand(
                stack.Id,
                scrollItem.Id,
                new WorldItemTarget.Map(new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0))),
            WorldCommandError.MissingLeaderAbility);
    }

    [Test]
    public void DeadLeaderTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leaderDefinition = ScrollLeaderDefinition();
        UnitDefinition soldierDefinition = TestHelpers.SimpleUnitDefinition("soldier", maxHitpoints: 100, initiative: 40, damage: 10);
        WorldSpellDefinition spell = HealSpell(areaRadius: 0);
        ItemDefinition scroll = SpellItem("scroll1", ItemCategory.Scroll, WorldResourceSet.Empty);
        WorldItem scrollItem = new(new WorldItemId("Scroll"), scroll.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = TestHelpers.SimpleStack(
                new WorldStackId("Stack1"),
                player.Id,
                new WorldPosition(0, 0),
                new UnitGrid<WorldUnit>.Builder()
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("leader"), leaderDefinition) with { Hitpoints = 0 }, GridPosition.BackBottom, false)
                    .Place(TestHelpers.SimpleUnit(new WorldUnitId("soldier"), soldierDefinition) with { Hitpoints = 60 }, GridPosition.FrontBottom, false)
                    .Build(),
                movementPoints: 6) with
            {
                Inventory = new WorldInventory([scrollItem])
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(soldierDefinition)
            .WithDefinition(spell)
            .WithDefinition(scroll)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldUseItemCommand(
                stack.Id,
                scrollItem.Id,
                new WorldItemTarget.Map(new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0))),
            WorldCommandError.UnitDead);
    }

    [Test]
    public void InsufficientResourcesTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leaderDefinition = ScrollLeaderDefinition();
        WorldSpellDefinition spell = HealSpell(areaRadius: 0);
        ItemDefinition wand = SpellItem("wand1", ItemCategory.Wand, new WorldResourceSet(0, 0, 0, 100, 0, 0));
        WorldItem wandItem = new(new WorldItemId("Wand"), wand.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(0, 0, 0, 50, 0, 0)
        };

        WorldStack stack = Stack(
            new WorldUnitId("stack1Leader"),
            new WorldStackId("Stack1"),
            player.Id,
            leaderDefinition,
            new WorldPosition(0, 0),
            leaderHitpoints: 40,
            [wandItem]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(spell)
            .WithDefinition(wand)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldUseItemCommand(
                stack.Id,
                wandItem.Id,
                new WorldItemTarget.Map(new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0))),
            WorldCommandError.InsufficientResources);
    }

    [Test]
    public void UnitSlotTargetTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leaderDefinition = ScrollLeaderDefinition();
        WorldSpellDefinition spell = HealSpell(areaRadius: 0);
        ItemDefinition scroll = SpellItem("scroll1", ItemCategory.Scroll, WorldResourceSet.Empty);
        WorldItem scrollItem = new(new WorldItemId("Scroll"), scroll.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = Stack(
            new WorldUnitId("stack1Leader"),
            new WorldStackId("Stack1"),
            player.Id,
            leaderDefinition,
            new WorldPosition(0, 0),
            leaderHitpoints: 40,
            [scrollItem]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(spell)
            .WithDefinition(scroll)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldUseItemCommand(stack.Id, scrollItem.Id, new WorldItemTarget.Unit(new WorldUnitId("stack1Leader"))),
            WorldCommandError.ItemTargetInvalid);
    }

    [Test]
    public void MissingTargetTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leaderDefinition = ScrollLeaderDefinition();
        WorldSpellDefinition spell = HealSpell(areaRadius: 0);
        ItemDefinition scroll = SpellItem("scroll1", ItemCategory.Scroll, WorldResourceSet.Empty);
        WorldItem scrollItem = new(new WorldItemId("Scroll"), scroll.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map);

        WorldStack stack = Stack(
            new WorldUnitId("stack1Leader"),
            new WorldStackId("Stack1"),
            player.Id,
            leaderDefinition,
            new WorldPosition(0, 0),
            leaderHitpoints: 40,
            [scrollItem]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(spell)
            .WithDefinition(scroll)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        engine.CheckCommandError(
            new WorldUseItemCommand(stack.Id, scrollItem.Id, Target: null),
            WorldCommandError.SpellTargetInvalid);
    }

    [Test]
    public void WandOncePerTurnTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leaderDefinition = ScrollLeaderDefinition();
        WorldSpellDefinition spell = HealSpell(areaRadius: 0);
        ItemDefinition wand = SpellItem("wand1", ItemCategory.Wand, new WorldResourceSet(0, 0, 0, 100, 0, 0));
        WorldItem wandItem = new(new WorldItemId("Wand"), wand.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(0, 0, 0, 500, 0, 0)
        };

        WorldStack stack = Stack(
            new WorldUnitId("stack1Leader"),
            new WorldStackId("Stack1"),
            player.Id,
            leaderDefinition,
            new WorldPosition(0, 0),
            leaderHitpoints: 10,
            [wandItem]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(spell)
            .WithDefinition(wand)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        WorldUseItemCommand command = new(
            stack.Id,
            wandItem.Id,
            new WorldItemTarget.Map(new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)));

        engine.CheckCommand(
            command,
            [
                new WorldItemUsedEvent(stack.Id, wandItem, RemainingCharges: null, Consumed: false),
                new WorldSpellCastEvent(
                    player.Id,
                    HealSpellId,
                    new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0),
                    new WorldResourceSet(0, 0, 0, 100, 0, 0),
                    WorldSpellCastSource.Item),
                new WorldUnitHealedEvent(
                    new WorldUnitGroup.Stack(stack.Id),
                    new WorldUnitId("stack1Leader"),
                    40,
                    WorldResourceSet.Empty)
            ]);

        engine.CheckCommandError(command, WorldCommandError.ItemAlreadyUsedThisTurn);
    }

    [Test]
    public void MageWandTwicePerTurnTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leaderDefinition = ScrollLeaderDefinition();
        WorldSpellDefinition spell = HealSpell(areaRadius: 0);
        ItemDefinition wand = SpellItem("wand1", ItemCategory.Wand, new WorldResourceSet(0, 0, 0, 100, 0, 0));
        WorldItem wandItem = new(new WorldItemId("Wand"), wand.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(0, 0, 0, 500, 0, 0)
        };

        WorldStack stack = Stack(
            new WorldUnitId("stack1Leader"),
            new WorldStackId("Stack1"),
            player.Id,
            leaderDefinition,
            new WorldPosition(0, 0),
            leaderHitpoints: 10,
            [wandItem]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(spell)
            .WithDefinition(wand)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Mage))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        WorldUseItemCommand command = new(
            stack.Id,
            wandItem.Id,
            new WorldItemTarget.Map(new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)));

        engine.CheckCommand(
            command,
            [
                new WorldItemUsedEvent(stack.Id, wandItem, RemainingCharges: null, Consumed: false),
                new WorldSpellCastEvent(
                    player.Id,
                    HealSpellId,
                    new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0),
                    new WorldResourceSet(0, 0, 0, 100, 0, 0),
                    WorldSpellCastSource.Item),
                new WorldUnitHealedEvent(
                    new WorldUnitGroup.Stack(stack.Id),
                    new WorldUnitId("stack1Leader"),
                    40,
                    WorldResourceSet.Empty)
            ]);

        engine.CheckCommand(
            command,
            [
                new WorldItemUsedEvent(stack.Id, wandItem, RemainingCharges: null, Consumed: false),
                new WorldSpellCastEvent(
                    player.Id,
                    HealSpellId,
                    new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0),
                    new WorldResourceSet(0, 0, 0, 100, 0, 0),
                    WorldSpellCastSource.Item),
                new WorldUnitHealedEvent(
                    new WorldUnitGroup.Stack(stack.Id),
                    new WorldUnitId("stack1Leader"),
                    70,
                    WorldResourceSet.Empty)
            ]);

        engine.CheckCommandError(command, WorldCommandError.ItemAlreadyUsedThisTurn);

        Assert.That(engine.State.Players[player.Id], Is.EqualTo(player with
        {
            Resources = new WorldResourceSet(0, 0, 0, 300, 0, 0),
            TurnActions = new WorldPlayerTurnActions
            {
                ItemUses = ImmutableSortedDictionary<WorldItemId, int>.Empty.Add(wandItem.Id, 2)
            }
        }));
    }

    [Test]
    public void SecondWandTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leaderDefinition = ScrollLeaderDefinition();
        WorldSpellDefinition spell = HealSpell(areaRadius: 0);
        ItemDefinition wand = SpellItem("wand1", ItemCategory.Wand, new WorldResourceSet(0, 0, 0, 100, 0, 0));
        WorldItem wandItem = new(new WorldItemId("Wand"), wand.Id, Charges: null);
        WorldItem secondWandItem = new(new WorldItemId("SecondWand"), wand.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(0, 0, 0, 500, 0, 0)
        };

        WorldStack stack = Stack(
            new WorldUnitId("stack1Leader"),
            new WorldStackId("Stack1"),
            player.Id,
            leaderDefinition,
            new WorldPosition(0, 0),
            leaderHitpoints: 10,
            [wandItem, secondWandItem]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(spell)
            .WithDefinition(wand)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        WorldItemTarget.Map target = new(new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0));

        _ = engine.DispatchCommand(new WorldUseItemCommand(stack.Id, wandItem.Id, target));

        engine.CheckCommandError(new WorldUseItemCommand(stack.Id, wandItem.Id, target), WorldCommandError.ItemAlreadyUsedThisTurn);

        engine.CheckCommand(
            new WorldUseItemCommand(stack.Id, secondWandItem.Id, target),
            [
                new WorldItemUsedEvent(stack.Id, secondWandItem, RemainingCharges: null, Consumed: false),
                new WorldSpellCastEvent(
                    player.Id,
                    HealSpellId,
                    new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0),
                    new WorldResourceSet(0, 0, 0, 100, 0, 0),
                    WorldSpellCastSource.Item),
                new WorldUnitHealedEvent(
                    new WorldUnitGroup.Stack(stack.Id),
                    new WorldUnitId("stack1Leader"),
                    70,
                    WorldResourceSet.Empty)
            ]);

        Assert.That(engine.State.Players[player.Id].TurnActions, Is.EqualTo(new WorldPlayerTurnActions
        {
            ItemUses = ImmutableSortedDictionary<WorldItemId, int>.Empty
                .Add(wandItem.Id, 1)
                .Add(secondWandItem.Id, 1)
        }));
    }

    [Test]
    public void WandTransferTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leaderDefinition = ScrollLeaderDefinition();
        WorldSpellDefinition spell = HealSpell(areaRadius: 0);
        ItemDefinition wand = SpellItem("wand1", ItemCategory.Wand, new WorldResourceSet(0, 0, 0, 100, 0, 0));
        WorldItem wandItem = new(new WorldItemId("Wand"), wand.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(0, 0, 0, 500, 0, 0)
        };

        WorldStack stack = Stack(
            new WorldUnitId("stack1Leader"),
            new WorldStackId("Stack1"),
            player.Id,
            leaderDefinition,
            new WorldPosition(0, 0),
            leaderHitpoints: 10,
            [wandItem]);

        WorldStack otherStack = Stack(
            new WorldUnitId("stack2Leader"),
            new WorldStackId("Stack2"),
            player.Id,
            leaderDefinition,
            new WorldPosition(1, 0),
            leaderHitpoints: 10,
            []);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(spell)
            .WithDefinition(wand)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .WithStack(otherStack)
            .Build();

        _ = engine.DispatchCommand(new WorldUseItemCommand(
            stack.Id,
            wandItem.Id,
            new WorldItemTarget.Map(new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0))));
        _ = engine.DispatchCommand(new WorldTransferInventoryItemCommand(
            new WorldUnitGroup.Stack(stack.Id),
            new WorldUnitGroup.Stack(otherStack.Id),
            wandItem.Id));

        engine.CheckCommandError(
            new WorldUseItemCommand(
                otherStack.Id,
                wandItem.Id,
                new WorldItemTarget.Map(new WorldSpellTarget(new WorldPosition(1, 0), Radius: 0))),
            WorldCommandError.ItemAlreadyUsedThisTurn);
    }

    [Test]
    public void WandNextTurnTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leaderDefinition = ScrollLeaderDefinition();
        WorldSpellDefinition spell = HealSpell(areaRadius: 0);
        ItemDefinition wand = SpellItem("wand1", ItemCategory.Wand, new WorldResourceSet(0, 0, 0, 100, 0, 0));
        WorldItem wandItem = new(new WorldItemId("Wand"), wand.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(0, 0, 0, 500, 0, 0)
        };

        WorldStack stack = Stack(
            new WorldUnitId("stack1Leader"),
            new WorldStackId("Stack1"),
            player.Id,
            leaderDefinition,
            new WorldPosition(0, 0),
            leaderHitpoints: 10,
            [wandItem]);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(spell)
            .WithDefinition(wand)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        WorldUseItemCommand command = new(
            stack.Id,
            wandItem.Id,
            new WorldItemTarget.Map(new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)));

        _ = engine.DispatchCommand(new WorldStartGameCommand());
        _ = engine.DispatchCommand(command);
        _ = engine.DispatchCommand(new WorldEndTurnCommand());

        ICommandResult<WorldCommandError, WorldEvent, Empty> result = engine.DispatchCommand(command);

        Assert.That(result.Error, Is.Null);
        Assert.That(engine.State.Players[player.Id].TurnActions, Is.EqualTo(new WorldPlayerTurnActions
        {
            ItemUses = ImmutableSortedDictionary<WorldItemId, int>.Empty.Add(wandItem.Id, 1)
        }));
    }

    [Test]
    public void SoldWandIdNotReusedTest()
    {
        WorldMap map = TestHelpers.SimpleMap(7, 7);

        UnitDefinition leaderDefinition = ScrollLeaderDefinition();
        WorldSpellDefinition spell = HealSpell(areaRadius: 0);
        ItemDefinition wand = SpellItem("wand1", ItemCategory.Wand, new WorldResourceSet(0, 0, 0, 100, 0, 0));
        ItemDefinition potion = TestHelpers.SimpleItemDefinition(new ItemDefinitionId("potion1"), ItemCategory.PotionHeal) with
        {
            Value = new WorldResourceSet(50, 0, 0, 0, 0, 0),
            Effect = new HealItemEffect(50)
        };
        WorldItem wandItem = new(new WorldItemId("Item0001"), wand.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(100, 0, 0, 500, 0, 0)
        };

        WorldStack stack = Stack(
            new WorldUnitId("stack1Leader"),
            new WorldStackId("Stack1"),
            player.Id,
            leaderDefinition,
            new WorldPosition(4, 4),
            leaderHitpoints: 10,
            [wandItem]);

        WorldMerchantSite site = new(
            new WorldSiteId("site"),
            new WorldFootprint(new WorldPosition(2, 2), 3, 3),
            Variant: new WorldObjectVariant(0),
            TitleTextId: new ScenarioTextId("Title"),
            GreetingTextId: new ScenarioTextId("Greeting"),
            [new WorldMerchantItem(potion.Id, 1)],
            VisitorIds: [],
            AiPriority: 3);

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(spell)
            .WithDefinition(wand)
            .WithDefinition(potion)
            .WithRace(TestHelpers.SimpleRace(WorldRace.Undead, new GlobalTextId("Undead Hordes"), WorldMapTerrain.Undead))
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithSubrace(TestHelpers.NeutralSubrace)
            .WithStack(stack)
            .WithSite(site)
            .Build();

        _ = engine.DispatchCommand(new WorldStartGameCommand());
        _ = engine.DispatchCommand(new WorldUseItemCommand(
            stack.Id,
            wandItem.Id,
            new WorldItemTarget.Map(new WorldSpellTarget(new WorldPosition(4, 4), Radius: 0))));
        _ = engine.DispatchCommand(new WorldSellItemCommand(stack.Id, site.Id, wandItem.Id));
        _ = engine.DispatchCommand(new WorldPurchaseItemCommand(stack.Id, site.Id, potion.Id));

        Assert.That(engine.State.Stacks[stack.Id].Inventory, Is.EqualTo(
            new WorldInventory([new WorldItem(new WorldItemId("Item0002"), potion.Id, Charges: null)])));
    }

    [Test]
    public void EquippedWandOncePerTurnTest()
    {
        WorldMap map = TestHelpers.SimpleMap(2, 2);

        UnitDefinition leaderDefinition = ScrollLeaderDefinition();
        WorldSpellDefinition spell = HealSpell(areaRadius: 0);
        ItemDefinition wand = SpellItem("wand1", ItemCategory.Wand, new WorldResourceSet(0, 0, 0, 100, 0, 0));
        WorldItem wandItem = new(new WorldItemId("Wand"), wand.Id, Charges: null);

        WorldPlayer player = TestHelpers.SimplePlayer(new WorldPlayerId(1), WorldRace.Undead, map) with
        {
            Resources = new WorldResourceSet(0, 0, 0, 500, 0, 0)
        };

        WorldStack stack = Stack(
                new WorldUnitId("stack1Leader"),
                new WorldStackId("Stack1"),
                player.Id,
                leaderDefinition,
                new WorldPosition(0, 0),
                leaderHitpoints: 10,
                []) with
            {
                Equipment = LeaderEquipment.Empty.With(LeaderEquipmentSlot.Battle1, wandItem)
            };

        WorldEngine engine = new TestWorldEngineBuilder()
            .WithMap(map)
            .WithDefinition(leaderDefinition)
            .WithDefinition(spell)
            .WithDefinition(wand)
            .WithDefinition(TestHelpers.SimpleLord(player.LordId, player.Race, LordCategory.Warrior))
            .WithPlayer(player)
            .WithStack(stack)
            .Build();

        WorldUseItemCommand command = new(
            stack.Id,
            wandItem.Id,
            new WorldItemTarget.Map(new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0)));

        engine.CheckCommand(
            command,
            [
                new WorldItemUsedEvent(stack.Id, wandItem, RemainingCharges: null, Consumed: false),
                new WorldSpellCastEvent(
                    player.Id,
                    HealSpellId,
                    new WorldSpellTarget(new WorldPosition(0, 0), Radius: 0),
                    new WorldResourceSet(0, 0, 0, 100, 0, 0),
                    WorldSpellCastSource.Item),
                new WorldUnitHealedEvent(
                    new WorldUnitGroup.Stack(stack.Id),
                    new WorldUnitId("stack1Leader"),
                    40,
                    WorldResourceSet.Empty)
            ]);

        Assert.That(engine.State, Is.EqualTo(
            engine.State with
            {
                Players = engine.State.Players.SetItem(player.Id, player with
                {
                    Resources = new WorldResourceSet(0, 0, 0, 400, 0, 0),
                    TurnActions = new WorldPlayerTurnActions
                    {
                        ItemUses = ImmutableSortedDictionary<WorldItemId, int>.Empty.Add(wandItem.Id, 1)
                    }
                }),
                Stacks = engine.State.Stacks.SetItem(stack.Id, stack with
                {
                    Units = new UnitGrid<WorldUnit>.Builder()
                        .Place(TestHelpers.SimpleUnit(new WorldUnitId("stack1Leader"), leaderDefinition) with { Hitpoints = 40 }, GridPosition.BackBottom, false)
                        .Build()
                })
            }));

        engine.CheckCommandError(command, WorldCommandError.ItemAlreadyUsedThisTurn);
    }
}
