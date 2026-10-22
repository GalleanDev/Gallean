using Gallean.Engine.Contracts;
using GameContent;

namespace Packer.Tests;

public sealed class SpriteFiltersTests
{
    private static List<string> FormatPlan(SpriteFilterPlan plan)
    {
        List<string> rows = [];

        foreach (ISpriteFilter filter in plan.Filters)
        {
            rows.Add($"filter {filter.GetType().Name}");
        }

        foreach (AssetTransform transform in plan.Transforms)
        {
            rows.Add($"transform {transform.Type:N} x{transform.Scale}");
        }

        return rows;
    }

    [Test]
    public void CategoriesTest()
    {
        IReadOnlyList<GameAssetDescriptor> entries = GameAssetCatalog.ListDescriptors(new GameAssetInputs(
            [new UnitDefinitionId("G000UU0001")],
            [],
            [new WorldSpellDefinitionId("G000SS0001")],
            [new WorldPlayerBuildingDefinitionId("G000BB0001")],
            [new WorldLandmarkDefinitionId("G000MG0004")],
            [new ItemDefinitionId("G000IG0001")],
            []));

        List<string> unmatched = [];

        foreach (SpriteFilterRule rule in SpriteFilters.Rules)
        {
            foreach (string category in rule.Categories)
            {
                CategorySelector selector = CategorySelector.FromPatterns([category.Split('/')[0]]);

                if (!entries.Any(entry => selector.Matches(entry.Key.CanonicalId)))
                {
                    unmatched.Add(category);
                }
            }
        }

        Assert.That(unmatched, Is.Empty);
    }

    [Test]
    public void FilterTypesTest()
    {
        List<string> rows = [];

        foreach (SpriteFilterRule rule in SpriteFilters.Rules)
        {
            rows.Add($"{rule.Filter.GetType().Name} {rule.Filter.Type:N}");
        }

        Assert.That(rows, Is.EqualTo([
            "ClearFilter 5dfbc6a69c9d4a13ac5d2b07786caca9",
            "CropFilter 601ca3e6978d4ee7a7ec4cf06b1790e1",
            "CropFilter 601ca3e6978d4ee7a7ec4cf06b1790e1",
            "AdditiveTrimFilter 73de3ef1c6ca4304be0946bbc27d5446",
            "AdditiveTrimFilter 73de3ef1c6ca4304be0946bbc27d5446",
            "AdditiveFrameFilter 268df81e71734aac8653a10cb1e295a5",
            "MarginFilter c4e0a6b28f3147da9b051de8f2a7c634",
            "CursorFilter e3eb087263574c51ab3093a2db931dc5",
            "FlattenFilter 1cbf315dc76d4318b158a55762d323f1",
            "UnitShadowFilter b3471958d5db4d7ab82f400eb5bd8e8b",
            "GroundBorderMaskFilter bada57c5173346809535380c4ef4a4ac",
            "GroundBorderMaskEdgeFilter a6c4beb5ecfb4b7e8773fa7f134317c3",
            "GroundBorderCoastFilter 291064aee5f44ad29eb0f98177561846",
            "GroundBorderFilter 26b5f3ec4d764b41ae3f6d56cc366e1e",
            "WaterTintFilter e46d70f814844e078e6b94ba9597ada2",
            "SpellIconFilter 8f3b2c61d47e4a19b5c81e6a9d07f2b4",
            "ShadowBlurFilter b7c9f1a43d524e189f0c2a6b5d8e4713",
            "ShadowBlurFilter b7c9f1a43d524e189f0c2a6b5d8e4713",
            "ShadowBlurFilter b7c9f1a43d524e189f0c2a6b5d8e4713",
            "MainMenuBackgroundOverlayFilter 5a0e3c718b2d4f96a1c47d93e6b20f58",
            "CapitalBuildingFilter e6466f70621e44cb903c71e1834c3310",
            "AlphaContourFilter 5d1e8a3792c44b6fa0e37c4f18b2d965"
        ]));
    }

    [Test]
    public void WaterLandmarkTest()
    {
        SpriteFilterPlan plan = SpriteFilters.CreatePlan(
            "water-landmark/g000mg0004",
            new AssetSpriteMetadata(32, 32, 32, 32, 0, 0, Additive: false),
            []);

        List<string> rows = FormatPlan(plan);

        Assert.That(rows, Is.EqualTo([
            "filter WaterTintFilter",
            "transform e46d70f814844e078e6b94ba9597ada2 x1"
        ]));
    }

