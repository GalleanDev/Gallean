namespace Packer;

internal sealed record SpriteFilterRule(ISpriteFilter Filter, IReadOnlyList<string> Categories)
{
    public CategorySelector Selector { get; } = CategorySelector.FromPatterns(Categories);
}
