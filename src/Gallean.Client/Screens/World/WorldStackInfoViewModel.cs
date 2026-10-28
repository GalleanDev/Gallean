using Gallean.Engine;

namespace Gallean.Client;

internal sealed record WorldStackInfoViewModel(
    AssetSpriteKey.UnitEventPortrait LeaderPortraitKey,
    string LeaderName,
    string RaceName,
    IReadOnlyList<TextSpans> Lines,
    string? Note,
    WorldUnitGroupInfoGridViewModel Grid) : WorldPositionInfoViewModel
{
    public static WorldStackInfoViewModel Create(
        WorldState world,
        WorldPlayerId viewPlayerId,
        WorldStack stack,
        IGameDefinitionProvider definitions,
        IGameTextProvider texts,
        IUnitStatsProvider stats)
    {
        (WorldUnit leader, GridPosition _, UnitDefinition _) = stack.GetLeader(definitions);
        AssetSpriteKey.UnitEventPortrait portraitKey = new(ScenarioEventPortraitId.Unit(leader.DefinitionId));
        string leaderName = UnitNameResolver.Resolve(leader, definitions, texts);
        WorldPlayer owner = world.Players[stack.OwnerId];
        string raceName = WorldPlayerFormatter.FormatRaceName(definitions, texts, owner);

        if (WorldVisibility.KnowsPlayerContents(world, viewPlayerId, stack.OwnerId))
        {
            return new WorldStackInfoViewModel(
                portraitKey,
                leaderName,
                raceName,
                [TrSpans("Battles won: {0:*}", stack.BattlesWon)],
                Note: null,
                WorldUnitGroupInfoGridViewModel.Create(stack.Units, isFacingLeft: false, definitions, texts, stats));
        }
        else
        {
            return new WorldStackInfoViewModel(
                portraitKey,
                leaderName,
                raceName,
                [],
                Tr("A thief must spy on this race to gain information about it."),
                WorldUnitGroupInfoGridViewModel.Empty);
        }
    }
}