    [Test]
    public void UnfilteredCategoriesTest()
    {
        AssetSpriteMetadata metadata = new(32, 32, 32, 32, 0, 0, Additive: false);

        SpriteFilterPlan ground = SpriteFilters.CreatePlan("ground/plain/0/0", metadata, []);
        SpriteFilterPlan face = SpriteFilters.CreatePlan("unit-face/g000uu0001", metadata, []);
        SpriteFilterPlan aura = SpriteFilters.CreatePlan("battle-unit-aura/g000uu0001/idle/attacker", metadata, []);
        SpriteFilterPlan unit = SpriteFilters.CreatePlan("battle-unit/g000uu0001/idle/attacker", metadata, []);
        SpriteFilterPlan landmark = SpriteFilters.CreatePlan("landmark/g000mg0004", metadata, []);

        Assert.That(FormatPlan(ground), Is.Empty);
        Assert.That(FormatPlan(face), Is.Empty);
        Assert.That(FormatPlan(aura), Is.Empty);
        Assert.That(FormatPlan(unit), Is.Empty);
        Assert.That(FormatPlan(landmark), Is.Empty);
    }

    [Test]
    public void UpscaledUnitTest()
    {
        SpriteFilterPlan plan = SpriteFilters.CreatePlan(
            "battle-unit/g000uu0001/idle/attacker",
            new AssetSpriteMetadata(32, 32, 64, 64, 0, 0, Additive: false),
            []);

        List<string> rows = FormatPlan(plan);

        Assert.That(rows, Is.EqualTo([
            "filter AlphaContourFilter",
            "transform 5d1e8a3792c44b6fa0e37c4f18b2d965 x2"
        ]));
    }

    [Test]
    public void UpscaledCorpseTest()
    {
        SpriteFilterPlan plan = SpriteFilters.CreatePlan(
            "battle-corpse/small/1",
            new AssetSpriteMetadata(32, 32, 64, 64, 0, 0, Additive: false),
            []);

        List<string> rows = FormatPlan(plan);

        Assert.That(rows, Is.EqualTo([
            "filter AlphaContourFilter",
            "transform 5d1e8a3792c44b6fa0e37c4f18b2d965 x2"
        ]));
    }

    [Test]
    public void SourceCursorTest()
    {
        SpriteFilterPlan plan = SpriteFilters.CreatePlan(
            "cursor/enter-city",
            new AssetSpriteMetadata(32, 32, 32, 32, 0, 0, Additive: false),
            []);

        List<string> rows = FormatPlan(plan);

        Assert.That(rows, Is.EqualTo([
            "filter MarginFilter",
            "filter CursorFilter",
            "transform c4e0a6b28f3147da9b051de8f2a7c634 x1",
            "transform e3eb087263574c51ab3093a2db931dc5 x1"
        ]));
    }

    [Test]
    public void UpscaledCursorTest()
    {
        SpriteFilterPlan plan = SpriteFilters.CreatePlan(
            "cursor/default",
            new AssetSpriteMetadata(32, 32, 64, 64, 0, 0, Additive: false),
            []);

        List<string> rows = FormatPlan(plan);

        Assert.That(rows, Is.EqualTo([
            "filter AlphaContourFilter",
            "transform 5d1e8a3792c44b6fa0e37c4f18b2d965 x2"
        ]));
    }

    [Test]
    public void AppliedAlphaContourTest()
    {
        SpriteFilterPlan plan = SpriteFilters.CreatePlan(
            "battle-unit/g000uu0001/idle/attacker",
            new AssetSpriteMetadata(32, 32, 128, 128, 0, 0, Additive: false),
            [new AssetTransform(new Guid("5d1e8a37-92c4-4b6f-a0e3-7c4f18b2d965"), 2)]);

        List<string> rows = FormatPlan(plan);

        Assert.That(rows, Is.EqualTo([
            "transform 5d1e8a3792c44b6fa0e37c4f18b2d965 x2"
        ]));
    }

    [Test]
    public void UnitShadowTest()
    {
        SpriteFilterPlan plan = SpriteFilters.CreatePlan(
            "stack-unit-shadow/g000uu0001/idle/3",
            new AssetSpriteMetadata(32, 32, 64, 64, 0, 0, Additive: false),
            []);

        List<string> rows = FormatPlan(plan);

        Assert.That(rows, Is.EqualTo([
            "filter ShadowBlurFilter",
            "transform b7c9f1a43d524e189f0c2a6b5d8e4713 x2"
        ]));
    }

    [Test]
    public void SourceUnitShadowTest()
    {
        SpriteFilterPlan plan = SpriteFilters.CreatePlan(
            "stack-unit-shadow/g000uu0001/idle/3",
            new AssetSpriteMetadata(32, 32, 32, 32, 0, 0, Additive: false),
            []);

        List<string> rows = FormatPlan(plan);

        Assert.That(rows, Is.EqualTo([
            "filter UnitShadowFilter",
            "transform b3471958d5db4d7ab82f400eb5bd8e8b x1"
        ]));
    }

