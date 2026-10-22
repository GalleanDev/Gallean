using System.Globalization;
using System.Runtime.CompilerServices;
using Gallean.Engine.Contracts;
using Gallean.Tests.Common;

namespace Packer.Tests;

public sealed class EnhancementMapTests
{
    private static string GetName(Type type, Type baseType)
    {
        if (type.DeclaringType == baseType)
        {
            return type.Name;
        }
        else
        {
            Type declaring = Assert.NotNull(type.DeclaringType);

            return $"{declaring.Name}.{type.Name}";
        }
    }

    private static List<Type> GetKeyTypes(Type baseType)
    {
        List<Type> types = [];

        foreach (Type nested in baseType.GetNestedTypes())
        {
            if (nested.IsSealed && baseType.IsAssignableFrom(nested))
            {
                types.Add(nested);
            }

            foreach (Type deeper in nested.GetNestedTypes())
            {
                if (deeper.IsSealed && baseType.IsAssignableFrom(deeper))
                {
                    types.Add(deeper);
                }
            }
        }

        return types;
    }

    private static string FormatEnhancements(IReadOnlyList<EnhancementType> enhancements)
    {
        return $"[{string.Join(", ", enhancements)}]";
    }

    private static List<string> FormatMapping(Type baseType, IReadOnlyList<Type> keyedTypes)
    {
        List<string> rows = [];

        foreach (Type type in GetKeyTypes(baseType))
        {
            if (keyedTypes.Contains(type))
            {
                continue;
            }

            AssetKey key = (AssetKey)RuntimeHelpers.GetUninitializedObject(type);
            IReadOnlyList<EnhancementType> enhancements = EnhancementMap.GetEnhancements(key);

            rows.Add($"{GetName(type, baseType)}: {FormatEnhancements(enhancements)}");
        }

        rows.Sort(StringComparer.Ordinal);

        return rows;
    }

    private static List<string> FormatKeyMapping(IReadOnlyList<string> ids)
    {
        List<string> rows = [];

        foreach (string id in ids)
        {
            AssetKey key = AssetKey.Parse(id, CultureInfo.InvariantCulture);
            IReadOnlyList<EnhancementType> enhancements = EnhancementMap.GetEnhancements(key);

            rows.Add($"{id}: {FormatEnhancements(enhancements)}");
        }

        return rows;
    }

    [Test]
    public void SpriteMappingTest()
    {
        Assert.That(FormatMapping(typeof(AssetSpriteKey), []), Is.EqualTo([
            "Bag: [Upscale]",
            "BattleBackground: [Refine]",
            "BattleCorpse: [Upscale, Refine, Alpha, Surface]",
            "BuildingIcon: [Upscale]",
            "CapitalBackground: [Upscale, Refine, Alpha]",
            "CapitalImage: [Upscale]",
            "CityImage: [Upscale]",
            "CityShield: [Upscale]",
            "Ground: [Upscale]",
            "GroundBorder: []",
            "ItemIcon: [Upscale]",
            "LordFace: [Upscale]",
            "MainMenuBackground: [Upscale, Refine, Alpha]",
            "MainMenuBackgroundV2: []",
            "Mountain: [Upscale]",
            "RaceSelectionBackground: [Upscale, Refine, Alpha]",
            "Road: [Upscale]",
            "Rod: [Upscale]",
            "RuinBanner: [Upscale]",
            "SiteImage: [Upscale]",
            "SpellIcon: [Upscale]",
            "StackBanner: [Upscale]",
            "ThiefActionImage: [Upscale]",
            "Tombstone: [Upscale]",
            "Tree: [Upscale]",
            "UnitEventPortrait: [Upscale]",
            "UnitFace: [Upscale]",
            "UnitPortrait: [Upscale]"
        ]));
    }

