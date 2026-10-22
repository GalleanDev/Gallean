namespace Gallean.Client.Tests;

public sealed class ResourceSetViewModelTests
{
    [Test]
    public void AllResourcesTest()
    {
        ResourceSetViewModel resources = new(new WorldResourceSet(Gold: 250, DeathMana: 1, RuneMana: 2, LifeMana: 3, InfernalMana: 4, GroveMana: 5), ResourceSetProjection.All);

        Assert.That(resources.Items, Is.EqualTo([
            new ResourceSetItemViewModel(WorldResourceType.Gold, 250, 250),
            new ResourceSetItemViewModel(WorldResourceType.LifeMana, 3, 3),
            new ResourceSetItemViewModel(WorldResourceType.DeathMana, 1, 1),
            new ResourceSetItemViewModel(WorldResourceType.RuneMana, 2, 2),
            new ResourceSetItemViewModel(WorldResourceType.InfernalMana, 4, 4),
            new ResourceSetItemViewModel(WorldResourceType.GroveMana, 5, 5)
        ]));
    }

    [Test]
    public void ZeroAmountsTest()
    {
        ResourceSetViewModel resources = new(new WorldResourceSet(Gold: 0, DeathMana: 5, RuneMana: 0, LifeMana: 3, InfernalMana: 0, GroveMana: 0), ResourceSetProjection.All);

        Assert.That(resources.Items, Is.EqualTo([
            new ResourceSetItemViewModel(WorldResourceType.LifeMana, 3, 3),
            new ResourceSetItemViewModel(WorldResourceType.DeathMana, 5, 5)
        ]));
    }

    [Test]
    public void ChangeTest()
    {
        ResourceSetViewModel resources = new(
            new WorldResourceSet(Gold: 350, DeathMana: 25, RuneMana: 0, LifeMana: 3, InfernalMana: 0, GroveMana: 0),
            new WorldResourceSet(Gold: 250, DeathMana: 0, RuneMana: 0, LifeMana: 3, InfernalMana: 0, GroveMana: 7),
            ResourceSetProjection.All);

        Assert.That(resources.Items, Is.EqualTo([
            new ResourceSetItemViewModel(WorldResourceType.Gold, 350, 250),
            new ResourceSetItemViewModel(WorldResourceType.LifeMana, 3, 3),
            new ResourceSetItemViewModel(WorldResourceType.DeathMana, 25, 0)
        ]));
    }

    [Test]
    public void EmptyTest()
    {
        ResourceSetViewModel resources = new(WorldResourceSet.Empty, ResourceSetProjection.All);

        Assert.That(resources.Items, Is.EqualTo([
            new ResourceSetItemViewModel(WorldResourceType.Gold, 0, 0)
        ]));
    }

    [Test]
    public void GoldTest()
    {
        ResourceSetViewModel resources = new(new WorldResourceSet(Gold: 250, DeathMana: 1, RuneMana: 0, LifeMana: 3, InfernalMana: 0, GroveMana: 0), ResourceSetProjection.Gold);

        Assert.That(resources.Items, Is.EqualTo([
            new ResourceSetItemViewModel(WorldResourceType.Gold, 250, 250)
        ]));
    }

    [Test]
    public void NoGoldTest()
    {
        ResourceSetViewModel resources = new(new WorldResourceSet(Gold: 0, DeathMana: 1, RuneMana: 0, LifeMana: 3, InfernalMana: 0, GroveMana: 0), ResourceSetProjection.Gold);

        Assert.That(resources.Items, Is.EqualTo([
            new ResourceSetItemViewModel(WorldResourceType.Gold, 0, 0)
        ]));
    }

    [Test]
    public void ManaTest()
    {
        ResourceSetViewModel resources = new(new WorldResourceSet(Gold: 250, DeathMana: 1, RuneMana: 0, LifeMana: 3, InfernalMana: 0, GroveMana: 0), ResourceSetProjection.Mana);

        Assert.That(resources.Items, Is.EqualTo([
            new ResourceSetItemViewModel(WorldResourceType.LifeMana, 3, 3),
            new ResourceSetItemViewModel(WorldResourceType.DeathMana, 1, 1)
        ]));
    }

    [Test]
    public void NoManaTest()
    {
        ResourceSetViewModel resources = new(new WorldResourceSet(Gold: 250, DeathMana: 0, RuneMana: 0, LifeMana: 0, InfernalMana: 0, GroveMana: 0), ResourceSetProjection.Mana);

        Assert.That(resources.Items, Is.EqualTo([
            new ResourceSetItemViewModel(WorldResourceType.LifeMana, 0, 0)
        ]));
    }
}