    [Test]
    public void CroppedUnitShadowTest()
    {
        SpriteFilterPlan plan = SpriteFilters.CreatePlan(
            "battle-unit-shadow/g000uu0028/idle/attacker",
            new AssetSpriteMetadata(32, 32, 32, 32, 0, 0, Additive: false),
            []);

        List<string> rows = FormatPlan(plan);

        Assert.That(rows, Is.EqualTo([
            "filter CropFilter",
            "filter UnitShadowFilter",
            "transform 601ca3e6978d4ee7a7ec4cf06b1790e1 x1",
            "transform b3471958d5db4d7ab82f400eb5bd8e8b x1"
        ]));
    }

    [Test]
    public void WaterOnlyUnitShadowTest()
    {
        SpriteFilterPlan plan = SpriteFilters.CreatePlan(
            "stack-unit-shadow/g000uu5126/idle/3",
            new AssetSpriteMetadata(32, 32, 64, 64, 0, 0, Additive: false),
            []);

        List<string> rows = FormatPlan(plan);

        Assert.That(rows, Is.EqualTo([
            "filter ShadowBlurFilter",
            "transform b7c9f1a43d524e189f0c2a6b5d8e4713 x2"
        ]));
    }

    [Test]
    public void AppliedShadowBlurTest()
    {
        SpriteFilterPlan plan = SpriteFilters.CreatePlan(
            "city-shadow/2",
            new AssetSpriteMetadata(32, 32, 128, 128, 0, 0, Additive: false),
            [new AssetTransform(new Guid("b7c9f1a4-3d52-4e18-9f0c-2a6b5d8e4713"), 2)]);

        List<string> rows = FormatPlan(plan);

        Assert.That(rows, Is.EqualTo([
            "transform b7c9f1a43d524e189f0c2a6b5d8e4713 x2"
        ]));
    }

    [Test]
    public void AppliedWaterTintTest()
    {
        SpriteFilterPlan plan = SpriteFilters.CreatePlan(
            "water-landmark/g000mg0004",
            new AssetSpriteMetadata(32, 32, 32, 32, 0, 0, Additive: false),
            [new AssetTransform(new Guid("e46d70f8-1484-4e07-8e6b-94ba9597ada2"), 1)]);

        List<string> rows = FormatPlan(plan);

        Assert.That(rows, Is.EqualTo([
            "transform e46d70f814844e078e6b94ba9597ada2 x1"
        ]));
    }

    [Test]
    public void RecordedTransformsTest()
    {
        SpriteFilterPlan plan = SpriteFilters.CreatePlan(
            "water-landmark/g000mg0004",
            new AssetSpriteMetadata(32, 32, 64, 64, 0, 0, Additive: false),
            []);

        List<string> rows = FormatPlan(plan);

        Assert.That(rows, Is.EqualTo([
            "filter WaterTintFilter",
            "transform e46d70f814844e078e6b94ba9597ada2 x2"
        ]));
    }

    [Test]
    public void KeptTransformsTest()
    {
        SpriteFilterPlan plan = SpriteFilters.CreatePlan(
            "water-landmark/g000mg0004",
            new AssetSpriteMetadata(32, 32, 64, 64, 0, 0, Additive: false),
            [
                new AssetTransform(new Guid("f3a1c0d2-6b47-4c8e-9a55-0d1e2f3a4b5c"), 1)
            ]);

        List<string> rows = FormatPlan(plan);

        Assert.That(rows, Is.EqualTo([
            "filter WaterTintFilter",
            "transform f3a1c0d26b474c8e9a550d1e2f3a4b5c x1",
            "transform e46d70f814844e078e6b94ba9597ada2 x2"
        ]));
    }

    [Test]
    public void AdditiveTest()
    {
        SpriteFilterPlan plan = SpriteFilters.CreatePlan(
            "battle-effect-attack/g000uu0001/hit/body/front/0",
            new AssetSpriteMetadata(32, 32, 32, 32, 0, 0, Additive: true),
            []);

        List<string> rows = FormatPlan(plan);

        Assert.That(rows, Is.EqualTo([
            "filter AdditiveTrimFilter",
            "filter AdditiveFrameFilter",
            "transform 73de3ef1c6ca4304be0946bbc27d5446 x1",
            "transform 268df81e71734aac8653a10cb1e295a5 x1"
        ]));
    }

    [Test]
    public void UpscaledAdditiveTest()
    {
        SpriteFilterPlan plan = SpriteFilters.CreatePlan(
            "battle-effect-attack/g000uu0001/hit/body/front/0",
            new AssetSpriteMetadata(32, 32, 64, 64, 0, 0, Additive: true),
            []);

        Assert.That(FormatPlan(plan), Is.Empty);
    }

