using Gallean.Engine.Contracts;

namespace Packer;

internal static class SpriteFilters
{
    public static IReadOnlyList<SpriteFilterRule> Rules { get; } =
    [
        new(new ClearFilter(),
        [
            "battle-unit-aura/g000uu6001/idle/attacker",
            "battle-unit-aura/g000uu6001/hit/attacker",
            "battle-unit-aura/g000uu6101/idle/attacker",
            "battle-unit-aura/g000uu6101/hit/attacker"
        ]),

        new(new CropFilter(left: 320, top: 380, right: 500, bottom: 520),
        [
            "battle-unit/g000uu0028/attack/defender"
        ]),

        new(new CropFilter(left: 340, top: 460, right: 480, bottom: 540),
        [
            "battle-unit-shadow/g000uu0028/idle/attacker"
        ]),

        new(new AdditiveTrimFilter(margin: 1),
        [
            "battle-unit*",
            "battle-effect-*"
        ]),

        new(new AdditiveTrimFilter(margin: 0),
        [
            "battle-corpse",
            "stack-*",
            "mountain-remove-effect",
            "world-spell-effect"
        ]),

        new(new AdditiveFrameFilter(),
        [
            "*"
        ]),

        new(new MarginFilter(margin: 3),
        [
            "cursor"
        ]),

        new(new CursorFilter(),
        [
            "cursor"
        ]),

        new(new FlattenFilter(),
        [
            "main-menu-background",
            "race-selection-background"
        ]),

        new(new UnitShadowFilter(),
        [
            "battle-unit-shadow",
            "stack-unit-shadow"
        ]),

        new(new GroundBorderMaskFilter(),
        [
            "ground-border/mask"
        ]),

        new(new GroundBorderMaskEdgeFilter(),
        [
            "ground-border/mask/edge"
        ]),

        new(new GroundBorderCoastFilter(),
        [
            "ground-border/coast"
        ]),

        new(new GroundBorderFilter(),
        [
            "ground-border"
        ]),

        new(new WaterTintFilter(),
        [
            "water-landmark"
        ]),

        new(new SpellIconFilter(),
        [
            "spell-icon"
        ]),

        new(new ShadowBlurFilter(0.5),
        [
            "stack-boat-shadow",
            "stack-unit-shadow/g000uu5126",
            "stack-unit-shadow/g000uu5127",
            "stack-unit-shadow/g000uu5129"
        ]),

        new(new ShadowBlurFilter(1.0),
        [
            "battle-unit-shadow"
        ]),

        new(new ShadowBlurFilter(0.7),
        [
            "stack-unit-shadow",
            "city-shadow",
            "site-shadow"
        ]),

        new(new MainMenuBackgroundOverlayFilter(),
        [
            "main-menu-background-overlay"
        ]),

        new(new CapitalBuildingFilter(new AssetSpriteKey.CapitalBackground(WorldRace.Elf, 0)),
        [
            "capital-building/g000bb01*"
        ]),

        new(new AlphaContourFilter(),
        [
            "battle-unit",
            "battle-corpse",
            "cursor"
        ])
    ];

    public static SpriteFilterPlan CreatePlan(string id, AssetSpriteMetadata metadata, IReadOnlyList<AssetTransform> currentTransforms)
    {
        List<ISpriteFilter> filters = [];

        List<AssetTransform> result = [];
        result.AddRange(currentTransforms);

        foreach (SpriteFilterRule rule in Rules)
        {
            if (rule.Selector.Matches(id) &&
                rule.Filter.Accepts(metadata) &&
                !HasTransform(result, rule.Filter.Type))
            {
                filters.Add(rule.Filter);
                result.Add(new AssetTransform(rule.Filter.Type, metadata.PixelScale));
            }
        }

        return new SpriteFilterPlan(filters, result);
    }

    private static bool HasTransform(IReadOnlyList<AssetTransform> transforms, Guid type)
    {
        foreach (AssetTransform transform in transforms)
        {
            if (transform.Type == type)
            {
                return true;
            }
        }

        return false;
    }
}
