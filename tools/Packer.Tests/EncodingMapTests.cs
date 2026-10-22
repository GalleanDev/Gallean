using System.Runtime.CompilerServices;
using Gallean.Tests.Common;

namespace Packer.Tests;

public sealed class EncodingMapTests
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

    private static List<string> FormatMapping(Type baseType)
    {
        List<string> rows = [];

        foreach (Type type in GetKeyTypes(baseType))
        {
            AssetKey key = (AssetKey)RuntimeHelpers.GetUninitializedObject(type);

            rows.Add($"{GetName(type, baseType)}: {EncodingMap.GetEncoding(key)}");
        }

        rows.Sort(StringComparer.Ordinal);

        return rows;
    }

    [Test]
    public void SpriteMappingTest()
    {
        Assert.That(FormatMapping(typeof(AssetSpriteKey)), Is.EqualTo([
            "Bag: Palette",
            "BattleBackground: None",
            "BattleCorpse: Palette",
            "BuildingIcon: Palette",
            "CapitalBackground: None",
            "CapitalImage: None",
            "CityImage: None",
            "CityShield: Palette",
            "Ground: Palette",
            "GroundBorder: Palette",
            "ItemIcon: None",
            "LordFace: None",
            "MainMenuBackground: None",
            "MainMenuBackgroundV2: None",
            "Mountain: Palette",
            "RaceSelectionBackground: None",
            "Road: Palette",
            "Rod: Palette",
            "RuinBanner: Palette",
            "SiteImage: None",
            "SpellIcon: None",
            "StackBanner: Palette",
            "ThiefActionImage: None",
            "Tombstone: Palette",
            "Tree: Palette",
            "UnitEventPortrait: None",
            "UnitFace: None",
            "UnitPortrait: None"
        ]));
    }

    [Test]
    public void AnimationMappingTest()
    {
        Assert.That(FormatMapping(typeof(AssetAnimationKey)), Is.EqualTo([
            "BattleBackgroundOverlay: None",
            "BattleEffect.Attack: Lossy",
            "BattleEffect.Blister: Lossy",
            "BattleEffect.Death: Lossy",
            "BattleEffect.Heal: Lossy",
            "BattleEffect.Item: Lossy",
            "BattleEffect.LevelUp: Lossy",
            "BattleEffect.Status: Lossy",
            "BattleUnit: Lossy",
            "BattleUnitAura: Lossy",
            "BattleUnitShadow: Palette",
            "Capital: Palette",
            "CapitalBackgroundOverlay: None",
            "CapitalBuilding: None",
            "City: Palette",
            "CityShadow: Palette",
            "Cloud: Palette",
            "Crystal: Palette",
            "Cursor: None",
            "Landmark: Palette",
            "MainMenuBackgroundOverlay: None",
            "MountainRemoveEffect: None",
            "RaceSelectionBackgroundOverlay: None",
            "Ruin: Palette",
            "Site: Palette",
            "SiteBanner: Palette",
            "SiteShadow: Palette",
            "StackBattleEffect: None",
            "StackBoat: Palette",
            "StackBoatShadow: Palette",
            "StackCreateEffect: None",
            "StackDestroyEffect: None",
            "StackOwnerChangeEffect: None",
            "StackSummonDestroyEffect: None",
            "StackUnit: Palette",
            "StackUnitAura: None",
            "StackUnitShadow: Palette",
            "Water: Palette",
            "WaterLandmark: Palette",
            "WorldSpellEffect: Lossy"
        ]));
    }
}
