namespace Gallean.Client.Tests;

public sealed class ResourceSetItemViewModelTests
{
    private static string FormatItem(ResourceSetItemViewModel item)
    {
        return $"{item.Name} | {item.Icon}";
    }

    [Test]
    public void ValueAnimationTest()
    {
        ResourceSetItemViewModel item = new(WorldResourceType.Gold, 350, 250);

        Assert.That(item.ValueAnimation, Is.EqualTo([
            new AnimationProperty(
                new ResourceSetValueAnimationTarget(WorldResourceType.Gold),
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1.0),
                From: 250.0,
                To: 350.0,
                AnimationEasing.Decelerate)
        ]));
    }

    [Test]
    public void UnchangedValueAnimationTest()
    {
        ResourceSetItemViewModel item = new(WorldResourceType.Gold, 350, 350);

        Assert.That(item.ValueAnimation, Is.Empty);
    }

    [Test]
    public void PresentationTest()
    {
        ResourceSetItemViewModel[] items =
        [
            new(WorldResourceType.Gold, 1, 1),
            new(WorldResourceType.LifeMana, 1, 1),
            new(WorldResourceType.DeathMana, 1, 1),
            new(WorldResourceType.RuneMana, 1, 1),
            new(WorldResourceType.InfernalMana, 1, 1),
            new(WorldResourceType.GroveMana, 1, 1)
        ];

        Assert.That(items.Select(FormatItem), Is.EqualTo([
            "Gold | Gold",
            "Life mana | ManaLife",
            "Death mana | ManaDeath",
            "Rune mana | ManaRune",
            "Infernal mana | ManaInfernal",
            "Grove mana | ManaGrove"
        ]));
    }
}