    [Test]
    public void AnimationMappingTest()
    {
        IReadOnlyList<Type> effects =
        [
            typeof(AssetAnimationKey.BattleEffect.Attack),
            typeof(AssetAnimationKey.BattleEffect.Blister),
            typeof(AssetAnimationKey.BattleEffect.Death),
            typeof(AssetAnimationKey.BattleEffect.Heal),
            typeof(AssetAnimationKey.BattleEffect.Item),
            typeof(AssetAnimationKey.BattleEffect.LevelUp),
            typeof(AssetAnimationKey.BattleEffect.Status),
            typeof(AssetAnimationKey.BattleUnitAura)
        ];

        Assert.That(FormatMapping(typeof(AssetAnimationKey), effects), Is.EqualTo([
            "BattleBackgroundOverlay: []",
            "BattleUnit: [Upscale, Refine, Alpha, Surface]",
            "BattleUnitShadow: [Upscale]",
            "Capital: [Upscale]",
            "CapitalBackgroundOverlay: [Upscale, Refine, Alpha]",
            "CapitalBuilding: [Upscale, Refine, Alpha]",
            "City: [Upscale]",
            "CityShadow: [Upscale]",
            "Cloud: [Upscale]",
            "Crystal: [Upscale]",
            "Cursor: [Upscale, Refine, Alpha]",
            "Landmark: [Upscale]",
            "MainMenuBackgroundOverlay: [Upscale, Refine, Alpha]",
            "MountainRemoveEffect: [Upscale]",
            "RaceSelectionBackgroundOverlay: [Upscale, Refine, Alpha]",
            "Ruin: [Upscale]",
            "Site: [Upscale]",
            "SiteBanner: [Upscale]",
            "SiteShadow: [Upscale]",
            "StackBattleEffect: [Upscale]",
            "StackBoat: [Upscale]",
            "StackBoatShadow: [Upscale]",
            "StackCreateEffect: [Upscale]",
            "StackDestroyEffect: [Upscale]",
            "StackOwnerChangeEffect: [Upscale]",
            "StackSummonDestroyEffect: [Upscale]",
            "StackUnit: [Upscale]",
            "StackUnitAura: [Upscale]",
            "StackUnitShadow: [Upscale]",
            "Water: []",
            "WaterLandmark: [Upscale]",
            "WorldSpellEffect: [Upscale]"
        ]));
    }

    [Test]
    public void AttackLayerMappingTest()
    {
        List<string> rows = [];

        foreach (AssetAnimationKey.BattleLayer layer in Enum.GetValues<AssetAnimationKey.BattleLayer>())
        {
            AssetAnimationKey.BattleEffect.Attack key = new(
                new UnitDefinitionId("g000uu0154"),
                AssetAnimationKey.BattleEffect.AttackType.Touch,
                layer,
                AssetAnimationKey.BattleEffect.AttackSide.Defend,
                0);

            IReadOnlyList<EnhancementType> enhancements = EnhancementMap.GetEnhancements(key);

            rows.Add($"{layer}: {FormatEnhancements(enhancements)}");
        }

        Assert.That(rows, Is.EqualTo([
            "Shadow: [Upscale]",
            "Body: [Upscale, Refine, Alpha]",
            "Aura: [Upscale, Refine, Alpha]"
        ]));
    }

    [Test]
    public void EffectMappingTest()
    {
        IReadOnlyList<string> ids =
        [
            "battle-effect-attack/g000uu0154/touch/body/defend/0",
            "battle-effect-attack/g000uu0154/touch/aura/defend/0",
            "battle-effect-blister",
            "battle-effect-death/elf/small",
            "battle-effect-heal",
            "battle-effect-item/potion",
            "battle-effect-level-up/heretic/large",
            "battle-effect-level-up/heretic/small",
            "battle-effect-status/blister/large",
            "battle-unit-aura/g000uu0066/attack/attacker",
            "battle-unit-aura/g000uu5013/attack/defender"
        ];

        Assert.That(FormatKeyMapping(ids), Is.EqualTo([
            "battle-effect-attack/g000uu0154/touch/body/defend/0: [Upscale, Refine, Alpha]",
            "battle-effect-attack/g000uu0154/touch/aura/defend/0: [Upscale, Refine, Alpha]",
            "battle-effect-blister: [Upscale, Refine, Alpha]",
            "battle-effect-death/elf/small: [Upscale, Refine, Alpha]",
            "battle-effect-heal: [Upscale]",
            "battle-effect-item/potion: [Upscale, Refine, Alpha]",
            "battle-effect-level-up/heretic/large: [Upscale]",
            "battle-effect-level-up/heretic/small: [Upscale, Refine, Alpha]",
            "battle-effect-status/blister/large: [Upscale, Refine, Alpha]",
            "battle-unit-aura/g000uu0066/attack/attacker: [Upscale, Refine, Alpha]",
            "battle-unit-aura/g000uu5013/attack/defender: [Upscale]"
        ]));
    }

