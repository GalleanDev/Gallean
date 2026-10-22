namespace Gallean.Engine;

internal static class ResourceSetCalculator
{
    public static WorldResourceSet Add(WorldResourceSet a, WorldResourceSet b)
    {
        return new WorldResourceSet(
            a.Gold + b.Gold,
            a.DeathMana + b.DeathMana,
            a.RuneMana + b.RuneMana,
            a.LifeMana + b.LifeMana,
            a.InfernalMana + b.InfernalMana,
            a.GroveMana + b.GroveMana
        );
    }

    public static WorldResourceSet Subtract(WorldResourceSet a, WorldResourceSet b)
    {
        if (!CanAfford(a, b))
        {
            throw new OverflowException();
        }

        return new WorldResourceSet(
            a.Gold - b.Gold,
            a.DeathMana - b.DeathMana,
            a.RuneMana - b.RuneMana,
            a.LifeMana - b.LifeMana,
            a.InfernalMana - b.InfernalMana,
            a.GroveMana - b.GroveMana
        );
    }

    public static WorldResourceSet Multiply(WorldResourceSet set, int factor)
    {
        return new WorldResourceSet(
            set.Gold * factor,
            set.DeathMana * factor,
            set.RuneMana * factor,
            set.LifeMana * factor,
            set.InfernalMana * factor,
            set.GroveMana * factor
        );
    }

    public static WorldResourceSet ScalePercent(WorldResourceSet set, int percent)
    {
        return new WorldResourceSet(
            set.Gold * percent / 100,
            set.DeathMana * percent / 100,
            set.RuneMana * percent / 100,
            set.LifeMana * percent / 100,
            set.InfernalMana * percent / 100,
            set.GroveMana * percent / 100
        );
    }

    public static bool CanAfford(WorldResourceSet available, WorldResourceSet cost)
    {
        return available.Gold >= cost.Gold &&
               available.DeathMana >= cost.DeathMana &&
               available.RuneMana >= cost.RuneMana &&
               available.LifeMana >= cost.LifeMana &&
               available.InfernalMana >= cost.InfernalMana &&
               available.GroveMana >= cost.GroveMana;
    }
}
