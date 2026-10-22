using System.Reflection;
using Gallean.Engine.Contracts;
using Gallean.Tests.Common;
using static Gallean.Assets.AssetAnimationKey;
using static Gallean.Assets.AssetSoundKey;
using static Gallean.Assets.AssetSpriteKey;

namespace Gallean.Assets.Tests;

public sealed class AssetKeyTests
{
    private static IReadOnlyList<Type> GetKeyTypes(Type root)
    {
        List<Type> types = [];
        Queue<Type> queue = new([root]);

        while (queue.Count > 0)
        {
            Type current = queue.Dequeue();

            foreach (Type nested in current.GetNestedTypes())
            {
                if (root.IsAssignableFrom(nested))
                {
                    queue.Enqueue(nested);

                    if (!nested.IsAbstract)
                    {
                        types.Add(nested);
                    }
                }
            }
        }

        return types;
    }

    private static IReadOnlyList<string> GetDeclaredCategories(Type root)
    {
        List<string> categories = [];

        foreach (Type type in GetKeyTypes(root))
        {
            FieldInfo field = Assert.NotNull(type.GetField("Category", BindingFlags.NonPublic | BindingFlags.Static));

            categories.Add(Assert.IsOfType<string>(field.GetRawConstantValue()));
        }

        return categories;
    }

