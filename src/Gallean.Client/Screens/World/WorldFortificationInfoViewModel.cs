using Gallean.Engine;

namespace Gallean.Client;

internal sealed record WorldFortificationInfoViewModel(
    string Title,
    IReadOnlyList<TextSpans> Lines,
    string? Note,
    string? VisitingStackTitle,
    WorldUnitGroupInfoGridViewModel VisitingStack,
    WorldUnitGroupInfoGridViewModel Defenses) : WorldPositionInfoViewModel
{
    public static WorldFortificationInfoViewModel CreateCapital(
        WorldState world,
        WorldPlayerId viewPlayerId,
        WorldCapital capital,
        WorldStack? visitingStack,
        IGameDefinitionProvider definitions,
        IGameTextProvider texts,
        IUnitStatsProvider stats)
    {
        WorldPlayer owner = world.Players[capital.OwnerId];
        WorldRaceDefinition race = definitions.GetRace(owner.Race);

        string name = texts.GetScenarioText(capital.NameTextId);

        return Create(
            name,
            TrSpans("City level: {0:*}", Tr("Capital")),
            race.CapitalRegeneration,
            capital,
            owner,
            visitingStack,
            world,
            viewPlayerId,
            definitions,
            texts,
            stats);
    }

    public static WorldFortificationInfoViewModel CreateCity(
        WorldState world,
        WorldPlayerId viewPlayerId,
        WorldCity city,
        WorldStack? visitingStack,
        IGameDefinitionProvider definitions,
        IGameTextProvider texts,
        IUnitStatsProvider stats)
    {
        WorldPlayer owner = world.Players[city.OwnerId];
        WorldCityDefinition definition = definitions.GetCity(city.DefinitionId);

        string name = texts.GetScenarioText(city.NameTextId);

        return Create(
            FormatCityTitle(city, name),
            TrSpans("City level: {0:*}", definition.Level),
            definition.Regeneration,
            city,
            owner,
            visitingStack,
            world,
            viewPlayerId,
            definitions,
            texts,
            stats);
    }

    private static WorldFortificationInfoViewModel Create(
        string title,
        TextSpans levelLine,
        int regeneration,
        WorldFortification fortification,
        WorldPlayer owner,
        WorldStack? visitingStack,
        WorldState world,
        WorldPlayerId viewPlayerId,
        IGameDefinitionProvider definitions,
        IGameTextProvider texts,
        IUnitStatsProvider stats)
    {
        string raceName = WorldPlayerFormatter.FormatRaceName(definitions, texts, owner);
        TextSpans ownerLine = TrSpans("Owner: {0:*}", raceName);
        TextSpans regenerationLine = TrSpans("Hit point regeneration per turn: {0:*}", Tr("{0}%", regeneration));
        string? visitingStackTitle = FormatVisitingStackTitle(visitingStack, definitions, texts);

        if (WorldVisibility.KnowsPlayerContents(world, viewPlayerId, owner.Id))
        {
            return new WorldFortificationInfoViewModel(
                title,
                [levelLine, ownerLine, regenerationLine],
                null,
                visitingStackTitle,
                CreateVisitingStack(visitingStack, world, viewPlayerId, definitions, texts, stats),
                WorldUnitGroupInfoGridViewModel.Create(fortification.Garrison, definitions, texts, stats));
        }
        else
        {
            return new WorldFortificationInfoViewModel(
                title,
                [levelLine, ownerLine, regenerationLine],
                Tr("A thief must spy on this city to gain information about it."),
                visitingStackTitle,
                WorldUnitGroupInfoGridViewModel.Empty,
                WorldUnitGroupInfoGridViewModel.Empty);
        }
    }

    private static WorldUnitGroupInfoGridViewModel CreateVisitingStack(
        WorldStack? visitingStack,
        WorldState world,
        WorldPlayerId viewPlayerId,
        IGameDefinitionProvider definitions,
        IGameTextProvider texts,
        IUnitStatsProvider stats)
    {
        if (visitingStack is not null && WorldVisibility.KnowsPlayerContents(world, viewPlayerId, visitingStack.OwnerId))
        {
            return WorldUnitGroupInfoGridViewModel.Create(visitingStack.Units, definitions, texts, stats);
        }
        else
        {
            return WorldUnitGroupInfoGridViewModel.Empty;
        }
    }

    private static string? FormatVisitingStackTitle(WorldStack? visitingStack, IGameDefinitionProvider definitions, IGameTextProvider texts)
    {
        if (visitingStack is not null)
        {
            (WorldUnit leader, GridPosition _, UnitDefinition _) = visitingStack.GetLeader(definitions);
            string leaderName = UnitNameResolver.Resolve(leader, definitions, texts);

            return Tr("{0}'s party", leaderName);
        }
        else
        {
            return null;
        }
    }

    private static string FormatCityTitle(WorldCity city, string name)
    {
        if (city.RiotTurns > 0)
        {
            return Tr("{0} (RIOT)", name);
        }
        else
        {
            return name;
        }
    }
}