    [Test]
    public void GroundBorderSetMappingTest()
    {
        List<string> rows = [];

        foreach (AssetSpriteKey.GroundBorderSet set in Enum.GetValues<AssetSpriteKey.GroundBorderSet>())
        {
            AssetSpriteKey.GroundBorder key = new(set, new AssetSpriteKey.GroundBorderShape.Edges(AssetSpriteKey.GroundBorderEdges.North), 0);

            IReadOnlyList<EnhancementType> enhancements = EnhancementMap.GetEnhancements(key);

            rows.Add($"{set}: {FormatEnhancements(enhancements)}");
        }

        Assert.That(rows, Is.EqualTo([
            "Mask: []",
            "Coast: [Upscale]"
        ]));
    }

    [Test]
    public void OutputEnhancementsTest()
    {
        IReadOnlyList<EnhancementType> enhancements = [EnhancementType.Upscale, EnhancementType.Refine, EnhancementType.Alpha, EnhancementType.Surface];

        Assert.That(
            EnhancementMap.GetOutputEnhancements(enhancements, EnhancementType.Upscale),
            Is.EqualTo([EnhancementType.Upscale]));
        Assert.That(
            EnhancementMap.GetOutputEnhancements(enhancements, EnhancementType.Refine),
            Is.EqualTo([EnhancementType.Upscale, EnhancementType.Refine]));
        Assert.That(
            EnhancementMap.GetOutputEnhancements(enhancements, EnhancementType.Surface),
            Is.EqualTo([EnhancementType.Upscale, EnhancementType.Refine, EnhancementType.Alpha, EnhancementType.Surface]));
    }

    [Test]
    public void OutputEnhancementsGapTest()
    {
        Assert.That(
            EnhancementMap.GetOutputEnhancements([EnhancementType.Refine], EnhancementType.Refine),
            Is.EqualTo([EnhancementType.Refine]));
        Assert.That(
            EnhancementMap.GetOutputEnhancements([EnhancementType.Upscale, EnhancementType.Alpha], EnhancementType.Alpha),
            Is.EqualTo([EnhancementType.Upscale, EnhancementType.Alpha]));
    }

    [Test]
    public void InputEnhancementsTest()
    {
        IReadOnlyList<EnhancementType> enhancements = [EnhancementType.Upscale, EnhancementType.Refine, EnhancementType.Alpha, EnhancementType.Surface];

        Assert.That(EnhancementMap.GetInputEnhancements(enhancements, EnhancementType.Upscale), Is.Empty);
        Assert.That(
            EnhancementMap.GetInputEnhancements(enhancements, EnhancementType.Refine),
            Is.EqualTo([EnhancementType.Upscale]));
        Assert.That(
            EnhancementMap.GetInputEnhancements(enhancements, EnhancementType.Surface),
            Is.EqualTo([EnhancementType.Upscale, EnhancementType.Refine, EnhancementType.Alpha]));
    }

    [Test]
    public void InputEnhancementsGapTest()
    {
        Assert.That(EnhancementMap.GetInputEnhancements([EnhancementType.Refine], EnhancementType.Refine), Is.Empty);
        Assert.That(
            EnhancementMap.GetInputEnhancements([EnhancementType.Upscale, EnhancementType.Alpha], EnhancementType.Alpha),
            Is.EqualTo([EnhancementType.Upscale]));
    }

    [Test]
    public void MissingEnhancementTest()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EnhancementMap.GetOutputEnhancements([EnhancementType.Refine], EnhancementType.Alpha));
        Assert.Throws<ArgumentOutOfRangeException>(() => EnhancementMap.GetInputEnhancements([EnhancementType.Refine], EnhancementType.Alpha));
    }
}