    [Test]
    public void CanonicalIdTest()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(new Ground(WorldMapTerrain.Human).CanonicalId, Is.EqualTo("ground/human"));
            Assert.That(new Ground(WorldMapTerrain.Water).CanonicalId, Is.EqualTo("ground/water"));
            Assert.That(new Water().CanonicalId, Is.EqualTo("water"));
            Assert.That(new Tree(WorldMapTerrain.Elf, 7).CanonicalId, Is.EqualTo("tree/elf/7"));
            Assert.That(new GroundBorder(GroundBorderSet.Mask, new GroundBorderShape.Edges(GroundBorderEdges.West), 0).CanonicalId, Is.EqualTo("ground-border/mask/edge/w/0"));
            Assert.That(new GroundBorder(GroundBorderSet.Mask, new GroundBorderShape.Edges(GroundBorderEdges.North | GroundBorderEdges.West), 0).CanonicalId, Is.EqualTo("ground-border/mask/edge/nw/0"));
            Assert.That(new GroundBorder(GroundBorderSet.Mask, new GroundBorderShape.Corners(GroundBorderCorners.NorthWest | GroundBorderCorners.SouthEast), 0).CanonicalId, Is.EqualTo("ground-border/mask/corner/nwse/0"));
            Assert.That(new GroundBorder(GroundBorderSet.Coast, new GroundBorderShape.Edges(GroundBorderEdges.North | GroundBorderEdges.South), 0).CanonicalId, Is.EqualTo("ground-border/coast/edge/ns/0"));
            Assert.That(new GroundBorder(GroundBorderSet.Coast, new GroundBorderShape.Edges(GroundBorderEdges.North | GroundBorderEdges.South), 2).CanonicalId, Is.EqualTo("ground-border/coast/edge/ns/2"));
            Assert.That(new GroundBorder(GroundBorderSet.Coast, new GroundBorderShape.Corners(GroundBorderCorners.SouthWest), 0).CanonicalId, Is.EqualTo("ground-border/coast/corner/sw/0"));
            Assert.That(new Mountain(WorldRace.Neutral, 3, 4).CanonicalId, Is.EqualTo("mountain/neutral/3/4"));
            Assert.That(new Mountain(WorldRace.Dwarf, 5, 2).CanonicalId, Is.EqualTo("mountain/dwarf/5/2"));
            Assert.That(new Mountain(WorldRace.Neutral, 4, 12).CanonicalId, Is.EqualTo("mountain/neutral/3/3"));
            Assert.That(new Mountain(WorldRace.Neutral, 7, 0).CanonicalId, Is.EqualTo("mountain/neutral/5/0"));
            Assert.That(new Mountain(WorldRace.Neutral, 0, 0).CanonicalId, Is.EqualTo("mountain/neutral/1/0"));
            Assert.That(new MountainRemoveEffect(5).CanonicalId, Is.EqualTo("mountain-remove-effect/5"));
            Assert.That(new MountainRemoveEffect(0).CanonicalId, Is.EqualTo("mountain-remove-effect/3"));
            Assert.That(new MountainRemoveEffect(9).CanonicalId, Is.EqualTo("mountain-remove-effect/7"));
            Assert.That(new Crystal(WorldResourceType.GroveMana).CanonicalId, Is.EqualTo("crystal/grove-mana"));
            Assert.That(new City(2).CanonicalId, Is.EqualTo("city/2"));
            Assert.That(new City(0).CanonicalId, Is.EqualTo("city/1"));
            Assert.That(new City(9).CanonicalId, Is.EqualTo("city/5"));
            Assert.That(new CityShadow(2).CanonicalId, Is.EqualTo("city-shadow/2"));
            Assert.That(new CityShadow(0).CanonicalId, Is.EqualTo("city-shadow/1"));
            Assert.That(new CityShadow(9).CanonicalId, Is.EqualTo("city-shadow/5"));
            Assert.That(new Capital(WorldRace.Dwarf, IsCaptured: false).CanonicalId, Is.EqualTo("capital/dwarf"));
            Assert.That(new Capital(WorldRace.Dwarf, IsCaptured: true).CanonicalId, Is.EqualTo("capital/captured/dwarf"));
            Assert.That(new CapitalBackground(WorldRace.Dwarf, 0).CanonicalId, Is.EqualTo("capital-background/dwarf/0"));
            Assert.That(new CapitalBackgroundOverlay(WorldRace.Human, 1).CanonicalId, Is.EqualTo("capital-background-overlay/human/1"));
            Assert.That(new CapitalBuilding(new WorldPlayerBuildingDefinitionId("G000BB0023")).CanonicalId, Is.EqualTo("capital-building/g000bb0023"));
            Assert.That(new MainMenuBackground().CanonicalId, Is.EqualTo("main-menu-background"));
            Assert.That(new MainMenuBackgroundV2().CanonicalId, Is.EqualTo("main-menu-background-v2"));
            Assert.That(new MainMenuBackgroundOverlay(1).CanonicalId, Is.EqualTo("main-menu-background-overlay/1"));
            Assert.That(new MainMenuBackgroundOverlay(3).CanonicalId, Is.EqualTo("main-menu-background-overlay/1"));
            Assert.That(new RaceSelectionBackground(WorldRace.Heretic).CanonicalId, Is.EqualTo("race-selection-background/heretic"));
            Assert.That(new RaceSelectionBackgroundOverlay(WorldRace.Undead, 1).CanonicalId, Is.EqualTo("race-selection-background-overlay/undead/1"));
            Assert.That(new CityShield(WorldRace.Undead, IsCapital: false).CanonicalId, Is.EqualTo("city-shield/city/undead"));
            Assert.That(new CityShield(WorldRace.Neutral, IsCapital: false).CanonicalId, Is.EqualTo("city-shield/city/neutral"));
            Assert.That(new CityShield(WorldRace.Elf, IsCapital: true).CanonicalId, Is.EqualTo("city-shield/capital/elf"));
            Assert.That(new Ruin(IsLooted: false, 4).CanonicalId, Is.EqualTo("ruin/intact/4"));
            Assert.That(new Ruin(IsLooted: true, 4).CanonicalId, Is.EqualTo("ruin/looted/4"));
            Assert.That(new Ruin(IsLooted: false, 12).CanonicalId, Is.EqualTo("ruin/intact/1"));
            Assert.That(new RuinBanner(IsLooted: false).CanonicalId, Is.EqualTo("ruin-banner/intact"));
            Assert.That(new RuinBanner(IsLooted: true).CanonicalId, Is.EqualTo("ruin-banner/looted"));
            Assert.That(new Rod(WorldRace.Undead).CanonicalId, Is.EqualTo("rod/undead"));
            Assert.That(new Rod(WorldRace.Elf).CanonicalId, Is.EqualTo("rod/elf"));
            Assert.That(new Bag(IsWater: false, 2).CanonicalId, Is.EqualTo("bag/land/2"));
            Assert.That(new Bag(IsWater: true, 1).CanonicalId, Is.EqualTo("bag/water/1"));
            Assert.That(new Tombstone(IsWater: false).CanonicalId, Is.EqualTo("tombstone/land"));
            Assert.That(new Tombstone(IsWater: true).CanonicalId, Is.EqualTo("tombstone/water"));
            Assert.That(new BattleBackground(new BattleLocation.Terrain(WorldMapTerrain.Human), 1).CanonicalId, Is.EqualTo("battle-background/terrain/human/1"));
            Assert.That(new BattleBackground(new BattleLocation.City(), 5).CanonicalId, Is.EqualTo("battle-background/city/0"));
            Assert.That(new BattleBackground(new BattleLocation.Ruin(), 1).CanonicalId, Is.EqualTo("battle-background/ruin/1"));
            Assert.That(new BattleBackground(new BattleLocation.Capital(WorldRace.Dwarf), 0).CanonicalId, Is.EqualTo("battle-background/capital/dwarf"));
            Assert.That(new BattleBackground(new BattleLocation.Terrain(WorldMapTerrain.Neutral), 5).CanonicalId, Is.EqualTo("battle-background/terrain/neutral/1"));
            Assert.That(new BattleBackground(new BattleLocation.Terrain(WorldMapTerrain.Water), 2).CanonicalId, Is.EqualTo("battle-background/terrain/water/0"));
            Assert.That(new BattleBackground(new BattleLocation.Boat(), 3).CanonicalId, Is.EqualTo("battle-background/boat/1"));
            Assert.That(new BattleCorpse(IsLarge: false, 1).CanonicalId, Is.EqualTo("battle-corpse/small/1"));
            Assert.That(new BattleCorpse(IsLarge: true, 3).CanonicalId, Is.EqualTo("battle-corpse/large/1"));
            Assert.That(new UnitFace(new UnitDefinitionId("G000UU0001")).CanonicalId, Is.EqualTo("unit-face/g000uu0001"));
            Assert.That(new UnitPortrait(new UnitDefinitionId("G000UU0001")).CanonicalId, Is.EqualTo("unit-portrait/g000uu0001"));
            Assert.That(new StackUnit(new UnitDefinitionId("G000UU0001"), StackUnitPose.Move, 3).CanonicalId, Is.EqualTo("stack-unit/g000uu0001/move/3"));
            Assert.That(new StackUnit(new UnitDefinitionId("G000UU0001"), StackUnitPose.Stop, 9).CanonicalId, Is.EqualTo("stack-unit/g000uu0001/stop/1"));
            Assert.That(new StackUnitShadow(new UnitDefinitionId("G000UU0001"), StackUnitPose.Stop, 3).CanonicalId, Is.EqualTo("stack-unit-shadow/g000uu0001/stop/3"));
            Assert.That(new StackUnitAura(new UnitDefinitionId("G000UU0001"), StackUnitPose.Move, 3).CanonicalId, Is.EqualTo("stack-unit-aura/g000uu0001/move/3"));
            Assert.That(new StackBoat(WorldRace.Neutral, StackUnitPose.Move, 5).CanonicalId, Is.EqualTo("stack-boat/neutral/move/5"));
            Assert.That(new StackBoatShadow(WorldRace.Elf, StackUnitPose.Stop, 5).CanonicalId, Is.EqualTo("stack-boat-shadow/elf/stop/5"));
            Assert.That(new StackBanner(7).CanonicalId, Is.EqualTo("stack-banner/7"));
            Assert.That(new StackDestroyEffect().CanonicalId, Is.EqualTo("stack-destroy-effect"));
            Assert.That(new StackSummonDestroyEffect().CanonicalId, Is.EqualTo("stack-summon-destroy-effect"));
            Assert.That(new StackCreateEffect().CanonicalId, Is.EqualTo("stack-create-effect"));
            Assert.That(new StackOwnerChangeEffect().CanonicalId, Is.EqualTo("stack-owner-change-effect"));
            Assert.That(new StackBattleEffect().CanonicalId, Is.EqualTo("stack-battle-effect"));
            Assert.That(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("G000UU0001"), BattleUnitPose.Attack, BattleSide.Defender).CanonicalId, Is.EqualTo("battle-unit/g000uu0001/attack/defender"));
            Assert.That(new BattleUnitShadow(new UnitDefinitionId("G000UU0001"), BattleUnitPose.Idle, BattleSide.Attacker).CanonicalId, Is.EqualTo("battle-unit-shadow/g000uu0001/idle/attacker"));
            Assert.That(new BattleUnitAura(new UnitDefinitionId("G000UU0001"), BattleUnitPose.Hit, BattleSide.Defender).CanonicalId, Is.EqualTo("battle-unit-aura/g000uu0001/hit/defender"));
            Assert.That(new AssetAnimationKey.BattleEffect.Attack(new UnitDefinitionId("G000UU0001"), AssetAnimationKey.BattleEffect.AttackType.Touch, BattleLayer.Body, AssetAnimationKey.BattleEffect.AttackSide.Attack, 0).CanonicalId, Is.EqualTo("battle-effect-attack/g000uu0001/touch/body/attack/0"));
            Assert.That(new AssetAnimationKey.BattleEffect.Attack(new UnitDefinitionId("G000UU0001"), AssetAnimationKey.BattleEffect.AttackType.Hit, BattleLayer.Shadow, AssetAnimationKey.BattleEffect.AttackSide.Defend, 2).CanonicalId, Is.EqualTo("battle-effect-attack/g000uu0001/hit/shadow/defend/2"));
            Assert.That(new AssetAnimationKey.BattleEffect.Attack(new UnitDefinitionId("G000UU0001"), AssetAnimationKey.BattleEffect.AttackType.Hit, BattleLayer.Aura, AssetAnimationKey.BattleEffect.AttackSide.Both, 1).CanonicalId, Is.EqualTo("battle-effect-attack/g000uu0001/hit/aura/both/1"));
            Assert.That(new AssetAnimationKey.BattleEffect.Attack(new UnitDefinitionId("G000UU0001"), AssetAnimationKey.BattleEffect.AttackType.Touch, BattleLayer.Body, AssetAnimationKey.BattleEffect.AttackSide.Attack, 2).CanonicalId, Is.EqualTo("battle-effect-attack/g000uu0001/touch/body/attack/2"));
            Assert.That(new AssetAnimationKey.BattleEffect.Attack(new UnitDefinitionId("G000UU0001"), AssetAnimationKey.BattleEffect.AttackType.Touch, BattleLayer.Body, AssetAnimationKey.BattleEffect.AttackSide.Attack, 4).CanonicalId, Is.EqualTo("battle-effect-attack/g000uu0001/touch/body/attack/1"));
            Assert.That(new AssetAnimationKey.BattleEffect.Attack(new UnitDefinitionId("G000UU0001"), AssetAnimationKey.BattleEffect.AttackType.Touch, BattleLayer.Body, AssetAnimationKey.BattleEffect.AttackSide.Attack, -1).CanonicalId, Is.EqualTo("battle-effect-attack/g000uu0001/touch/body/attack/2"));
            Assert.That(new AssetAnimationKey.BattleEffect.Death(UnitDeathAnimation.Dragon, IsLarge: true).CanonicalId, Is.EqualTo("battle-effect-death/dragon/large"));
            Assert.That(new AssetAnimationKey.BattleEffect.LevelUp(WorldRace.Undead, IsLarge: false).CanonicalId, Is.EqualTo("battle-effect-level-up/undead/small"));
            Assert.That(new AssetAnimationKey.BattleEffect.Status(AssetAnimationKey.BattleEffect.StatusType.Transform, IsLarge: false).CanonicalId, Is.EqualTo("battle-effect-status/transform/small"));
            Assert.That(new AssetAnimationKey.BattleEffect.Status(AssetAnimationKey.BattleEffect.StatusType.Doppelganger, IsLarge: true).CanonicalId, Is.EqualTo("battle-effect-status/doppelganger/large"));
            Assert.That(new AssetAnimationKey.BattleEffect.Status(AssetAnimationKey.BattleEffect.StatusType.ReduceLevel, IsLarge: false).CanonicalId, Is.EqualTo("battle-effect-status/reduce-level/small"));
            Assert.That(new AssetAnimationKey.BattleEffect.Status(AssetAnimationKey.BattleEffect.StatusType.Untransform, IsLarge: true).CanonicalId, Is.EqualTo("battle-effect-status/untransform/large"));
            Assert.That(new AssetAnimationKey.BattleEffect.Status(AssetAnimationKey.BattleEffect.StatusType.Undoppelganger, IsLarge: false).CanonicalId, Is.EqualTo("battle-effect-status/undoppelganger/small"));
            Assert.That(new AssetAnimationKey.BattleEffect.Status(AssetAnimationKey.BattleEffect.StatusType.RestoreLevel, IsLarge: true).CanonicalId, Is.EqualTo("battle-effect-status/restore-level/large"));
            Assert.That(new AssetAnimationKey.BattleEffect.Heal().CanonicalId, Is.EqualTo("battle-effect-heal"));
            Assert.That(new AssetAnimationKey.BattleEffect.Blister().CanonicalId, Is.EqualTo("battle-effect-blister"));
            Assert.That(new AssetAnimationKey.BattleEffect.Item(AssetAnimationKey.BattleEffect.ItemType.Potion).CanonicalId, Is.EqualTo("battle-effect-item/potion"));
            Assert.That(new AssetAnimationKey.BattleEffect.Item(AssetAnimationKey.BattleEffect.ItemType.Talisman).CanonicalId, Is.EqualTo("battle-effect-item/talisman"));
            Assert.That(new UnitEventPortrait(new ScenarioEventPortraitId("G000UU0001")).CanonicalId, Is.EqualTo("unit-event-portrait/g000uu0001"));
            Assert.That(new SpellIcon(new WorldSpellDefinitionId("G000SS0001")).CanonicalId, Is.EqualTo("spell-icon/g000ss0001"));
            Assert.That(new BuildingIcon(new WorldPlayerBuildingDefinitionId("G000BB0001")).CanonicalId, Is.EqualTo("building-icon/g000bb0001"));
            Assert.That(new ItemIcon(new ItemIconSource.Item(new ItemDefinitionId("G000IG0001"))).CanonicalId, Is.EqualTo("item-icon/g000ig0001"));
            Assert.That(new ItemIcon(new ItemIconSource.Scroll(WorldRace.Human)).CanonicalId, Is.EqualTo("item-icon/scroll/human"));
            Assert.That(new ItemIcon(new ItemIconSource.Scroll(WorldRace.Elf)).CanonicalId, Is.EqualTo("item-icon/scroll/elf"));
            Assert.That(new ThiefActionImage(WorldThiefActionType.PoisonStack, IsLarge: false).CanonicalId, Is.EqualTo("thief-action-image/poison-stack/small"));
            Assert.That(new ThiefActionImage(WorldThiefActionType.RiotCity, IsLarge: true).CanonicalId, Is.EqualTo("thief-action-image/riot-city/large"));
            Assert.That(new SiteImage(SiteType.Merchant).CanonicalId, Is.EqualTo("site-image/merchant"));
            Assert.That(new SiteImage(SiteType.Trainer).CanonicalId, Is.EqualTo("site-image/trainer"));
            Assert.That(new CityImage(3).CanonicalId, Is.EqualTo("city-image/3"));
            Assert.That(new CityImage(9).CanonicalId, Is.EqualTo("city-image/5"));
            Assert.That(new CapitalImage(WorldRace.Heretic).CanonicalId, Is.EqualTo("capital-image/heretic"));
            Assert.That(new LordFace(WorldRace.Human, 3, IsLarge: true).CanonicalId, Is.EqualTo("lord-face/human/3/large"));
            Assert.That(new LordFace(WorldRace.Undead, 0, IsLarge: false).CanonicalId, Is.EqualTo("lord-face/undead/0/small"));
            Assert.That(new LordFace(WorldRace.Elf, 5, IsLarge: false).CanonicalId, Is.EqualTo("lord-face/elf/1/small"));
            Assert.That(new LordFace(WorldRace.Neutral, 2, IsLarge: true).CanonicalId, Is.EqualTo("lord-face/neutral/0/large"));
            Assert.That(new Landmark(new WorldLandmarkDefinitionId("G000MG0004")).CanonicalId, Is.EqualTo("landmark/g000mg0004"));
            Assert.That(new WaterLandmark(new WorldLandmarkDefinitionId("G000MG0004")).CanonicalId, Is.EqualTo("water-landmark/g000mg0004"));
            Assert.That(new Site(SiteType.Mage, 2).CanonicalId, Is.EqualTo("site/mage/2"));
            Assert.That(new Site(SiteType.Merchant, 9).CanonicalId, Is.EqualTo("site/merchant/1"));
            Assert.That(new SiteShadow(SiteType.Mage, 2).CanonicalId, Is.EqualTo("site-shadow/mage/2"));
            Assert.That(new SiteShadow(SiteType.Merchant, 9).CanonicalId, Is.EqualTo("site-shadow/merchant/1"));
            Assert.That(new SiteBanner(SiteType.Merchant).CanonicalId, Is.EqualTo("site-banner/merchant"));
            Assert.That(new SiteBanner(SiteType.Trainer).CanonicalId, Is.EqualTo("site-banner/trainer"));
            Assert.That(new Cloud(2).CanonicalId, Is.EqualTo("cloud/2"));
            Assert.That(new Cloud(4).CanonicalId, Is.EqualTo("cloud/1"));
            Assert.That(new Cursor(CursorType.Default).CanonicalId, Is.EqualTo("cursor/default"));
            Assert.That(new Cursor(CursorType.CastSpellInvalid).CanonicalId, Is.EqualTo("cursor/cast-spell-invalid"));
            Assert.That(new Cursor(CursorType.BattleAttack).CanonicalId, Is.EqualTo("cursor/battle-attack"));
            Assert.That(new WorldSpellEffect(new WorldSpellDefinitionId("G000SS0001"), WorldSpellEffectLayer.Ground, 1).CanonicalId, Is.EqualTo("world-spell-effect/g000ss0001/ground/1"));
            Assert.That(new WorldSpellEffect(new WorldSpellDefinitionId("G000SS0001"), WorldSpellEffectLayer.Top, 3).CanonicalId, Is.EqualTo("world-spell-effect/g000ss0001/top/1"));
            Assert.That(new Road(RoadConnections.None).CanonicalId, Is.EqualTo("road/none"));
            Assert.That(new Road(RoadConnections.North).CanonicalId, Is.EqualTo("road/n"));
            Assert.That(new Road(RoadConnections.East | RoadConnections.West).CanonicalId, Is.EqualTo("road/ew"));
            Assert.That(new Road(RoadConnections.All).CanonicalId, Is.EqualTo("road/nesw"));
        }
    }

    [Test]
    public void SoundCanonicalIdTest()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(new MenuMusic().CanonicalId, Is.EqualTo("menu-music"));
            Assert.That(new StackCreate().CanonicalId, Is.EqualTo("stack-create"));
            Assert.That(new StackDestroy().CanonicalId, Is.EqualTo("stack-destroy"));
            Assert.That(new StackSummonDestroy().CanonicalId, Is.EqualTo("stack-summon-destroy"));
            Assert.That(new WorldSpellCast(new WorldSpellDefinitionId("G000SS0001")).CanonicalId, Is.EqualTo("world-spell-cast/g000ss0001"));
            Assert.That(new BattleMusic(1).CanonicalId, Is.EqualTo("battle-music/1"));
            Assert.That(new BattleMusic(5).CanonicalId, Is.EqualTo("battle-music/1"));
            Assert.That(new BattleMusic(-1).CanonicalId, Is.EqualTo("battle-music/3"));
            Assert.That(new WorldMusic(10).CanonicalId, Is.EqualTo("world-music/10"));
            Assert.That(new WorldMusic(11).CanonicalId, Is.EqualTo("world-music/0"));
            Assert.That(new AmbientMusic(10).CanonicalId, Is.EqualTo("ambient-music/10"));
            Assert.That(new CapitalMusic(WorldRace.Human, 10).CanonicalId, Is.EqualTo("capital-music/human/1"));
            Assert.That(new BattleUnitAttack(new UnitDefinitionId("G000UU0001"), 6).CanonicalId, Is.EqualTo("battle-unit-attack/g000uu0001/0"));
            Assert.That(new BattleUnitHit(new UnitDefinitionId("G000UU0001"), 2).CanonicalId, Is.EqualTo("battle-unit-hit/g000uu0001/2"));
            Assert.That(new BattleUnitTouch(new UnitDefinitionId("G000UU0001"), 7).CanonicalId, Is.EqualTo("battle-unit-touch/g000uu0001/2"));
            Assert.That(new BattleUnitDamaged(new UnitDefinitionId("G000UU0001"), 5).CanonicalId, Is.EqualTo("battle-unit-damaged/g000uu0001/1"));
            Assert.That(new StackUnitMove(new UnitDefinitionId("G000UU0001"), 3).CanonicalId, Is.EqualTo("stack-unit-move/g000uu0001/3"));
            Assert.That(new StackBoatMove().CanonicalId, Is.EqualTo("stack-boat-move"));
            Assert.That(new StackUnitResponse(new UnitDefinitionId("G000UU0001"), 5).CanonicalId, Is.EqualTo("stack-unit-response/g000uu0001/1"));
            Assert.That(new StackUnitAttack(new UnitDefinitionId("G000UU0001")).CanonicalId, Is.EqualTo("stack-unit-attack/g000uu0001"));
            Assert.That(new StackUnitIdle(new UnitDefinitionId("G000UU0001"), 1).CanonicalId, Is.EqualTo("stack-unit-idle/g000uu0001/1"));
            Assert.That(new StackUnitIdle(new UnitDefinitionId("G000UU0001"), 3).CanonicalId, Is.EqualTo("stack-unit-idle/g000uu0001/1"));
            Assert.That(new StackUnitDeath(new UnitDefinitionId("G000UU0001")).CanonicalId, Is.EqualTo("stack-unit-death/g000uu0001"));
            Assert.That(new InterfaceOpen().CanonicalId, Is.EqualTo("interface-open"));
            Assert.That(new Beep().CanonicalId, Is.EqualTo("beep"));
            Assert.That(new BookOpen().CanonicalId, Is.EqualTo("book-open"));
            Assert.That(new BookClose().CanonicalId, Is.EqualTo("book-close"));
            Assert.That(new TurnStart().CanonicalId, Is.EqualTo("turn-start"));
            Assert.That(new WorldBattleStart().CanonicalId, Is.EqualTo("world-battle-start"));
            Assert.That(new Occupy().CanonicalId, Is.EqualTo("occupy"));
            Assert.That(new RuinEnter().CanonicalId, Is.EqualTo("ruin-enter"));
            Assert.That(new CityEnter().CanonicalId, Is.EqualTo("city-enter"));
            Assert.That(new CityExit().CanonicalId, Is.EqualTo("city-exit"));
            Assert.That(new RuinLoot().CanonicalId, Is.EqualTo("ruin-loot"));
            Assert.That(new BuildingConstruct().CanonicalId, Is.EqualTo("building-construct"));
            Assert.That(new CityGrow().CanonicalId, Is.EqualTo("city-grow"));
            Assert.That(new CityRiotEnd().CanonicalId, Is.EqualTo("city-riot-end"));
            Assert.That(new BagPickUp().CanonicalId, Is.EqualTo("bag-pick-up"));
            Assert.That(new UnitPurchase().CanonicalId, Is.EqualTo("unit-purchase"));
            Assert.That(new SitePurchase().CanonicalId, Is.EqualTo("site-purchase"));
            Assert.That(new SiteEnter().CanonicalId, Is.EqualTo("site-enter"));
            Assert.That(new ItemUse().CanonicalId, Is.EqualTo("item-use"));
            Assert.That(new SpellResearch().CanonicalId, Is.EqualTo("spell-research"));
            Assert.That(new ThiefAction().CanonicalId, Is.EqualTo("thief-action"));
            Assert.That(new AssetSoundKey.BattleEffect.Heal().CanonicalId, Is.EqualTo("battle-effect-sound-heal"));
            Assert.That(new AssetSoundKey.BattleEffect.Death().CanonicalId, Is.EqualTo("battle-effect-sound-death"));
            Assert.That(new AssetSoundKey.BattleEffect.Status(AssetSoundKey.BattleEffect.StatusType.Poison).CanonicalId, Is.EqualTo("battle-effect-sound-status/poison"));
            Assert.That(new AssetSoundKey.BattleEffect.Status(AssetSoundKey.BattleEffect.StatusType.ShatterArmor).CanonicalId, Is.EqualTo("battle-effect-sound-status/shatter-armor"));
            Assert.That(new AssetSoundKey.BattleEffect.Status(AssetSoundKey.BattleEffect.StatusType.Unsummon).CanonicalId, Is.EqualTo("battle-effect-sound-status/unsummon"));
            Assert.That(new AssetSoundKey.BattleEffect.Potion(AssetSoundKey.BattleEffect.PotionType.Boost).CanonicalId, Is.EqualTo("battle-effect-sound-potion/boost"));
            Assert.That(new AssetSoundKey.BattleEffect.Potion(AssetSoundKey.BattleEffect.PotionType.Heal).CanonicalId, Is.EqualTo("battle-effect-sound-potion/heal"));
            Assert.That(new AssetSoundKey.BattleEffect.Potion(AssetSoundKey.BattleEffect.PotionType.Revive).CanonicalId, Is.EqualTo("battle-effect-sound-potion/revive"));
        }
    }

    [Test]
    public void ParseTest()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(AssetSpriteKey.Parse("ground/water", null), Is.EqualTo(new Ground(WorldMapTerrain.Water)));
            Assert.That(AssetSpriteKey.Parse("ground-border/mask/edge/nw/0", null), Is.EqualTo(new GroundBorder(GroundBorderSet.Mask, new GroundBorderShape.Edges(GroundBorderEdges.North | GroundBorderEdges.West), 0)));
            Assert.That(AssetSpriteKey.Parse("ground-border/coast/edge/ns/2", null), Is.EqualTo(new GroundBorder(GroundBorderSet.Coast, new GroundBorderShape.Edges(GroundBorderEdges.North | GroundBorderEdges.South), 2)));
            Assert.That(AssetSpriteKey.Parse("ground-border/mask/corner/nwse/0", null), Is.EqualTo(new GroundBorder(GroundBorderSet.Mask, new GroundBorderShape.Corners(GroundBorderCorners.NorthWest | GroundBorderCorners.SouthEast), 0)));
            Assert.That(AssetSpriteKey.Parse("tree/elf/7", null), Is.EqualTo(new Tree(WorldMapTerrain.Elf, 7)));
            Assert.That(AssetSpriteKey.Parse("mountain/dwarf/5/2", null), Is.EqualTo(new Mountain(WorldRace.Dwarf, 5, 2)));
            Assert.That(AssetSpriteKey.Parse("bag/water/1", null), Is.EqualTo(new Bag(IsWater: true, 1)));
            Assert.That(AssetSpriteKey.Parse("tombstone/land", null), Is.EqualTo(new Tombstone(IsWater: false)));
            Assert.That(AssetSpriteKey.Parse("main-menu-background", null), Is.EqualTo(new MainMenuBackground()));
            Assert.That(AssetSpriteKey.Parse("main-menu-background-v2", null), Is.EqualTo(new MainMenuBackgroundV2()));
            Assert.That(AssetSpriteKey.Parse("battle-background/terrain/human/1", null), Is.EqualTo(new BattleBackground(new BattleLocation.Terrain(WorldMapTerrain.Human), 1)));
            Assert.That(AssetSpriteKey.Parse("battle-background/boat/1", null), Is.EqualTo(new BattleBackground(new BattleLocation.Boat(), 1)));
            Assert.That(AssetSpriteKey.Parse("battle-background/city/1", null), Is.EqualTo(new BattleBackground(new BattleLocation.City(), 1)));
            Assert.That(AssetSpriteKey.Parse("battle-background/capital/dwarf", null), Is.EqualTo(new BattleBackground(new BattleLocation.Capital(WorldRace.Dwarf), 0)));
            Assert.That(AssetSpriteKey.Parse("battle-corpse/large/1", null), Is.EqualTo(new BattleCorpse(IsLarge: true, 1)));
            Assert.That(AssetSpriteKey.Parse("unit-face/g000uu0001", null), Is.EqualTo(new UnitFace(new UnitDefinitionId("G000UU0001"))));
            Assert.That(AssetSpriteKey.Parse("unit-event-portrait/centaur", null), Is.EqualTo(new UnitEventPortrait(new ScenarioEventPortraitId("CENTAUR"))));
            Assert.That(AssetSpriteKey.Parse("item-icon/g000ig0001", null), Is.EqualTo(new ItemIcon(new ItemIconSource.Item(new ItemDefinitionId("G000IG0001")))));
            Assert.That(AssetSpriteKey.Parse("item-icon/scroll/elf", null), Is.EqualTo(new ItemIcon(new ItemIconSource.Scroll(WorldRace.Elf))));
            Assert.That(AssetSpriteKey.Parse("thief-action-image/steal-item/small", null), Is.EqualTo(new ThiefActionImage(WorldThiefActionType.StealItem, IsLarge: false)));
            Assert.That(AssetSpriteKey.Parse("thief-action-image/spy-ruin/large", null), Is.EqualTo(new ThiefActionImage(WorldThiefActionType.SpyRuin, IsLarge: true)));
            Assert.That(AssetSpriteKey.Parse("site-image/mercenary", null), Is.EqualTo(new SiteImage(SiteType.Mercenary)));
            Assert.That(AssetSpriteKey.Parse("city-image/2", null), Is.EqualTo(new CityImage(2)));
            Assert.That(AssetSpriteKey.Parse("capital-image/elf", null), Is.EqualTo(new CapitalImage(WorldRace.Elf)));
            Assert.That(AssetSpriteKey.Parse("lord-face/human/3/large", null), Is.EqualTo(new LordFace(WorldRace.Human, 3, IsLarge: true)));
            Assert.That(AssetSpriteKey.Parse("city-shield/capital/elf", null), Is.EqualTo(new CityShield(WorldRace.Elf, IsCapital: true)));
            Assert.That(AssetSpriteKey.Parse("ruin-banner/looted", null), Is.EqualTo(new RuinBanner(IsLooted: true)));
            Assert.That(AssetSpriteKey.Parse("road/none", null), Is.EqualTo(new Road(RoadConnections.None)));
            Assert.That(AssetSpriteKey.Parse("road/ew", null), Is.EqualTo(new Road(RoadConnections.East | RoadConnections.West)));
            Assert.That(AssetSpriteKey.Parse("road/nesw", null), Is.EqualTo(new Road(RoadConnections.All)));
        }
    }

    [Test]
    public void ParseAnimationTest()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(AssetAnimationKey.Parse("water", null), Is.EqualTo(new Water()));
            Assert.That(AssetAnimationKey.Parse("stack-unit/g000uu0001/move/3", null), Is.EqualTo(new StackUnit(new UnitDefinitionId("G000UU0001"), StackUnitPose.Move, 3)));
            Assert.That(AssetAnimationKey.Parse("stack-boat-shadow/elf/stop/5", null), Is.EqualTo(new StackBoatShadow(WorldRace.Elf, StackUnitPose.Stop, 5)));
            Assert.That(AssetAnimationKey.Parse("stack-owner-change-effect", null), Is.EqualTo(new StackOwnerChangeEffect()));
            Assert.That(AssetAnimationKey.Parse("stack-battle-effect", null), Is.EqualTo(new StackBattleEffect()));
            Assert.That(AssetAnimationKey.Parse("mountain-remove-effect/5", null), Is.EqualTo(new MountainRemoveEffect(5)));
            Assert.That(AssetAnimationKey.Parse("battle-unit/g000uu0001/attack/defender", null), Is.EqualTo(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("G000UU0001"), BattleUnitPose.Attack, BattleSide.Defender)));
            Assert.That(AssetAnimationKey.Parse("battle-unit-aura/g000uu0001/hit/defender", null), Is.EqualTo(new BattleUnitAura(new UnitDefinitionId("G000UU0001"), BattleUnitPose.Hit, BattleSide.Defender)));
            Assert.That(AssetAnimationKey.Parse("battle-effect-attack/g000uu0001/hit/aura/both/1", null), Is.EqualTo(new AssetAnimationKey.BattleEffect.Attack(new UnitDefinitionId("G000UU0001"), AssetAnimationKey.BattleEffect.AttackType.Hit, BattleLayer.Aura, AssetAnimationKey.BattleEffect.AttackSide.Both, 1)));
            Assert.That(AssetAnimationKey.Parse("battle-effect-heal", null), Is.EqualTo(new AssetAnimationKey.BattleEffect.Heal()));
            Assert.That(AssetAnimationKey.Parse("battle-effect-item/talisman", null), Is.EqualTo(new AssetAnimationKey.BattleEffect.Item(AssetAnimationKey.BattleEffect.ItemType.Talisman)));
            Assert.That(AssetAnimationKey.Parse("battle-effect-death/dragon/large", null), Is.EqualTo(new AssetAnimationKey.BattleEffect.Death(UnitDeathAnimation.Dragon, IsLarge: true)));
            Assert.That(AssetAnimationKey.Parse("battle-effect-status/reduce-level/small", null), Is.EqualTo(new AssetAnimationKey.BattleEffect.Status(AssetAnimationKey.BattleEffect.StatusType.ReduceLevel, IsLarge: false)));
            Assert.That(AssetAnimationKey.Parse("crystal/grove-mana", null), Is.EqualTo(new Crystal(WorldResourceType.GroveMana)));
            Assert.That(AssetAnimationKey.Parse("city-shadow/2", null), Is.EqualTo(new CityShadow(2)));
            Assert.That(AssetAnimationKey.Parse("capital/dwarf", null), Is.EqualTo(new Capital(WorldRace.Dwarf, IsCaptured: false)));
            Assert.That(AssetAnimationKey.Parse("capital/captured/dwarf", null), Is.EqualTo(new Capital(WorldRace.Dwarf, IsCaptured: true)));
            Assert.That(AssetAnimationKey.Parse("capital-building/g000bb0023", null), Is.EqualTo(new CapitalBuilding(new WorldPlayerBuildingDefinitionId("G000BB0023"))));
            Assert.That(AssetAnimationKey.Parse("ruin/intact/4", null), Is.EqualTo(new Ruin(IsLooted: false, 4)));
            Assert.That(AssetAnimationKey.Parse("water-landmark/g000mg0004", null), Is.EqualTo(new WaterLandmark(new WorldLandmarkDefinitionId("G000MG0004"))));
            Assert.That(AssetAnimationKey.Parse("site-shadow/merchant/1", null), Is.EqualTo(new SiteShadow(SiteType.Merchant, 1)));
            Assert.That(AssetAnimationKey.Parse("cursor/wait", null), Is.EqualTo(new Cursor(CursorType.Wait)));
            Assert.That(AssetAnimationKey.Parse("cursor/create-rod-invalid", null), Is.EqualTo(new Cursor(CursorType.CreateRodInvalid)));
            Assert.That(AssetAnimationKey.Parse("world-spell-effect/g000ss0001/ground/1", null), Is.EqualTo(new WorldSpellEffect(new WorldSpellDefinitionId("G000SS0001"), WorldSpellEffectLayer.Ground, 1)));
        }
    }

    [Test]
    public void ParseSoundTest()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(AssetSoundKey.Parse("menu-music", null), Is.EqualTo(new MenuMusic()));
            Assert.That(AssetSoundKey.Parse("world-music/10", null), Is.EqualTo(new WorldMusic(10)));
            Assert.That(AssetSoundKey.Parse("ambient-music/10", null), Is.EqualTo(new AmbientMusic(10)));
            Assert.That(AssetSoundKey.Parse("capital-music/human/1", null), Is.EqualTo(new CapitalMusic(WorldRace.Human, 1)));
            Assert.That(AssetSoundKey.Parse("battle-unit-damaged/g000uu0001/1", null), Is.EqualTo(new BattleUnitDamaged(new UnitDefinitionId("G000UU0001"), 1)));
            Assert.That(AssetSoundKey.Parse("battle-effect-sound-death", null), Is.EqualTo(new AssetSoundKey.BattleEffect.Death()));
            Assert.That(AssetSoundKey.Parse("battle-effect-sound-status/shatter-armor", null), Is.EqualTo(new AssetSoundKey.BattleEffect.Status(AssetSoundKey.BattleEffect.StatusType.ShatterArmor)));
            Assert.That(AssetSoundKey.Parse("battle-effect-sound-potion/revive", null), Is.EqualTo(new AssetSoundKey.BattleEffect.Potion(AssetSoundKey.BattleEffect.PotionType.Revive)));
            Assert.That(AssetSoundKey.Parse("stack-unit-attack/g000uu0001", null), Is.EqualTo(new StackUnitAttack(new UnitDefinitionId("G000UU0001"))));
            Assert.That(AssetSoundKey.Parse("ambient/city-in-flames", null), Is.EqualTo(new Ambient(AmbientSoundType.CityInFlames)));
            Assert.That(AssetSoundKey.Parse("ambient-ruin/3", null), Is.EqualTo(new AmbientRuin(3)));
        }
    }

    [Test]
    public void TryParseInvalidTest()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(AssetSpriteKey.TryParse(null, null, out _), Is.False);
            Assert.That(AssetSpriteKey.TryParse("", null, out _), Is.False);
            Assert.That(AssetSpriteKey.TryParse("water", null, out _), Is.False);
            Assert.That(AssetSpriteKey.TryParse("tree/water/0", null, out _), Is.False);
            Assert.That(AssetSpriteKey.TryParse("tree/elf", null, out _), Is.False);
            Assert.That(AssetSpriteKey.TryParse("tree/elf/7/7", null, out _), Is.False);
            Assert.That(AssetSpriteKey.TryParse("tree/elf/-1", null, out _), Is.False);
            Assert.That(AssetSpriteKey.TryParse("tree/elf/24", null, out _), Is.False);
            Assert.That(AssetSpriteKey.TryParse("ground/nowhere", null, out _), Is.False);
            Assert.That(AssetSpriteKey.TryParse("ground/0", null, out _), Is.False);
            Assert.That(AssetSpriteKey.TryParse("road/nx", null, out _), Is.False);
            Assert.That(AssetAnimationKey.TryParse("cloud/4", null, out _), Is.False);
            Assert.That(AssetAnimationKey.TryParse("capital/captured", null, out _), Is.False);
            Assert.That(AssetAnimationKey.TryParse("battle-unit/g000uu0001/paralyzed/attacker", null, out _), Is.False);
            Assert.That(AssetAnimationKey.TryParse("battle-unit-shadow/g000uu0001/paralyzed/attacker", null, out _), Is.False);
            Assert.That(AssetAnimationKey.TryParse("battle-unit-aura/g000uu0001/paralyzed/attacker", null, out _), Is.False);
            Assert.That(AssetSoundKey.TryParse("battle-effect-sound-status/shatterarmor", null, out _), Is.False);
        }
    }

    [Test]
    public void ParseInvalidTest()
    {
        FormatException ex = Assert.NotNull(Assert.Throws<FormatException>(() => AssetSoundKey.Parse("menu-musik", null)));

        Assert.That(ex.Message, Is.EqualTo("The 'menu-musik' asset key is not valid."));
    }

    [Test]
    public void EqualityTest()
    {
        AssetKey body = new AssetAnimationKey.BattleUnit(new UnitDefinitionId("G000UU0002"), BattleUnitPose.Idle, BattleSide.Attacker);
        AssetKey shadow = new BattleUnitShadow(new UnitDefinitionId("G000UU0002"), BattleUnitPose.Idle, BattleSide.Attacker);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                new AssetAnimationKey.BattleUnit(new UnitDefinitionId("G000UU0002"), BattleUnitPose.Idle, BattleSide.Attacker),
                Is.EqualTo(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("g000uu0002"), BattleUnitPose.Idle, BattleSide.Attacker)));
            Assert.That(body, Is.Not.EqualTo(shadow));
            Assert.That(new Cloud(4), Is.EqualTo(new Cloud(1)));
            Assert.That(new City(9), Is.EqualTo(new City(5)));
            Assert.That(new Mountain(WorldRace.Neutral, 4, 0), Is.EqualTo(new Mountain(WorldRace.Neutral, 3, 0)));
        }
    }

    [Test]
    public void RegisteredKeysTest()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(AssetSpriteKey.Categories, Is.EquivalentTo(GetDeclaredCategories(typeof(AssetSpriteKey))));
            Assert.That(AssetAnimationKey.Categories, Is.EquivalentTo(GetDeclaredCategories(typeof(AssetAnimationKey))));
            Assert.That(AssetSoundKey.Categories, Is.EquivalentTo(GetDeclaredCategories(typeof(AssetSoundKey))));
        }
    }

    [Test]
    public void ParseAssetKeyTest()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(AssetKey.Parse("battle-unit/g000uu0001/attack/defender", null), Is.EqualTo(new AssetAnimationKey.BattleUnit(new UnitDefinitionId("G000UU0001"), BattleUnitPose.Attack, BattleSide.Defender)));
            Assert.That(AssetKey.Parse("battle-corpse/large/1", null), Is.EqualTo(new BattleCorpse(IsLarge: true, 1)));
            Assert.That(AssetKey.Parse("menu-music", null), Is.EqualTo(new MenuMusic()));
            Assert.That(AssetKey.TryParse("menu-musik", null, out _), Is.False);
        }
    }

    [Test]
    public void BattleBackgroundListTest()
    {
        List<string> ids = [];

        foreach (BattleBackground background in BattleBackground.ListAll())
        {
            ids.Add(background.CanonicalId);
        }

        Assert.That(ids, Is.EqualTo([
            "battle-background/terrain/human/0",
            "battle-background/terrain/human/1",
            "battle-background/terrain/undead/0",
            "battle-background/terrain/undead/1",
            "battle-background/terrain/heretic/0",
            "battle-background/terrain/heretic/1",
            "battle-background/terrain/heretic/2",
            "battle-background/terrain/dwarf/0",
            "battle-background/terrain/dwarf/1",
            "battle-background/terrain/neutral/0",
            "battle-background/terrain/neutral/1",
            "battle-background/terrain/neutral/2",
            "battle-background/terrain/neutral/3",
            "battle-background/terrain/elf/0",
            "battle-background/terrain/elf/1",
            "battle-background/terrain/water/0",
            "battle-background/boat/0",
            "battle-background/boat/1",
            "battle-background/city/0",
            "battle-background/city/1",
            "battle-background/city/2",
            "battle-background/city/3",
            "battle-background/city/4",
            "battle-background/ruin/0",
            "battle-background/ruin/1",
            "battle-background/ruin/2",
            "battle-background/capital/human",
            "battle-background/capital/undead",
            "battle-background/capital/heretic",
            "battle-background/capital/dwarf",
            "battle-background/capital/elf"
        ]));
    }

    [Test]
    public void CursorCompositionTest()
    {
        List<string> rows = [];

        foreach (CursorType type in Enum.GetValues<CursorType>())
        {
            Cursor cursor = new(type);

            rows.Add($"{cursor.CanonicalId} {cursor.Arrow.CanonicalId} {cursor.Overlay?.CanonicalId ?? "-"}");
        }

        Assert.That(rows, Is.EqualTo([
            "cursor/default cursor/default -",
            "cursor/wait cursor/wait -",
            "cursor/alliance cursor/alliance -",
            "cursor/attack cursor/default -",
            "cursor/negotiate cursor/negotiate -",
            "cursor/enter-city cursor/default cursor/enter-city",
            "cursor/move cursor/default -",
            "cursor/select-unit cursor/select-unit -",
            "cursor/select-city cursor/default cursor/select-city",
            "cursor/view-city cursor/default cursor/view-city",
            "cursor/manage-stack cursor/default cursor/manage-stack",
            "cursor/use-scroll cursor/use-scroll -",
            "cursor/use-scroll-invalid cursor/use-scroll-invalid -",
            "cursor/cast-spell cursor/cast-spell -",
            "cursor/cast-spell-invalid cursor/cast-spell-invalid -",
            "cursor/take-bag cursor/take-bag -",
            "cursor/exchange cursor/exchange -",
            "cursor/visit cursor/default cursor/visit",
            "cursor/drop-bag cursor/drop-bag -",
            "cursor/create-rod cursor/default cursor/create-rod",
            "cursor/create-rod-invalid cursor/default cursor/create-rod-invalid",
            "cursor/destroy-rod cursor/default cursor/destroy-rod",
            "cursor/drag-invalid cursor/drag-invalid -",
            "cursor/dismiss cursor/dismiss -",
            "cursor/dismiss-invalid cursor/dismiss-invalid -",
            "cursor/heal cursor/heal -",
            "cursor/heal-invalid cursor/heal-invalid -",
            "cursor/revive cursor/revive -",
            "cursor/revive-invalid cursor/revive-invalid -",
            "cursor/dynamic-level cursor/dynamic-level -",
            "cursor/dynamic-level-invalid cursor/dynamic-level-invalid -",
            "cursor/battle-default cursor/battle-default -",
            "cursor/battle-attack cursor/battle-attack -",
            "cursor/battle-attack-invalid cursor/battle-attack-invalid -",
            "cursor/battle-heal cursor/battle-heal -",
            "cursor/battle-heal-invalid cursor/battle-heal-invalid -",
            "cursor/battle-transform cursor/battle-transform -",
            "cursor/battle-transform-invalid cursor/battle-transform-invalid -"
        ]));
    }
}