    [Test]
    public void FlattenTest()
    {
        SpriteFilterPlan plan = SpriteFilters.CreatePlan(
            "race-selection-background/elf",
            new AssetSpriteMetadata(800, 600, 800, 600, 0, 0, Additive: false),
            []);

        List<string> rows = FormatPlan(plan);

        Assert.That(rows, Is.EqualTo([
            "filter FlattenFilter",
            "transform 1cbf315dc76d4318b158a55762d323f1 x1"
        ]));
    }

    [Test]
    public void GroundBorderMaskEdgeTest()
    {
        SpriteFilterPlan plan = SpriteFilters.CreatePlan(
            "ground-border/mask/edge/n/0",
            new AssetSpriteMetadata(64, 32, 64, 32, 0, 0, Additive: false),
            []);

        List<string> rows = FormatPlan(plan);

        Assert.That(rows, Is.EqualTo([
            "filter GroundBorderMaskFilter",
            "filter GroundBorderMaskEdgeFilter",
            "filter GroundBorderFilter",
            "transform bada57c5173346809535380c4ef4a4ac x1",
            "transform a6c4beb5ecfb4b7e8773fa7f134317c3 x1",
            "transform 26b5f3ec4d764b41ae3f6d56cc366e1e x1"
        ]));
    }

    [Test]
    public void GroundBorderMaskCornerTest()
    {
        SpriteFilterPlan plan = SpriteFilters.CreatePlan(
            "ground-border/mask/corner/nw/0",
            new AssetSpriteMetadata(64, 32, 64, 32, 0, 0, Additive: false),
            []);

        List<string> rows = FormatPlan(plan);

        Assert.That(rows, Is.EqualTo([
            "filter GroundBorderMaskFilter",
            "filter GroundBorderFilter",
            "transform bada57c5173346809535380c4ef4a4ac x1",
            "transform 26b5f3ec4d764b41ae3f6d56cc366e1e x1"
        ]));
    }

    [Test]
    public void GroundBorderCoastTest()
    {
        SpriteFilterPlan plan = SpriteFilters.CreatePlan(
            "ground-border/coast/edge/n/0",
            new AssetSpriteMetadata(64, 32, 64, 32, 0, 0, Additive: false),
            []);

        List<string> rows = FormatPlan(plan);

        Assert.That(rows, Is.EqualTo([
            "filter GroundBorderCoastFilter",
            "filter GroundBorderFilter",
            "transform 291064aee5f44ad29eb0f98177561846 x1",
            "transform 26b5f3ec4d764b41ae3f6d56cc366e1e x1"
        ]));
    }

    [Test]
    public void MainMenuOverlayTest()
    {
        SpriteFilterPlan source = SpriteFilters.CreatePlan(
            "main-menu-background-overlay/0",
            new AssetSpriteMetadata(187, 112, 187, 112, 73, 321, Additive: false),
            []);

        SpriteFilterPlan upscaled = SpriteFilters.CreatePlan(
            "main-menu-background-overlay/0",
            new AssetSpriteMetadata(187, 112, 748, 448, 73, 321, Additive: false),
            []);

        Assert.That(FormatPlan(source), Is.Empty);

        Assert.That(FormatPlan(upscaled), Is.EqualTo([
            "filter MainMenuBackgroundOverlayFilter",
            "transform 5a0e3c718b2d4f96a1c47d93e6b20f58 x4"
        ]));
    }

    [Test]
    public void CapitalBuildingTest()
    {
        SpriteFilterPlan source = SpriteFilters.CreatePlan(
            "capital-building/g000bb0100",
            new AssetSpriteMetadata(117, 90, 117, 90, 274, 475, Additive: false),
            []);

        SpriteFilterPlan upscaled = SpriteFilters.CreatePlan(
            "capital-building/g000bb0100",
            new AssetSpriteMetadata(117, 90, 468, 360, 274, 475, Additive: false),
            []);

        SpriteFilterPlan human = SpriteFilters.CreatePlan(
            "capital-building/g000bb0001",
            new AssetSpriteMetadata(123, 115, 492, 460, 517, 485, Additive: false),
            []);

        Assert.That(FormatPlan(source), Is.Empty);

        Assert.That(FormatPlan(upscaled), Is.EqualTo([
            "filter CapitalBuildingFilter",
            "transform e6466f70621e44cb903c71e1834c3310 x4"
        ]));

        Assert.That(FormatPlan(human), Is.Empty);
    }

    [Test]
    public void SpellIconTest()
    {
        SpriteFilterPlan plan = SpriteFilters.CreatePlan(
            "spell-icon/g000ss0001",
            new AssetSpriteMetadata(70, 85, 70, 85, 0, 0, Additive: false),
            []);

        List<string> rows = FormatPlan(plan);

        Assert.That(rows, Is.EqualTo([
            "filter SpellIconFilter",
            "transform 8f3b2c61d47e4a19b5c81e6a9d07f2b4 x1"
        ]));
    }
}
