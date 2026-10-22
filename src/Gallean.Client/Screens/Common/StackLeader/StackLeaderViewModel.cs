using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed record StackLeaderViewModel(
    AssetSpriteKey.UnitEventPortrait PortraitKey,
    string Title,
    IReadOnlyList<StatRowViewModel> Stats,
    IReadOnlyList<LeaderAbilityViewModel> Abilities,
    UnitDetailsViewModel Details) : IDetailsSource
{
    object? IDetailsSource.DetailsContent => Details;

    public bool Equals(StackLeaderViewModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return PortraitKey == other.PortraitKey &&
               Title == other.Title &&
               Stats.SequenceEqual(other.Stats) &&
               Abilities.SequenceEqual(other.Abilities) &&
               Details == other.Details;
    }

    public static StackLeaderViewModel Create(
        WorldStack stack,
        WorldPlayer owner,
        IGameDefinitionProvider definitions,
        IGameTextProvider texts,
        IUnitStatsProvider stats)
    {
        (WorldUnit Leader, GridPosition Position, UnitDefinition Definition) leader = stack.GetLeader(definitions);
        UnitStats leaderStats = stats.GetWorldUnitStats(leader.Leader);
        StackStats stackStats = stats.GetWorldStackStats(stack);
        LeaderAbility leaderAbilities = stats.GetWorldUnitAbilities(leader.Leader);

        IReadOnlyList<StatRowViewModel> rows =
        [
            new(IconMoniker.Hitpoint, Tr("Hit points"), new StatValueViewModel($"{leader.Leader.Hitpoints} / {leaderStats.MaxHitpoints}", 0)),
            new(IconMoniker.Movement, Tr("Movement"), new StatValueViewModel($"{stack.MovementPoints} / {stackStats.MaxMovementPoints}", 0)),
            new(IconMoniker.Stack, Tr("Units"), new StatValueViewModel($"{stack.Units.GetUnits().Count()} / {GridPosition.Count}", 0)),
            new(IconMoniker.Leadership, Tr("Leadership"), new StatValueViewModel($"{stackStats.Leadership}", 0))
        ];

        return new StackLeaderViewModel(
            new AssetSpriteKey.UnitEventPortrait(ScenarioEventPortraitId.Unit(leader.Leader.DefinitionId)),
            UnitNameResolver.Resolve(leader.Leader, definitions, texts),
            rows,
            LeaderAbilityViewModel.CreateList(leaderAbilities),
            UnitDetailsViewModel.CreateFromWorld(leader.Leader, owner, definitions, texts, stats));
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(PortraitKey);
        hash.Add(Title);
        hash.AddSequence(Stats);
        hash.AddSequence(Abilities);
        hash.Add(Details);

        return hash.ToHashCode();
    }
}
