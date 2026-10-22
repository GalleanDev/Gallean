using System.Runtime.CompilerServices;

namespace Packer.Tests;

public sealed class UpscaleMapTests
{
    private static List<string> FormatKeys(Type baseType, UpscalerType upscaler)
    {
        List<string> rows = [];

        foreach (Type nested in baseType.GetNestedTypes())
        {
            foreach (Type type in (Type[])[nested, .. nested.GetNestedTypes()])
            {
                if (type.IsSealed && baseType.IsAssignableFrom(type))
                {
                    AssetKey key = (AssetKey)RuntimeHelpers.GetUninitializedObject(type);

                    if (UpscaleMap.GetUpscaler(key) == upscaler)
                    {
                        rows.Add(type.Name);
                    }
                }
            }
        }

        rows.Sort(StringComparer.Ordinal);

        return rows;
    }

    [Test]
    public void VsrSpritesTest()
    {
        Assert.That(FormatKeys(typeof(AssetSpriteKey), UpscalerType.Vsr), Is.EqualTo([
            "MainMenuBackground",
            "RaceSelectionBackground"
        ]));
    }

    [Test]
    public void VsrAnimationsTest()
    {
        Assert.That(FormatKeys(typeof(AssetAnimationKey), UpscalerType.Vsr), Is.EqualTo([
            "Attack",
            "BattleUnit",
            "BattleUnitAura",
            "Blister",
            "CapitalBackgroundOverlay",
            "CapitalBuilding",
            "Cursor",
            "Death",
            "Heal",
            "Item",
            "LevelUp",
            "MainMenuBackgroundOverlay",
            "RaceSelectionBackgroundOverlay",
            "Status"
        ]));
    }

    [Test]
    public void TransformTest()
    {
        Assert.That(UpscaleMap.GetTransform(UpscalerType.Vsr), Is.EqualTo(new Guid("7638e1d7-13d7-4ed4-abbb-d514797df049")));
        Assert.That(UpscaleMap.GetTransform(UpscalerType.Mosr), Is.EqualTo(new Guid("b3d1f5a7-6c24-4e93-8f70-2a5c9de41b68")));
    }
}
