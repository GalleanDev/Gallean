using System.Collections.Immutable;

namespace Gallean.Client;

internal sealed record ResourceSetProjection(ImmutableArray<WorldResourceType> Types)
{
    public static ResourceSetProjection All { get; } = new(
    [
        WorldResourceType.Gold,
        WorldResourceType.LifeMana,
        WorldResourceType.DeathMana,
        WorldResourceType.RuneMana,
        WorldResourceType.InfernalMana,
        WorldResourceType.GroveMana
    ]);

    public static ResourceSetProjection Gold { get; } = new(
    [
        WorldResourceType.Gold
    ]);

    public static ResourceSetProjection Mana { get; } = new(
    [
        WorldResourceType.LifeMana,
        WorldResourceType.DeathMana,
        WorldResourceType.RuneMana,
        WorldResourceType.InfernalMana,
        WorldResourceType.GroveMana
    ]);
}
