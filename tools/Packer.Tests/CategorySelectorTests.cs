using Gallean.Engine.Contracts;
using Gallean.Tests.Common;
using GameContent;

namespace Packer.Tests;

public sealed class CategorySelectorTests
{
    [Test]
    public void WholeCategoryTest()
    {
        CategorySelector selector = CategorySelector.FromPatterns(["battle-unit"]);

        Assert.That(selector.Matches("battle-unit/g000uu0001/attack/attacker"), Is.True);
        Assert.That(selector.Matches("battle-unit/g000uu0001/idle/defender"), Is.True);
        Assert.That(selector.Matches("unit-face/g000uu0001"), Is.False);
    }

    [Test]
    public void ExactMatchTest()
    {
        CategorySelector selector = CategorySelector.FromPatterns(["battle-unit/g000uu0001/attack/attacker"]);

        Assert.That(selector.Matches("battle-unit/g000uu0001/attack/attacker"), Is.True);
        Assert.That(selector.Matches("battle-unit/g000uu0001/attack/defender"), Is.False);
        Assert.That(selector.Matches("battle-unit/g000uu0001/idle/attacker"), Is.False);
    }

    [Test]
    public void WildcardSegmentTest()
    {
        CategorySelector selector = CategorySelector.FromPatterns(["battle-unit/*/*/attacker"]);

        Assert.That(selector.Matches("battle-unit/g000uu0001/attack/attacker"), Is.True);
        Assert.That(selector.Matches("battle-unit/g000uu0099/hit/attacker"), Is.True);
        Assert.That(selector.Matches("battle-unit/g000uu0001/hit/defender"), Is.False);
    }

    [Test]
    public void PartialWildcardTest()
    {
        CategorySelector selector = CategorySelector.FromPatterns(["battle-background/hu*/*"]);

        Assert.That(selector.Matches("battle-background/human/0"), Is.True);
        Assert.That(selector.Matches("battle-background/undead/0"), Is.False);
    }

    [Test]
    public void SingleCharacterWildcardTest()
    {
        CategorySelector selector = CategorySelector.FromPatterns(["battle-unit/g000uu000?/idle/attacker"]);

        Assert.That(selector.Matches("battle-unit/g000uu0001/idle/attacker"), Is.True);
        Assert.That(selector.Matches("battle-unit/g000uu0012/idle/attacker"), Is.False);
    }

    [Test]
    public void CaseInsensitiveTest()
    {
        CategorySelector selector = CategorySelector.FromPatterns(["Battle-Unit/G000UU0001/*/attacker"]);

        Assert.That(selector.Matches("battle-unit/g000uu0001/idle/attacker"), Is.True);
    }

    [Test]
    public void LongPatternTest()
    {
        CategorySelector selector = CategorySelector.FromPatterns(["unit-face/g000uu0001/attack"]);

        Assert.That(selector.Matches("unit-face/g000uu0001"), Is.False);
    }

    [Test]
    public void MultiplePatternsTest()
    {
        CategorySelector selector = CategorySelector.FromPatterns(["unit-face", "unit-portrait", "battle-background", "battle-unit/*/attack/attacker"]);

        Assert.That(selector.Matches("unit-face/g000uu0001"), Is.True);
        Assert.That(selector.Matches("unit-portrait/g000uu0001"), Is.True);
        Assert.That(selector.Matches("battle-background/human/0"), Is.True);
        Assert.That(selector.Matches("battle-unit/g000uu0001/attack/attacker"), Is.True);
        Assert.That(selector.Matches("battle-unit/g000uu0001/idle/attacker"), Is.False);
    }

    [Test]
    public void UnmatchedPatternTest()
    {
        CategorySelector selector = CategorySelector.FromPatterns(["battle-unit", "unit-fce", "battle-unit/*/sprint/attacker"]);

        Assert.That(selector.UnmatchedPatterns([
            "battle-unit/g000uu0001/attack/attacker",
            "unit-face/g000uu0001"
        ]), Is.EquivalentTo([
            "unit-fce",
            "battle-unit/*/sprint/attacker"
        ]));
    }

    [Test]
    public void AliasTest()
    {
        bool found = CategoryAliases.TryResolve(":upscale-test-battle", out IReadOnlyList<string>? patterns);

        Assert.That(found, Is.True);
        Assert.That(patterns, Contains.Item("battle-unit/g000uu8014/attack/attacker"));
    }

    [Test]
    public void AliasPrefixTest()
    {
        Assert.That(CategoryAliases.IsAlias(":upscale-test-battle"), Is.True);
        Assert.That(CategoryAliases.IsAlias("battle-unit"), Is.False);
    }

    [Test]
    public void UnknownAliasTest()
    {
        Assert.That(CategoryAliases.TryResolve(":12345", out _), Is.False);
    }

    [Test]
    public void AliasCategoriesInCatalogTest()
    {
        IReadOnlyList<GameAssetDescriptor> descriptors = GameAssetCatalog.ListDescriptors(new GameAssetInputs(
            [new UnitDefinitionId("G000UU0001")],
            [],
            [new WorldSpellDefinitionId("G000SS0001")],
            [new WorldPlayerBuildingDefinitionId("G000BB0001")],
            [new WorldLandmarkDefinitionId("G000MG0004")],
            [new ItemDefinitionId("G000IG0001")],
            []));

        HashSet<string> categories = [];

        foreach (PackBuilderEntry entry in PackBuilderCatalog.ListEntries(descriptors))
        {
            categories.Add(entry.Key.CanonicalId.Split('/')[0]);
        }

        foreach (string name in CategoryAliases.Names)
        {
            CategoryAliases.TryResolve(name, out IReadOnlyList<string>? patterns);

            foreach (string pattern in Assert.NotNull(patterns))
            {
                Assert.That(categories, Contains.Item(pattern.Split('/')[0]));
            }
        }
    }
}
