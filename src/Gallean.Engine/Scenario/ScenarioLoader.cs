using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

public static class ScenarioLoader
{
    private const int PlayerRaceUnset = 99;
    private const int UnfogRadiusUnset = -1;
    private const int UnfogRadiusLimit = 24;
    private const int ChanceLimit = 100;
    private const int TextSequenceZero = 0;

    public static IReadOnlyList<ScenarioCampaign> EnumerateCampaigns(
        IGlobalDatabaseIndex globalDb,
        IScenarioDatabaseIndex scenarioDb,
        CampaignDatabase campaignDb)
    {
        IEnumerable<Db.ScenarioInfo> playableScenarios = scenarioDb.ScenarioInfos
            .Where(info => scenarioDb.ScenariosWithEvents.Contains(info.ScenarioId));

        List<ScenarioCampaign> result = [];

        foreach (IGrouping<Db.Id, Db.ScenarioInfo> group in playableScenarios
            .GroupBy(info => info.CampaignId)
            .OrderBy(group => group.Key.Value, StringComparer.OrdinalIgnoreCase))
        {
            Db.Campaign campaign = campaignDb.Campaigns[group.Key];
            GlobalTextId nameTextId = new(campaign.NameId);
            Db.Race playRace = FindRaceByType(globalDb, campaign.PlayRace);

            Dictionary<Db.ScenarioId, int> scenarioOrder = campaignDb.CampaignScenariosByCampaignId[group.Key]
                .Where(entry => entry.ScenarioNumber is not null)
                .ToDictionary(entry => entry.ScenarioId, entry => entry.ScenarioNumber!.Value);

            List<ScenarioMetadata> scenarios = [];
            foreach (Db.ScenarioInfo info in group.OrderBy(info => scenarioOrder[info.ScenarioId]))
            {
                scenarios.Add(new ScenarioMetadata(
                    new ScenarioId(info.ScenarioId.Value),
                    LoadInfoTexts(info),
                    info.SuggestedLevel,
                    info.MapSize,
                    info.QuantityCities,
                    MapDifficulty(info.DifficultyGame),
                    LoadPlayableRaces(globalDb, info)));
            }

            result.Add(new ScenarioCampaign(
                new ScenarioCampaignId(group.Key.Value),
                nameTextId,
                new ScenarioRace(MapRace(playRace.RaceType), new GlobalTextId(playRace.NameText)),
                campaign.Skirmish,
                scenarios));
        }

        return result;
    }

    public static Scenario Create(
        GlobalDatabase globalDb,
        ScenarioDatabase scenarioDb,
        CampaignDatabase campaignDb,
        ScenarioId scenarioId,
        IGameDefinitionProvider definitions,
        GlobalVariables globals,
        ScenarioConfiguration configuration,
        IRandomProvider random)
    {
        ScenarioLeader? leader = configuration.Leader;

        if (leader is not null && leader.Unit.Name is null)
        {
            UnitDefinition definition = definitions.GetUnit(leader.Unit.DefinitionId);
            string? name = LeaderNameCalculator.TryGetName(definition, definitions, random);

            configuration = configuration with { Leader = leader with { Unit = leader.Unit with { Name = name } } };
        }

        return LoadInternal(
            globalDb,
            scenarioDb,
            campaignDb,
            new Db.ScenarioId(scenarioId.Value),
            definitions,
            globals,
            configuration);
    }

    public static Scenario Load(
        GlobalDatabase globalDb,
        ScenarioDatabase scenarioDb,
        CampaignDatabase campaignDb,
        ScenarioId scenarioId,
        IGameDefinitionProvider definitions,
        GlobalVariables globals)
    {
        return LoadInternal(
            globalDb,
            scenarioDb,
            campaignDb,
            new Db.ScenarioId(scenarioId.Value),
            definitions,
            globals,
            configuration: null);
    }

    private static Scenario LoadInternal(
        GlobalDatabase globalDb,
        ScenarioDatabase scenarioDb,
        CampaignDatabase campaignDb,
        Db.ScenarioId scenarioId,
        IGameDefinitionProvider definitions,
        GlobalVariables globals,
        ScenarioConfiguration? configuration)
    {
        Db.ScenarioInfo? scenarioInfo = scenarioDb.ScenarioInfosByScenario[scenarioId].SingleOrDefault();
        if (scenarioInfo is null)
        {
            throw new ArgumentException($"Scenario '{scenarioId}' was not found.");
        }

        WorldDifficulty difficulty = configuration?.Difficulty ?? MapDifficulty(scenarioInfo.DifficultyGame);

        ImmutableDictionary<Db.Id, WorldPlayerId> playerIds =
            LoadPlayerIds(scenarioDb, scenarioId);
        IReadOnlyDictionary<Db.Id, Db.ScenarioLocation> locations =
            scenarioDb.ScenarioLocationsByScenario[scenarioId].ToDictionary(location => location.LocationId);
        WorldMap worldMap =
            LoadMap(globalDb, scenarioDb, scenarioId, scenarioInfo);
        ImmutableSortedDictionary<WorldPlayerId, WorldPlayer> players =
            LoadPlayers(globalDb, scenarioDb, campaignDb, scenarioId, scenarioInfo, playerIds, globals, difficulty, configuration);
        WorldTurnInfo turnInfo =
            LoadTurnInfo(scenarioInfo, players);
        ImmutableSortedDictionary<WorldRelationId, WorldRelation> relations =
            LoadRelations(scenarioDb, scenarioId);
        ImmutableSortedDictionary<WorldFortificationId, WorldFortification> fortifications =
            LoadFortifications(globalDb, scenarioDb, scenarioId, playerIds, definitions, globals);
        ImmutableSortedDictionary<WorldStackId, WorldStack> stacks =
            LoadStacks(scenarioDb, scenarioId, playerIds, definitions, globals, fortifications, locations);
        ImmutableSortedDictionary<WorldResourceNodeId, WorldResourceNode> resourceNodes =
            LoadResourceNodes(scenarioDb, scenarioId, GetNeutralPlayerId(players));
        ImmutableSortedDictionary<WorldRodId, WorldRod> rods =
            LoadRods(scenarioDb, scenarioId, playerIds);
        ImmutableSortedDictionary<WorldBagId, WorldBag> bags =
            LoadBags(scenarioDb, scenarioId, definitions, globals);
        ImmutableSortedDictionary<WorldLandmarkId, WorldLandmark> landmarks =
            LoadLandmarks(globalDb, scenarioDb, scenarioId);
        ImmutableSortedDictionary<WorldMountainId, WorldMountain> mountains =
            LoadMountains(scenarioDb, scenarioId, worldMap);
        ImmutableSortedDictionary<WorldSiteId, WorldSite> sites =
            LoadSites(scenarioDb, scenarioId);
        ImmutableSortedDictionary<WorldSubraceId, WorldSubrace> subraces =
            LoadSubraces(scenarioDb, scenarioId, playerIds);
        ScenarioInfoTexts infoTexts =
            LoadInfoTexts(scenarioInfo);
        WorldScenarioState scenarioState =
            LoadScenarioState(infoTexts);
        List<ScenarioVariableDefinition> variableDefinitions =
            LoadVariables(scenarioDb, scenarioId);
        List<ScenarioEventDefinition> eventDefinitions =
            LoadEvents(scenarioDb, scenarioId, playerIds, subraces, locations, variableDefinitions, globals);
        List<ScenarioLocationDefinition> locationDefinitions =
            LoadLocations(scenarioDb, scenarioId);
        ImmutableArray<ScenarioName> names =
            ScenarioNameLoader.Load(globalDb, scenarioDb, scenarioId);
        List<ScenarioStackTemplateDefinition> stackTemplates =
            LoadStackTemplates(scenarioDb, scenarioId, playerIds, definitions, globals, locations);

        ScenarioLeader? scenarioLeader = configuration?.Leader;
        if (scenarioLeader is not null)
        {
            stacks = ImportLeader(stacks, players, fortifications, scenarioLeader, definitions, globals);
        }

        ScenarioId? nextScenarioId = FindNextScenarioId(campaignDb, scenarioInfo);

        ScenarioDefinition scenarioDefinition = new(
            new ScenarioId(scenarioId.Value),
            infoTexts,
            scenarioInfo.SuggestedLevel,
            scenarioInfo.MaxLeader,
            scenarioInfo.MaxCity,
            nextScenarioId);

        WorldState state = new(
            worldMap,
            turnInfo,
            difficulty,
            players,
            relations,
            stacks,
            fortifications,
            resourceNodes,
            rods,
            bags,
            ImmutableSortedDictionary<WorldTombId, WorldTomb>.Empty,
            landmarks,
            mountains,
            sites,
            subraces,
            scenarioState,
            ImmutableSortedDictionary<WorldPlayerId, WorldAiMemory>.Empty);

        GameScenarioProvider scenarioProvider = new(
            eventDefinitions,
            locationDefinitions,
            names,
            stackTemplates,
            variableDefinitions,
            scenarioDefinition);

        ValidateMapPositions(state);

        return new Scenario(state, scenarioProvider);
    }

    private static WorldTurnInfo LoadTurnInfo(Db.ScenarioInfo scenarioInfo, ImmutableSortedDictionary<WorldPlayerId, WorldPlayer> players)
    {
        return new WorldTurnInfo(Math.Max(scenarioInfo.CurrentTurn, 1), players.Keys.First());
    }

    private static WorldScenarioState LoadScenarioState(ScenarioInfoTexts infoTexts)
    {
        return WorldScenarioState.Empty with { ObjectiveTextId = infoTexts.Objective };
    }

    private static ScenarioId? FindNextScenarioId(CampaignDatabase campaignDb, Db.ScenarioInfo scenarioInfo)
    {
        Db.Campaign campaign = campaignDb.Campaigns[scenarioInfo.CampaignId];

        if (campaign.Skirmish)
        {
            return null;
        }

        SortedDictionary<int, Db.ScenarioId> scenarioIds = [];

        foreach (Db.CampaignScenario entry in campaignDb.CampaignScenariosByCampaignId[scenarioInfo.CampaignId])
        {
            if (entry.ScenarioNumber is not null)
            {
                scenarioIds.Add(entry.ScenarioNumber.Value, entry.ScenarioId);
            }
        }

        List<Db.ScenarioId> orderedIds = [.. scenarioIds.Values];

        int index = orderedIds.IndexOf(scenarioInfo.ScenarioId);

        if (index >= 0 && index + 1 < orderedIds.Count)
        {
            return new ScenarioId(orderedIds[index + 1].Value);
        }
        else
        {
            return null;
        }
    }

    private static void ValidateMapPositions(WorldState state)
    {
        WorldMap map = state.Map;

        foreach (WorldStack stack in state.Stacks.Values)
        {
            CheckPosition(stack.Position, "Stack", stack.Id.Value);
        }

        foreach (WorldResourceNode resourceNode in state.ResourceNodes.Values)
        {
            CheckPosition(resourceNode.Position, "Resource node", resourceNode.Id.Value);
        }

        foreach (WorldBag bag in state.Bags.Values)
        {
            CheckPosition(bag.Position, "Bag", bag.Id.Value);
        }

        foreach (WorldRod rod in state.Rods.Values)
        {
            CheckPosition(rod.Position, "Rod", rod.Id.Value);
        }

        foreach (WorldFortification fortification in state.Fortifications.Values)
        {
            CheckFootprint(fortification.Footprint, "Fortification", fortification.Id.Value);
        }

        foreach (WorldSite site in state.Sites.Values)
        {
            CheckFootprint(site.Footprint, "Site", site.Id.Value);
        }

        foreach (WorldLandmark landmark in state.Landmarks.Values)
        {
            CheckFootprint(landmark.Footprint, "Landmark", landmark.Id.Value);
        }

        void CheckPosition(WorldPosition position, string kind, string id)
        {
            if (!map.IsValidPosition(position))
            {
                throw new InvalidDataException($"{kind} '{id}' is outside the map.");
            }
        }

        void CheckFootprint(WorldFootprint footprint, string kind, string id)
        {
            foreach (WorldPosition tile in footprint.EnumerateTiles())
            {
                if (!map.IsValidPosition(tile))
                {
                    throw new InvalidDataException($"{kind} '{id}' extends outside the map.");
                }
            }
        }
    }

    private static ImmutableDictionary<Db.Id, WorldPlayerId> LoadPlayerIds(
        ScenarioDatabase scenarioDb,
        Db.ScenarioId scenarioId)
    {
        ImmutableDictionary<Db.Id, WorldPlayerId>.Builder result = ImmutableDictionary.CreateBuilder<Db.Id, WorldPlayerId>();
        int currentPlayerId = 1;

        foreach (Db.ScenarioPlayer player in scenarioDb.ScenarioPlayersByScenario[scenarioId])
        {
            result.Add(player.PlayerId, new WorldPlayerId(currentPlayerId++));
        }

        return result.ToImmutable();
    }

    private static ImmutableSortedDictionary<WorldResourceNodeId, WorldResourceNode> LoadResourceNodes(
        ScenarioDatabase scenarioDb,
        Db.ScenarioId scenarioId,
        WorldPlayerId neutralPlayerId)
    {
        ImmutableSortedDictionary<WorldResourceNodeId, WorldResourceNode>.Builder result =
            ImmutableSortedDictionary.CreateBuilder<WorldResourceNodeId, WorldResourceNode>();

        foreach (Db.ScenarioCrystal crystal in scenarioDb.ScenarioCrystalsByScenario[scenarioId])
        {
            WorldResourceNode node = new(
                new WorldResourceNodeId(crystal.CrystalId),
                neutralPlayerId,
                new WorldPosition(crystal.PositionX, crystal.PositionY),
                MapCrystalType(crystal.Resource),
                crystal.AiPriority
            );

            result.Add(node.Id, node);
        }

        return result.ToImmutable();
    }

    private static List<ScenarioVariableDefinition> LoadVariables(
        ScenarioDatabase scenarioDb,
        Db.ScenarioId scenarioId)
    {
        List<ScenarioVariableDefinition> result = [];

        foreach (Db.ScenarioVariable variable in scenarioDb.ScenarioVariablesByScenario[scenarioId])
        {
            result.Add(new ScenarioVariableDefinition(
                new ScenarioVariableId(variable.Id),
                variable.Name,
                variable.Value));
        }

        return result;
    }

    private static List<ScenarioEventDefinition> LoadEvents(
        ScenarioDatabase scenarioDb,
        Db.ScenarioId scenarioId,
        IReadOnlyDictionary<Db.Id, WorldPlayerId> playerIds,
        IReadOnlyDictionary<WorldSubraceId, WorldSubrace> subraces,
        IReadOnlyDictionary<Db.Id, Db.ScenarioLocation> locations,
        IReadOnlyList<ScenarioVariableDefinition> variables,
        GlobalVariables globals)
    {
        HashSet<ScenarioVariableId> variableIds = [];

        foreach (ScenarioVariableDefinition variable in variables)
        {
            variableIds.Add(variable.Id);
        }

        ILookup<Db.Id, Db.ScenarioEventCondition> conditionsByEvent =
            GetScenarioObjectsLookup(scenarioDb.ScenarioEventConditionsByScenarioAndId, scenarioId);
        ILookup<Db.Id, Db.ScenarioEventEffect> effectsByEvent =
            GetScenarioObjectsLookup(scenarioDb.ScenarioEventEffectsByScenarioAndId, scenarioId);
        ILookup<Db.Id, Db.ScenarioEventUnfog> unfogAudiencesByEvent =
            GetScenarioObjectsLookup(scenarioDb.ScenarioEventUnfogsByScenarioAndEventId, scenarioId);
        ILookup<Db.Id, Db.TextScenario> popupTextsByObject =
            GetScenarioTextsLookup(scenarioDb, scenarioId, Db.TextScenario.PopupTextId);
        IReadOnlyDictionary<Db.Id, ScenarioStackReference> stackReferences =
            LoadStackReferences(scenarioDb, scenarioId);

        List<ScenarioEventDefinition> definitions = [];

        foreach (Db.ScenarioEvent ev in scenarioDb.ScenarioEventsByScenario[scenarioId])
        {
            IReadOnlyList<Db.ScenarioEventCondition> conditions = [.. conditionsByEvent[ev.Id]];

            if (conditions.Count > 0)
            {
                if (ev.Chance < 0 || ev.Chance > ChanceLimit)
                {
                    throw new InvalidDataException($"Event '{ev.Id}' has an invalid chance.");
                }

                ImmutableArray<ScenarioEventCondition> mappedConditions =
                    MapEventConditions(conditions, playerIds, stackReferences, variableIds, globals);
                ImmutableArray<WorldPlayerId> unfogAudience =
                    MapUnfogAudience(unfogAudiencesByEvent[ev.Id], playerIds);
                ImmutableArray<ScenarioEventEffect> effects = MapEventEffects(
                    ev,
                    effectsByEvent[ev.Id],
                    popupTextsByObject,
                    playerIds,
                    unfogAudience,
                    stackReferences,
                    subraces,
                    locations,
                    variableIds);

                definitions.Add(new ScenarioEventDefinition(
                    new ScenarioEventId(ev.Id),
                    MapEventAffectedRaces(ev),
                    MapEventVerifiedRaces(ev),
                    mappedConditions,
                    effects,
                    ev.Enabled,
                    ev.OccurOnce,
                    ev.Chance,
                    ev.Order));
            }
        }

        return definitions;
    }

    private static IReadOnlyDictionary<Db.Id, ScenarioStackReference> LoadStackReferences(
        ScenarioDatabase scenarioDb,
        Db.ScenarioId scenarioId)
    {
        Dictionary<Db.Id, ScenarioStackReference> result = [];

        foreach (Db.Id templateId in GetScenarioObjects(scenarioDb.ScenarioStackTemplatesByScenarioAndId, scenarioId).Keys)
        {
            result[templateId] = new ScenarioStackReference.Template(new ScenarioStackTemplateId(templateId));
        }

        foreach (Db.Id stackId in GetScenarioObjects(scenarioDb.ScenarioStacksByScenarioAndStackId, scenarioId).Keys)
        {
            result[stackId] = new ScenarioStackReference.Stack(new WorldStackId(stackId));
        }

        return result;
    }

    private static ScenarioInfoTexts LoadInfoTexts(Db.ScenarioInfo info)
    {
        return new ScenarioInfoTexts(
            MakeInfoTextId(info, ScenarioInfoTextDecoder.NameTextId),
            TryMakeInfoTextId(info, ScenarioInfoTextDecoder.DescriptionTextId, info.Description),
            MakeInfoTextId(info, ScenarioInfoTextDecoder.ObjectiveTextId),
            TryMakeInfoTextId(info, ScenarioInfoTextDecoder.BriefingTextId, info.BriefingLong1),
            MakeInfoTextId(info, ScenarioInfoTextDecoder.WinDebriefingTextId),
            MakeInfoTextId(info, ScenarioInfoTextDecoder.LoseDebriefingTextId),
            TryMakeInfoTextId(info, ScenarioInfoTextDecoder.CreatorTextId, info.Creator));
    }

    private static ScenarioTextId MakeInfoTextId(Db.ScenarioInfo info, string idString)
    {
        return ScenarioTextId.MakeInternal(info.InfoId, idString, 0);
    }

    private static ScenarioTextId? TryMakeInfoTextId(Db.ScenarioInfo info, string idString, string? text)
    {
        if (text is not null && !IsEmptyText(text))
        {
            return MakeInfoTextId(info, idString);
        }
        else
        {
            return null;
        }
    }

    private static List<ScenarioLocationDefinition> LoadLocations(
        ScenarioDatabase scenarioDb,
        Db.ScenarioId scenarioId)
    {
        List<ScenarioLocationDefinition> result = [];

        foreach (Db.ScenarioLocation location in scenarioDb.ScenarioLocationsByScenario[scenarioId])
        {
            ScenarioLocationId locationId = new(location.LocationId);
            ScenarioTextId nameTextId = ScenarioTextId.MakeInternal(
                location.LocationId, Db.TextScenario.NameTextId, TextSequenceZero);
            WorldPosition position = new(location.PositionX, location.PositionY);

            result.Add(new ScenarioLocationDefinition(locationId, nameTextId, position, location.Radius));
        }

        return result;
    }

    private static ImmutableSortedSet<WorldPlayerId> MapSpiedPlayers(
        Db.ScenarioPlayer player,
        IReadOnlyDictionary<Db.Id, WorldPlayerId> playerIds)
    {
        ImmutableSortedSet<WorldPlayerId>.Builder result = ImmutableSortedSet.CreateBuilder<WorldPlayerId>();

        foreach (Db.Id? spy in new[] { player.Spy1Id, player.Spy2Id, player.Spy3Id })
        {
            if (spy is not { } spyId)
            {
                continue;
            }

            if (!playerIds.TryGetValue(spyId, out WorldPlayerId spiedPlayerId))
            {
                throw new InvalidDataException($"Player '{player.PlayerId}' spies on an unexpected player.");
            }

            result.Add(spiedPlayerId);
        }

        return result.ToImmutable();
    }

    private static ImmutableArray<WorldPlayerId> MapUnfogAudience(
        IEnumerable<Db.ScenarioEventUnfog> audience,
        IReadOnlyDictionary<Db.Id, WorldPlayerId> playerIds)
    {
        ImmutableArray<WorldPlayerId>.Builder result = ImmutableArray.CreateBuilder<WorldPlayerId>();

        foreach (Db.ScenarioEventUnfog entry in audience)
        {
            if (!playerIds.TryGetValue(entry.Player, out WorldPlayerId playerId))
            {
                throw new InvalidDataException($"Unexpected data in event '{entry.EventId}'.");
            }

            result.Add(playerId);
        }

        return result.ToImmutable();
    }

    private static ImmutableArray<ScenarioEventEffect> MapEventEffects(
        Db.ScenarioEvent ev,
        IEnumerable<Db.ScenarioEventEffect> effects,
        ILookup<Db.Id, Db.TextScenario> popupTextsByObject,
        IReadOnlyDictionary<Db.Id, WorldPlayerId> playerIds,
        ImmutableArray<WorldPlayerId> unfogAudience,
        IReadOnlyDictionary<Db.Id, ScenarioStackReference> stackReferences,
        IReadOnlyDictionary<WorldSubraceId, WorldSubrace> subraces,
        IReadOnlyDictionary<Db.Id, Db.ScenarioLocation> locations,
        IReadOnlySet<ScenarioVariableId> variableIds)
    {
        ImmutableArray<ScenarioEventEffect>.Builder result = ImmutableArray.CreateBuilder<ScenarioEventEffect>();

        List<Db.ScenarioEventEffect> orderedEffects = effects.OrderBy(entry => entry.Number).ToList();

        foreach (Db.ScenarioEventEffect effect in orderedEffects)
        {
            ScenarioEventEffect? mappedEffect = MapEventEffect(
                effect,
                ev,
                popupTextsByObject,
                playerIds,
                unfogAudience,
                stackReferences,
                subraces,
                locations,
                variableIds);

            if (mappedEffect is not null)
            {
                result.Add(mappedEffect);
            }
        }

        return result.ToImmutable();
    }

    private static ScenarioEventEffect? MapEventEffect(
        Db.ScenarioEventEffect effect,
        Db.ScenarioEvent ev,
        ILookup<Db.Id, Db.TextScenario> popupTextsByObject,
        IReadOnlyDictionary<Db.Id, WorldPlayerId> playerIds,
        ImmutableArray<WorldPlayerId> unfogAudience,
        IReadOnlyDictionary<Db.Id, ScenarioStackReference> stackReferences,
        IReadOnlyDictionary<WorldSubraceId, WorldSubrace> subraces,
        IReadOnlyDictionary<Db.Id, Db.ScenarioLocation> locations,
        IReadOnlySet<ScenarioVariableId> variableIds)
    {
        return effect.Category switch
        {
            Db.ScenarioEventEffectType.Popup => MapPopup(),
            Db.ScenarioEventEffectType.EnableEvent => MapEnableEvent(),
            Db.ScenarioEventEffectType.GiveItem => MapGiveItem(),
            Db.ScenarioEventEffectType.Unfog => MapUnfog(),
            Db.ScenarioEventEffectType.RemoveMountain => MapRemoveMountain(),
            Db.ScenarioEventEffectType.RemoveLandmark => MapRemoveLandmark(),
            Db.ScenarioEventEffectType.ChangeObjective => MapChangeObjective(),
            Db.ScenarioEventEffectType.ChangeLandmark => MapChangeLandmark(),
            Db.ScenarioEventEffectType.ChangeTerrain => MapChangeTerrain(),
            Db.ScenarioEventEffectType.Win => MapWin(),
            Db.ScenarioEventEffectType.CreateStack => MapCreateStack(),
            Db.ScenarioEventEffectType.CastSpell => MapCastSpell(),
            Db.ScenarioEventEffectType.CastSpellMap => MapCastSpellMap(),
            Db.ScenarioEventEffectType.ChangeOwner => MapChangeOwner(),
            Db.ScenarioEventEffectType.MoveBeside => MapMoveBeside(),
            Db.ScenarioEventEffectType.Battle => MapBattle(),
            Db.ScenarioEventEffectType.GiveSpell => MapGiveSpell(),
            Db.ScenarioEventEffectType.MoveStack => MapMoveStack(),
            Db.ScenarioEventEffectType.Ally => MapAlly(),
            Db.ScenarioEventEffectType.Diplomacy => MapDiplomacy(),
            Db.ScenarioEventEffectType.ChangeOrder => MapChangeOrder(),
            Db.ScenarioEventEffectType.DestroyItem => MapDestroyItem(),
            Db.ScenarioEventEffectType.RemoveStack => MapRemoveStack(),
            Db.ScenarioEventEffectType.ModifyVariable => MapModifyVariable()
        };

        ScenarioEventWinEffect MapWin()
        {
            if (effect.WinScenario is true)
            {
                return new ScenarioEventWinEffect(playerIds[RequireId(effect.IdPlayer1, effect)]);
            }
            else
            {
                return new ScenarioEventWinEffect(WinnerId: null);
            }
        }

        ScenarioEventPopupEffect? MapPopup()
        {
            IEnumerable<string?> texts = popupTextsByObject[ev.Id]
                .Where(popupText => popupText.SeqNum == effect.Number)
                .Select(popupText => popupText.Text);

            if (!IsEmptyText(string.Concat(texts)))
            {
                ScenarioEventPortraitId? portraitId = null;

                if (effect.Image is not null)
                {
                    portraitId = new ScenarioEventPortraitId(effect.Image);
                }

                return new ScenarioEventPopupEffect(
                    ScenarioTextId.MakeInternal(ev.Id, Db.TextScenario.PopupTextId, effect.Number),
                    MapPopupAudience(),
                    portraitId,
                    RequireBool(effect.LeftSide, effect),
                    effect.BoolValue is true);
            }
            else
            {
                return null;
            }
        }

        ScenarioEventPopupAudience MapPopupAudience()
        {
            return effect.PopupShow switch
            {
                "TRI" => ScenarioEventPopupAudience.Triggerer,
                "ALL" => ScenarioEventPopupAudience.AllPlayers,
                "AFF" => ScenarioEventPopupAudience.AffectedRaces,
                _ => throw new InvalidDataException($"Event effect '{effect.Id}' #{effect.Number} has an unexpected audience.")
            };
        }

        ScenarioEventEnableEventEffect MapEnableEvent()
        {
            return new ScenarioEventEnableEventEffect(
                new ScenarioEventId(RequireId(effect.IdEvent, effect)),
                RequireBool(effect.Enable, effect));
        }

        ScenarioEventGiveItemEffect MapGiveItem()
        {
            return new ScenarioEventGiveItemEffect(
                new ItemDefinitionId(RequireId(effect.TypeItem, effect)),
                MapGiveItemTarget());
        }

        ScenarioEventGiveItemTarget MapGiveItemTarget()
        {
            return RequireInt(effect.GiveTo, effect) switch
            {
                0 => ScenarioEventGiveItemTarget.Triggerer,
                1 => ScenarioEventGiveItemTarget.TriggererCapital,
                _ => throw new InvalidDataException($"Event effect '{effect.Id}' #{effect.Number} has an unexpected target.")
            };
        }

        ScenarioEventGiveSpellEffect MapGiveSpell()
        {
            return new ScenarioEventGiveSpellEffect(
                new WorldSpellDefinitionId(RequireId(effect.TypeSpell, effect)));
        }

        ScenarioEventAllyEffect MapAlly()
        {
            return new ScenarioEventAllyEffect(
                playerIds[RequireId(effect.IdPlayer1, effect)],
                playerIds[RequireId(effect.IdPlayer2, effect)],
                effect.PermanentAlliance is true);
        }

        ScenarioEventDiplomacyEffect MapDiplomacy()
        {
            return new ScenarioEventDiplomacyEffect(
                playerIds[RequireId(effect.IdPlayer1, effect)],
                playerIds[RequireId(effect.IdPlayer2, effect)],
                RequireInt(effect.Diplomacy, effect),
                effect.Enable);
        }

        ScenarioEventDestroyItemEffect MapDestroyItem()
        {
            return new ScenarioEventDestroyItemEffect(
                new ItemDefinitionId(RequireId(effect.TypeItem, effect)),
                RequireBool(effect.TriggerOnly, effect));
        }

        ScenarioEventUnfogEffect MapUnfog()
        {
            return new ScenarioEventUnfogEffect(
                new ScenarioLocationId(RequireId(effect.IdLocation, effect)),
                effect.Enable ?? true,
                MapUnfogRadius(),
                unfogAudience);
        }

        int? MapUnfogRadius()
        {
            if (effect.NumValue is null)
            {
                return null;
            }

            int radius = effect.NumValue.Value;

            if (radius == UnfogRadiusUnset)
            {
                return null;
            }
            else if (radius < 0 || radius > UnfogRadiusLimit)
            {
                throw new InvalidDataException($"Event effect '{effect.Id}' #{effect.Number} has an invalid unfog radius.");
            }
            else
            {
                return radius;
            }
        }

        ScenarioEventRemoveMountainEffect MapRemoveMountain()
        {
            return new ScenarioEventRemoveMountainEffect(
                new ScenarioLocationId(RequireId(effect.IdLocation, effect)));
        }

        ScenarioEventRemoveLandmarkEffect MapRemoveLandmark()
        {
            return new ScenarioEventRemoveLandmarkEffect(
                new WorldLandmarkId(RequireId(effect.IdLandmark, effect)));
        }

        ScenarioEventChangeObjectiveEffect MapChangeObjective()
        {
            return new ScenarioEventChangeObjectiveEffect(
                ScenarioTextId.MakeInternal(ev.Id, Db.TextScenario.ObjectiveTextId, TextSequenceZero));
        }

        ScenarioEventChangeLandmarkEffect MapChangeLandmark()
        {
            return new ScenarioEventChangeLandmarkEffect(
                new WorldLandmarkId(RequireId(effect.IdLandmark, effect)),
                new WorldLandmarkDefinitionId(RequireId(effect.TypeLandmark, effect)));
        }

        ScenarioEventChangeTerrainEffect MapChangeTerrain()
        {
            return new ScenarioEventChangeTerrainEffect(
                new ScenarioLocationId(RequireId(effect.IdLocation, effect)),
                MapTerrain(RequireInt(effect.Lookup, effect)),
                RequireInt(effect.NumValue, effect));
        }

        ScenarioEventMoveStackEffect? MapMoveStack()
        {
            Db.Id? stackId = effect.IdStack ?? effect.IdStackTemplate;

            if (stackId is not null && stackReferences.TryGetValue(stackId.Value, out ScenarioStackReference? stack))
            {
                return new ScenarioEventMoveStackEffect(
                    stack,
                    new ScenarioLocationId(RequireId(effect.IdLocation, effect)));
            }
            else
            {
                return null;
            }
        }

        ScenarioEventMoveBesideEffect? MapMoveBeside()
        {
            Db.Id? stackId = effect.IdStack ?? effect.IdStackTemplate;

            if (stackId is not null && stackReferences.TryGetValue(stackId.Value, out ScenarioStackReference? stack))
            {
                return new ScenarioEventMoveBesideEffect(stack);
            }
            else
            {
                return null;
            }
        }

        ScenarioEventBattleEffect? MapBattle()
        {
            Db.Id? stackId = effect.IdStack ?? effect.IdStackTemplate;

            if (stackId is not null && stackReferences.TryGetValue(stackId.Value, out ScenarioStackReference? stack))
            {
                return new ScenarioEventBattleEffect(stack);
            }
            else
            {
                return null;
            }
        }

        ScenarioEventCastSpellEffect MapCastSpell()
        {
            return new ScenarioEventCastSpellEffect(
                new WorldSpellDefinitionId(RequireId(effect.TypeSpell, effect)),
                MapCastingPlayer());
        }

        ScenarioEventCastSpellMapEffect MapCastSpellMap()
        {
            return new ScenarioEventCastSpellMapEffect(
                new WorldSpellDefinitionId(RequireId(effect.TypeSpell, effect)),
                new ScenarioLocationId(RequireId(effect.IdLocation, effect)),
                MapCastingPlayer());
        }

        WorldPlayerId? MapCastingPlayer()
        {
            if (effect.IdPlayer1 is { } playerId)
            {
                return playerIds[playerId];
            }
            else
            {
                return null;
            }
        }

        ScenarioEventCreateStackEffect? MapCreateStack()
        {
            Db.Id? templateId = effect.IdStackTemplate ?? effect.IdStack;

            if (templateId is not null &&
                stackReferences.TryGetValue(templateId.Value, out ScenarioStackReference? reference) &&
                reference is ScenarioStackReference.Template template)
            {
                return new ScenarioEventCreateStackEffect(
                    template.TemplateId,
                    new ScenarioLocationId(RequireId(effect.IdLocation, effect)));
            }
            else
            {
                return null;
            }
        }

        ScenarioEventRemoveStackEffect? MapRemoveStack()
        {
            Db.Id? stackId = effect.IdStack ?? effect.IdStackTemplate;

            if (stackId is not null && stackReferences.TryGetValue(stackId.Value, out ScenarioStackReference? stack))
            {
                return new ScenarioEventRemoveStackEffect(stack, RequireBool(effect.FirstOnly, effect));
            }
            else
            {
                return null;
            }
        }

        ScenarioEventModifyVariableEffect MapModifyVariable()
        {
            ScenarioVariableId variable = new(RequireInt(effect.NumValue2, effect));

            if (!variableIds.Contains(variable))
            {
                throw new InvalidDataException($"Event effect '{effect.Id}' #{effect.Number} has an unexpected variable.");
            }

            ScenarioVariableOperation operation = MapVariableOperation();
            int value = RequireInt(effect.NumValue, effect);

            if (operation == ScenarioVariableOperation.Divide && value == 0)
            {
                throw new InvalidDataException($"Event effect '{effect.Id}' #{effect.Number} divides by zero.");
            }

            return new ScenarioEventModifyVariableEffect(variable, operation, value);
        }

        ScenarioVariableOperation MapVariableOperation()
        {
            return RequireInt(effect.Lookup, effect) switch
            {
                0 => ScenarioVariableOperation.Add,
                1 => ScenarioVariableOperation.Subtract,
                2 => ScenarioVariableOperation.Multiply,
                3 => ScenarioVariableOperation.Divide,
                4 => ScenarioVariableOperation.Replace,
                _ => throw new InvalidDataException($"Event effect '{effect.Id}' #{effect.Number} has an unexpected operation.")
            };
        }

        ScenarioEventChangeOwnerEffect? MapChangeOwner()
        {
            Db.Id? stackId = effect.IdStack ?? effect.IdStackTemplate;

            if (stackId is not null &&
                effect.IdPlayer1 is { } ownerId &&
                stackReferences.TryGetValue(stackId.Value, out ScenarioStackReference? stack) &&
                subraces.ContainsKey(new WorldSubraceId(ownerId)))
            {
                return new ScenarioEventChangeOwnerEffect(
                    stack,
                    new WorldSubraceId(ownerId),
                    RequireBool(effect.FirstOnly, effect),
                    RequireBool(effect.PlayAnim, effect));
            }
            else
            {
                return null;
            }
        }

        ScenarioEventChangeOrderEffect? MapChangeOrder()
        {
            Db.Id? stackId = effect.IdStack ?? effect.IdStackTemplate;

            if (stackId is not null && stackReferences.TryGetValue(stackId.Value, out ScenarioStackReference? stack))
            {
                Db.OrderType order = effect.Order ?? throw MakeMissingFieldError(effect);

                return new ScenarioEventChangeOrderEffect(
                    stack,
                    MapWorldStackOrder(order, effect.OrderTarget, effect.Id, locations),
                    RequireBool(effect.FirstOnly, effect));
            }
            else
            {
                return null;
            }
        }

        static Db.Id RequireId(Db.Id? id, Db.ScenarioEventEffect effect)
        {
            return id ?? throw MakeMissingFieldError(effect);
        }

        static int RequireInt(int? value, Db.ScenarioEventEffect effect)
        {
            return value ?? throw MakeMissingFieldError(effect);
        }

        static bool RequireBool(bool? value, Db.ScenarioEventEffect effect)
        {
            return value ?? throw MakeMissingFieldError(effect);
        }

        static InvalidDataException MakeMissingFieldError(Db.ScenarioEventEffect effect)
        {
            return new InvalidDataException($"Event effect '{effect.Id}' #{effect.Number} is missing a required field.");
        }
    }

    private static ImmutableArray<ScenarioEventCondition> MapEventConditions(
        IReadOnlyList<Db.ScenarioEventCondition> conditions,
        IReadOnlyDictionary<Db.Id, WorldPlayerId> playerIds,
        IReadOnlyDictionary<Db.Id, ScenarioStackReference> stackReferences,
        IReadOnlySet<ScenarioVariableId> variableIds,
        GlobalVariables globals)
    {
        ImmutableArray<ScenarioEventCondition>.Builder result = ImmutableArray.CreateBuilder<ScenarioEventCondition>(conditions.Count);

        foreach (Db.ScenarioEventCondition condition in conditions)
        {
            result.Add(MapEventCondition(condition, playerIds, stackReferences, variableIds, globals));
        }

        return result.ToImmutable();
    }

    private static ScenarioEventCondition MapEventCondition(
        Db.ScenarioEventCondition condition,
        IReadOnlyDictionary<Db.Id, WorldPlayerId> playerIds,
        IReadOnlyDictionary<Db.Id, ScenarioStackReference> stackReferences,
        IReadOnlySet<ScenarioVariableId> variableIds,
        GlobalVariables globals)
    {
        return condition.Category switch
        {
            Db.ScenarioEventConditionType.Frequency => new ScenarioEventFrequencyCondition(
                RequireInt(condition.Frequency, condition)),
            Db.ScenarioEventConditionType.EnteringZone => new ScenarioEventEnteringZoneCondition(
                new ScenarioLocationId(RequireId(condition.IdLocation, condition))),
            Db.ScenarioEventConditionType.EnteringCity => new ScenarioEventEnteringCityCondition(
                new WorldFortificationId(RequireId(condition.IdCity, condition))),
            Db.ScenarioEventConditionType.OwningCity => new ScenarioEventOwningCityCondition(
                new WorldFortificationId(RequireId(condition.IdCity, condition))),
            Db.ScenarioEventConditionType.DestroyingStack => new ScenarioEventDestroyingStackCondition(
                RequireStackReference()),
            Db.ScenarioEventConditionType.OwningItem => new ScenarioEventOwningItemCondition(
                new ItemDefinitionId(RequireId(condition.TypeItem, condition))),
            Db.ScenarioEventConditionType.LeaderOwningItem => new ScenarioEventLeaderOwningItemCondition(
                RequireStackReference(),
                new ItemDefinitionId(RequireId(condition.TypeItem, condition))),
            Db.ScenarioEventConditionType.DiplomacyRelations => new ScenarioEventDiplomacyRelationsCondition(
                playerIds[RequireId(condition.IdPlayer1, condition)],
                playerIds[RequireId(condition.IdPlayer2, condition)],
                DiplomacyCalculator.ResolveRelationLevel(RequireInt(condition.Diplomacy, condition), globals)),
            Db.ScenarioEventConditionType.Alliance => new ScenarioEventAllianceCondition(
                playerIds[RequireId(condition.IdPlayer1, condition)],
                playerIds[RequireId(condition.IdPlayer2, condition)]),
            Db.ScenarioEventConditionType.LootingRuin => new ScenarioEventLootingRuinCondition(
                new WorldFortificationId(RequireId(condition.IdRuin, condition))),
            Db.ScenarioEventConditionType.TransformingLand => new ScenarioEventTransformingLandCondition(
                RequireInt(condition.PercentLand, condition)),
            Db.ScenarioEventConditionType.VisitingSite => new ScenarioEventVisitingSiteCondition(
                new WorldSiteId(RequireId(condition.IdSite, condition))),
            Db.ScenarioEventConditionType.StackToLocation => new ScenarioEventStackToLocationCondition(
                RequireStackReference(),
                new ScenarioLocationId(RequireId(condition.IdLocation, condition))),
            Db.ScenarioEventConditionType.StackToCity => new ScenarioEventStackToCityCondition(
                RequireStackReference(),
                new WorldFortificationId(RequireId(condition.IdCity, condition))),
            Db.ScenarioEventConditionType.ItemToLocation => new ScenarioEventItemToLocationCondition(
                new ItemDefinitionId(RequireId(condition.TypeItem, condition)),
                new ScenarioLocationId(RequireId(condition.IdLocation, condition))),
            Db.ScenarioEventConditionType.StackExistence => new ScenarioEventStackExistenceCondition(
                RequireStackReference(),
                RequireExistence(condition.MiscInt, condition)),
            Db.ScenarioEventConditionType.VariableInRange => MapVariableInRange()
        };

        ScenarioEventVariableInRangeCondition MapVariableInRange()
        {
            ScenarioVariableRange range = MapVariableRange(condition.MiscInt, condition.MiscInt2, condition.MiscInt3);

            ScenarioVariableClause? second = RequireInt(condition.MiscInt7, condition) switch
            {
                0 => null,
                1 => MapVariableClause(ScenarioVariableOperator.And),
                2 => MapVariableClause(ScenarioVariableOperator.Or),
                _ => throw new InvalidDataException($"Event condition '{condition.Id}' ({condition.Category}) has an unexpected operator.")
            };

            return new ScenarioEventVariableInRangeCondition(range, second);
        }

        ScenarioVariableClause MapVariableClause(ScenarioVariableOperator variableOperator)
        {
            return new ScenarioVariableClause(
                variableOperator,
                MapVariableRange(condition.MiscInt4, condition.MiscInt5, condition.MiscInt6));
        }

        ScenarioVariableRange MapVariableRange(int? variableValue, int? minimum, int? maximum)
        {
            ScenarioVariableId variable = new(RequireInt(variableValue, condition));

            if (!variableIds.Contains(variable))
            {
                throw new InvalidDataException($"Event condition '{condition.Id}' ({condition.Category}) has an unexpected variable.");
            }

            return new ScenarioVariableRange(
                variable,
                RequireInt(minimum, condition),
                RequireInt(maximum, condition));
        }

        ScenarioStackReference RequireStackReference()
        {
            Db.Id stackId = RequireId(condition.IdStack, condition);

            if (!stackReferences.TryGetValue(stackId, out ScenarioStackReference? reference))
            {
                throw new InvalidDataException($"Event condition '{condition.Id}' ({condition.Category}) has an unexpected reference.");
            }

            return reference;
        }

        static Db.Id RequireId(Db.Id? id, Db.ScenarioEventCondition condition)
        {
            return id ?? throw MakeMissingFieldError(condition);
        }

        static int RequireInt(int? value, Db.ScenarioEventCondition condition)
        {
            return value ?? throw MakeMissingFieldError(condition);
        }

        static bool RequireExistence(int? value, Db.ScenarioEventCondition condition)
        {
            return RequireInt(value, condition) switch
            {
                0 => true,
                1 => false,
                _ => throw new InvalidDataException($"Event condition '{condition.Id}' ({condition.Category}) has an unexpected existence value.")
            };
        }

        static InvalidDataException MakeMissingFieldError(Db.ScenarioEventCondition condition)
        {
            return new InvalidDataException($"Event condition '{condition.Id}' ({condition.Category}) is missing a required field.");
        }
    }

    private static ImmutableArray<WorldRace> MapEventAffectedRaces(Db.ScenarioEvent ev)
    {
        ImmutableArray<WorldRace>.Builder result = ImmutableArray.CreateBuilder<WorldRace>();

        if (ev.Human)
        {
            result.Add(WorldRace.Human);
        }

        if (ev.Dwarf)
        {
            result.Add(WorldRace.Dwarf);
        }

        if (ev.Undead)
        {
            result.Add(WorldRace.Undead);
        }

        if (ev.Heretic)
        {
            result.Add(WorldRace.Heretic);
        }

        if (ev.Neutral)
        {
            result.Add(WorldRace.Neutral);
        }

        if (ev.Elf == true)
        {
            result.Add(WorldRace.Elf);
        }

        return result.ToImmutable();
    }

    private static ImmutableArray<WorldRace> MapEventVerifiedRaces(Db.ScenarioEvent ev)
    {
        ImmutableArray<WorldRace>.Builder result = ImmutableArray.CreateBuilder<WorldRace>();

        if (ev.VerifiedHuman)
        {
            result.Add(WorldRace.Human);
        }

        if (ev.VerifiedDwarf)
        {
            result.Add(WorldRace.Dwarf);
        }

        if (ev.VerifiedUndead)
        {
            result.Add(WorldRace.Undead);
        }

        if (ev.VerifiedHeretic)
        {
            result.Add(WorldRace.Heretic);
        }

        if (ev.VerifiedNeutral)
        {
            result.Add(WorldRace.Neutral);
        }

        if (ev.VerifiedElf == true)
        {
            result.Add(WorldRace.Elf);
        }

        return result.ToImmutable();
    }

    private static WorldPlayerId GetNeutralPlayerId(ImmutableSortedDictionary<WorldPlayerId, WorldPlayer> players)
    {
        foreach (WorldPlayer player in players.Values)
        {
            if (player.Race == WorldRace.Neutral)
            {
                return player.Id;
            }
        }

        throw new InvalidDataException("Scenario has no neutral player.");
    }

    private static WorldResourceType MapCrystalType(int resource)
    {
        return (Db.ResourceType)resource switch
        {
            Db.ResourceType.Gold => WorldResourceType.Gold,
            Db.ResourceType.Red => WorldResourceType.InfernalMana,
            Db.ResourceType.Yellow => WorldResourceType.LifeMana,
            Db.ResourceType.Orange => WorldResourceType.DeathMana,
            Db.ResourceType.White => WorldResourceType.RuneMana,
            Db.ResourceType.Blue => WorldResourceType.GroveMana,
            _ => throw new InvalidDataException($"Unexpected crystal resource '{resource}'.")
        };
    }

    private static WorldMap LoadMap(
        GlobalDatabase globalDb,
        ScenarioDatabase scenarioDb,
        Db.ScenarioId scenarioId,
        Db.ScenarioInfo scenarioInfo)
    {
        int mapSize = scenarioInfo.MapSize;

        ScenarioMapDecoder.TileInfo[] tileInfos =
            ScenarioMapDecoder.DecodeMap(mapSize, scenarioDb.ScenarioMapBlocksByScenario[scenarioId]);

        WorldMapCell[] cells = new WorldMapCell[mapSize * mapSize];

        for (int i = 0; i < cells.Length; i++)
        {
            ScenarioMapDecoder.TileInfo tileInfo = tileInfos[i];
            WorldMapTerrain cellTerrain = MapTerrain(tileInfo);
            WorldMapFeature cellFeature = MapFeature(tileInfo);

            cells[i] = new WorldMapCell(cellTerrain, cellFeature, new WorldObjectVariant(tileInfo.ImageVariant));
        }

        ApplyMapElements(
            cells,
            mapSize,
            scenarioDb.ScenarioMapElementsByScenario[scenarioId],
            GetScenarioObjects(scenarioDb.ScenarioLandmarksByScenarioAndLandmarkId, scenarioId),
            globalDb.Landmarks);

        foreach (Db.ScenarioRoad road in scenarioDb.ScenarioRoadsByScenario[scenarioId])
        {
            int index = road.PositionY * mapSize + road.PositionX;
            cells[index] = cells[index] with { Feature = WorldMapFeature.Road };
        }

        ApplyScenarioMountains(cells, mapSize, scenarioDb.ScenarioMountainsByScenario[scenarioId]);

        return new WorldMap(mapSize, mapSize, [.. cells]);
    }

    private static WorldMapFeature MapFeature(ScenarioMapDecoder.TileInfo tileInfo)
    {
        return tileInfo.GroundId switch
        {
            0 => WorldMapFeature.None,
            1 => WorldMapFeature.Forest,
            3 => WorldMapFeature.None,
            4 => WorldMapFeature.Mountain,
            _ => throw new InvalidDataException($"Unexpected ground '{tileInfo.GroundId}'.")
        };
    }

    private static WorldMapTerrain MapTerrain(ScenarioMapDecoder.TileInfo tileInfo)
    {
        WorldMapTerrain? result = tileInfo.GroundId switch
        {
            3 => WorldMapTerrain.Water,
            _ => null
        };

        if (result is not null)
        {
            return result.Value;
        }

        return MapTerrain(tileInfo.TerrainId);
    }

    private static WorldMapTerrain MapTerrain(int terrainId)
    {
        return terrainId switch
        {
            1 => WorldMapTerrain.Human,
            2 => WorldMapTerrain.Dwarf,
            3 => WorldMapTerrain.Heretic,
            4 => WorldMapTerrain.Undead,
            5 => WorldMapTerrain.Neutral,
            6 => WorldMapTerrain.Elf,
            _ => throw new InvalidDataException($"Unexpected terrain '{terrainId}'.")
        };
    }

    private static void ApplyMapElements(
        WorldMapCell[] cells,
        int mapSize,
        IEnumerable<Db.ScenarioMapElement> mapElements,
        IReadOnlyDictionary<Db.Id, Db.ScenarioLandmark> scenarioLandmarks,
        IReadOnlyDictionary<Db.Id, Db.Landmark> globalLandmarks)
    {
        foreach (Db.ScenarioMapElement mapElement in mapElements)
        {
            if (scenarioLandmarks.TryGetValue(mapElement.ElementId, out Db.ScenarioLandmark? scenarioLandmark))
            {
                Db.Landmark landmark = globalLandmarks[scenarioLandmark.TypeId];

                if (landmark.Mountain == true)
                {
                    WorldPosition position = new(mapElement.PositionX, mapElement.PositionY);
                    if (position.X < 0 || position.X >= mapSize || position.Y < 0 || position.Y >= mapSize)
                    {
                        continue;
                    }

                    int cellIndex = position.Y * mapSize + position.X;
                    cells[cellIndex] = cells[cellIndex] with { Feature = WorldMapFeature.Mountain };
                }
            }
        }
    }

    private static void ApplyScenarioMountains(
        WorldMapCell[] cells,
        int mapSize,
        IEnumerable<Db.ScenarioMountain> mountains)
    {
        foreach (Db.ScenarioMountain mountain in mountains)
        {
            int startX = Math.Max(0, mountain.PositionX);
            int startY = Math.Max(0, mountain.PositionY);
            int endX = Math.Min(mapSize, mountain.PositionX + mountain.SizeX);
            int endY = Math.Min(mapSize, mountain.PositionY + mountain.SizeY);

            for (int y = startY; y < endY; y++)
            {
                for (int x = startX; x < endX; x++)
                {
                    int cellIndex = y * mapSize + x;
                    cells[cellIndex] = cells[cellIndex] with { Feature = WorldMapFeature.Mountain };
                }
            }
        }
    }

    private static ImmutableSortedDictionary<WorldPlayerId, WorldPlayer> LoadPlayers(
        GlobalDatabase globalDb,
        ScenarioDatabase scenarioDb,
        CampaignDatabase campaignDb,
        Db.ScenarioId scenarioId,
        Db.ScenarioInfo scenarioInfo,
        IReadOnlyDictionary<Db.Id, WorldPlayerId> playerIds,
        GlobalVariables globals,
        WorldDifficulty difficulty,
        ScenarioConfiguration? configuration)
    {
        ImmutableSortedDictionary<WorldPlayerId, WorldPlayer>.Builder result =
            ImmutableSortedDictionary.CreateBuilder<WorldPlayerId, WorldPlayer>();

        Db.Campaign campaign = campaignDb.Campaigns[scenarioInfo.CampaignId];

        IReadOnlyDictionary<Db.Id, ScenarioPlayerConfiguration?> humanPlayers = GetHumanPlayers(
            globalDb,
            campaign,
            scenarioInfo,
            [.. scenarioDb.ScenarioPlayersByScenario[scenarioId]],
            configuration);

        foreach (Db.ScenarioPlayer player in scenarioDb.ScenarioPlayersByScenario[scenarioId])
        {
            WorldPlayerId playerId = playerIds[player.PlayerId];
            bool isAi = !humanPlayers.TryGetValue(player.PlayerId, out ScenarioPlayerConfiguration? playerConfiguration);
            ImmutableBitmap fog = ImmutableBitmap.Create(scenarioInfo.MapSize, scenarioInfo.MapSize);
            WorldRace race = MapRace(globalDb.Races[player.RaceId].RaceType);
            (Db.Id lordId, int lordFaceVariant) = GetLord(globalDb, player, playerConfiguration);

            ImmutableSortedSet<WorldPlayerBuildingDefinitionId> availableBuildings = globalDb.BuildingLists
                .Where(building => building.LordId == lordId)
                .Where(building =>
                {
                    Db.Building definition = globalDb.Buildings[building.BuildingId];

                    if (definition.Category == Db.BuildingType.Unit &&
                        definition.Level > scenarioInfo.MaxUnit)
                    {
                        return false;
                    }

                    return true;
                })
                .Select(building => new WorldPlayerBuildingDefinitionId(building.BuildingId))
                .ToImmutableSortedSet();

            ImmutableSortedSet<WorldSpellDefinitionId> availableSpells = globalDb.SpellResearches
                .Where(research => research.LordId == lordId)
                .Where(research => globalDb.Spells[research.SpellId].Level <= scenarioInfo.MaxSpell)
                .Select(research => new WorldSpellDefinitionId(research.SpellId))
                .ToImmutableSortedSet();

            ILookup<Db.Id, Db.ScenarioSpell> knownSpellsByGroup =
                GetScenarioObjectsLookup(scenarioDb.ScenarioSpellsByScenarioAndKnownSpellsId, scenarioId);

            ImmutableSortedSet<WorldSpellDefinitionId> knownSpells = knownSpellsByGroup[player.KnownId]
                .Select(spell => new WorldSpellDefinitionId(spell.SpellId))
                .ToImmutableSortedSet();

            WorldPlayerBuildings buildings = new(availableBuildings, []);
            WorldPlayerSpellbook spellbook = new(availableSpells, knownSpells);

            WorldResourceSet resources = ResourceSetCalculator.Add(MapResourceSet(player.Bank), MapResourceSet(player.SpellBank));

            if (!isAi)
            {
                int startingPercent = GetStartingResourcesPercent(difficulty, globals);
                resources = ResourceSetCalculator.ScalePercent(resources, startingPercent);
            }

            result.Add(playerId, new WorldPlayer(
                playerId,
                ScenarioTextId.MakeInternal(player.PlayerId, Db.TextScenario.NameTextId, TextSequenceZero),
                new LordDefinitionId(lordId),
                race,
                new WorldObjectVariant(lordFaceVariant),
                MapAiAttitude(player.Attitude),
                isAi,
                resources,
                buildings,
                spellbook,
                fog,
                MapSpiedPlayers(player, playerIds),
                [],
                [],
                new WorldPlayerTurnActions
                {
                    BuildingConstructed = player.ConstructTurn == Math.Max(scenarioInfo.CurrentTurn, 1),
                    SpellResearched = player.ResearchTurn == Math.Max(scenarioInfo.CurrentTurn, 1)
                }));
        }

        return result.ToImmutable();
    }

    private static (Db.Id LordId, int LordFaceVariant) GetLord(
        GlobalDatabase globalDb,
        Db.ScenarioPlayer player,
        ScenarioPlayerConfiguration? configuration)
    {
        if (configuration is not null)
        {
            foreach (Db.Lord lord in globalDb.Lords.Values)
            {
                if (lord.RaceId == player.RaceId && MapLordCategory(lord.Category) == configuration.LordCategory)
                {
                    return (lord.LordId, configuration.LordFaceVariant.Value);
                }
            }

            throw new InvalidDataException($"Lord '{configuration.LordCategory}' for race '{player.RaceId}' was not found.");
        }
        else
        {
            return (player.LordId, player.Face);
        }
    }

    private static LordCategory MapLordCategory(Db.LordType lordType)
    {
        return lordType switch
        {
            Db.LordType.Mage => LordCategory.Mage,
            Db.LordType.Warrior => LordCategory.Warrior,
            Db.LordType.Diplomat => LordCategory.Guildmaster
        };
    }

    private static int GetStartingResourcesPercent(WorldDifficulty difficulty, GlobalVariables globals)
    {
        return difficulty switch
        {
            WorldDifficulty.Easy => globals.StartingResourcesPercentEasy,
            WorldDifficulty.Average => globals.StartingResourcesPercentAverage,
            WorldDifficulty.Hard => globals.StartingResourcesPercentHard,
            WorldDifficulty.VeryHard => globals.StartingResourcesPercentVeryHard
        };
    }

    private static IReadOnlyDictionary<Db.Id, ScenarioPlayerConfiguration?> GetHumanPlayers(
        GlobalDatabase globalDb,
        Db.Campaign campaign,
        Db.ScenarioInfo scenarioInfo,
        IReadOnlyList<Db.ScenarioPlayer> players,
        ScenarioConfiguration? configuration)
    {
        Dictionary<Db.Id, ScenarioPlayerConfiguration?> result = [];

        if (configuration is not null)
        {
            if (configuration.HumanPlayers.IsEmpty)
            {
                throw new InvalidOperationException("Scenario configuration requires a human player.");
            }

            foreach (ScenarioPlayerConfiguration playerConfiguration in configuration.HumanPlayers)
            {
                Db.ScenarioPlayer player = GetConfiguredHumanPlayer(globalDb, scenarioInfo, players, playerConfiguration.Race);

                if (!result.TryAdd(player.PlayerId, playerConfiguration))
                {
                    throw new InvalidOperationException($"Race '{playerConfiguration.Race}' is configured more than once.");
                }
            }
        }
        else
        {
            IEnumerable<Db.ScenarioPlayer> humans;

            if (players.Any(player => player.IsHuman))
            {
                humans = players.Where(player => player.IsHuman);
            }
            else if (campaign.Skirmish)
            {
                humans = players.Where(player => GetRace(player) != Db.RaceType.Neutral).Take(1);
            }
            else
            {
                humans = players.Where(player => GetRace(player) == campaign.PlayRace);
            }

            foreach (Db.ScenarioPlayer human in humans)
            {
                result.Add(human.PlayerId, null);
            }
        }

        return result;

        Db.RaceType GetRace(Db.ScenarioPlayer player)
        {
            return globalDb.Races[player.RaceId].RaceType;
        }
    }

    private static Db.ScenarioPlayer GetConfiguredHumanPlayer(
        GlobalDatabase globalDb,
        Db.ScenarioInfo scenarioInfo,
        IReadOnlyList<Db.ScenarioPlayer> players,
        WorldRace race)
    {
        IReadOnlyList<ScenarioRace> playableRaces = LoadPlayableRaces(globalDb, scenarioInfo);

        if (!ContainsRace(playableRaces, race))
        {
            throw new InvalidOperationException($"Race '{race}' is not playable.");
        }

        foreach (Db.ScenarioPlayer player in players)
        {
            Db.Race playerRace = globalDb.Races[player.RaceId];

            if (MapRace(playerRace.RaceType) == race)
            {
                return player;
            }
        }

        throw new InvalidDataException($"Scenario has no player of race '{race}'.");
    }

    private static bool ContainsRace(IReadOnlyList<ScenarioRace> races, WorldRace race)
    {
        foreach (ScenarioRace candidate in races)
        {
            if (candidate.Race == race)
            {
                return true;
            }
        }

        return false;
    }

    private static IReadOnlyList<ScenarioRace> LoadPlayableRaces(IGlobalDatabaseIndex globalDb, Db.ScenarioInfo scenarioInfo)
    {
        int[] playerRaces =
        [
            scenarioInfo.Player1,
            scenarioInfo.Player2,
            scenarioInfo.Player3,
            scenarioInfo.Player4,
            scenarioInfo.Player5,
            scenarioInfo.Player6,
            scenarioInfo.Player7,
            scenarioInfo.Player8,
            scenarioInfo.Player9,
            scenarioInfo.Player10,
            scenarioInfo.Player11,
            scenarioInfo.Player12,
            scenarioInfo.Player13
        ];

        List<ScenarioRace> result = [];

        foreach (int playerRace in playerRaces)
        {
            if (playerRace != PlayerRaceUnset)
            {
                Db.Race race = FindRaceByType(globalDb, (Db.RaceType)playerRace);

                if (race.Playable)
                {
                    result.Add(new ScenarioRace(MapRace(race.RaceType), new GlobalTextId(race.NameText)));
                }
            }
        }

        return result;
    }

    private static Db.Race FindRaceByType(IGlobalDatabaseIndex globalDb, Db.RaceType raceType)
    {
        foreach (Db.Race race in globalDb.Races.Values)
        {
            if (race.RaceType == raceType)
            {
                return race;
            }
        }

        throw new InvalidDataException($"Race '{raceType}' was not found.");
    }

    private static ImmutableSortedDictionary<WorldFortificationId, WorldFortification> LoadFortifications(
        GlobalDatabase globalDb,
        ScenarioDatabase scenarioDb,
        Db.ScenarioId scenarioId,
        IReadOnlyDictionary<Db.Id, WorldPlayerId> playerIds,
        IGameDefinitionProvider definitions,
        GlobalVariables globals)
    {
        ImmutableSortedDictionary<WorldFortificationId, WorldFortification>.Builder result = ImmutableSortedDictionary.CreateBuilder<WorldFortificationId, WorldFortification>();

        IReadOnlyDictionary<Db.Id, Db.ScenarioUnit> scenarioUnits = GetScenarioObjects(scenarioDb.ScenarioUnitsByScenarioAndUnitId, scenarioId);
        IReadOnlyDictionary<Db.Id, Db.ScenarioUnitGroup> scenarioUnitGroups = GetScenarioObjects(scenarioDb.ScenarioUnitGroupsByScenarioAndGroupId, scenarioId);
        IReadOnlyDictionary<Db.Id, Db.ScenarioSubrace> scenarioSubraces = GetScenarioObjects(scenarioDb.ScenarioSubracesByScenarioAndSubraceId, scenarioId);
        IReadOnlyDictionary<Db.Id, Db.ScenarioPlayer> scenarioPlayers = GetScenarioObjects(scenarioDb.ScenarioPlayersByScenarioAndPlayerId, scenarioId);
        ILookup<Db.Id, Db.ScenarioUnitModifier> scenarioUnitModifiers = GetScenarioObjectsLookup(scenarioDb.ScenarioUnitModifiersByScenarioAndUnitId, scenarioId);
        ILookup<Db.Id, Db.TextScenario> unitNames = GetScenarioTextsLookup(scenarioDb, scenarioId, Db.TextScenario.NameTextId);
        ILookup<Db.Id, Db.ScenarioInventory> inventories = GetScenarioObjectsLookup(scenarioDb.ScenarioInventoriesByScenarioAndInventoryId, scenarioId);
        IReadOnlyDictionary<Db.Id, Db.ScenarioItem> items = GetScenarioObjects(scenarioDb.ScenarioItemsByScenarioAndItemId, scenarioId);
        IReadOnlyDictionary<Db.Id, Db.ScenarioTalismanCharges> talismanCharges = GetScenarioObjects(scenarioDb.ScenarioTalismanChargesByScenarioAndIdTalisman, scenarioId);

        foreach (Db.ScenarioFortification fortification in scenarioDb.ScenarioFortificationsByScenario[scenarioId])
        {
            WorldPlayerId ownerId = playerIds[fortification.OwnerId];

            int footprintSize;
            if (fortification.Size is null)
            {
                footprintSize = FortificationCalculator.CapitalFootprint;
            }
            else
            {
                footprintSize = FortificationCalculator.CityFootprint;
            }

            WorldFootprint footprint = new(new WorldPosition(fortification.PositionX, fortification.PositionY), footprintSize, footprintSize);
            Db.ScenarioUnitGroup? group = scenarioUnitGroups.GetValueOrDefault(fortification.CityId);
            UnitGrid<WorldUnit> garrison = LoadUnitGrid(group, scenarioUnits, scenarioUnitModifiers, unitNames, definitions);
            WorldInventory inventory = LoadInventory(fortification.CityId, inventories, items, talismanCharges, definitions, globals);
            Db.ScenarioSubrace subrace = scenarioSubraces[fortification.SubraceId];
            Db.ScenarioPlayer player = scenarioPlayers[subrace.PlayerId];
            WorldRace race = MapRace(globalDb.Races[player.RaceId].RaceType);
            ScenarioTextId nameTextId = ScenarioTextId.MakeInternal(fortification.CityId, Db.TextScenario.NameTextId, TextSequenceZero);

            if (fortification.Size is null)
            {
                WorldFortificationId capitalId = new(fortification.CityId);
                Db.ScenarioPlayer owner = scenarioPlayers[fortification.OwnerId];
                WorldPlayerId? capturedById;

                if (owner.CapturedById is not null)
                {
                    capturedById = playerIds[owner.CapturedById.Value];
                }
                else
                {
                    capturedById = null;
                }

                WorldCapital capital = new(
                    capitalId,
                    nameTextId,
                    ownerId,
                    footprint,
                    garrison,
                    inventory,
                    fortification.AiPriority,
                    capturedById);

                result.Add(capitalId, capital);
            }
            else
            {
                WorldFortificationId cityId = new(fortification.CityId);

                WorldCityDefinitionId definitionId = new(race, fortification.Size.Value);
                definitions.GetCity(definitionId);

                WorldCity city = new(
                    cityId,
                    nameTextId,
                    definitionId,
                    ownerId,
                    new WorldSubraceId(fortification.SubraceId),
                    footprint,
                    garrison,
                    inventory,
                    fortification.AiPriority,
                    fortification.RiotTurns ?? 0,
                    fortification.GrowthTurn ?? 0);

                result.Add(cityId, city);
            }
        }

        foreach (Db.ScenarioRuin ruin in scenarioDb.ScenarioRuinsByScenario[scenarioId])
        {
            WorldFortificationId ruinId = new(ruin.RuinId);
            ScenarioTextId nameTextId = ScenarioTextId.MakeInternal(ruin.RuinId, Db.TextScenario.RuinTitleTextId, TextSequenceZero);
            WorldFootprint footprint = new(
                new WorldPosition(ruin.PositionX, ruin.PositionY),
                FortificationCalculator.RuinFootprint,
                FortificationCalculator.RuinFootprint);

            Db.ScenarioUnitGroup? group = scenarioUnitGroups.GetValueOrDefault(ruin.RuinId);
            UnitGrid<WorldUnit> garrison = LoadUnitGrid(group, scenarioUnits, scenarioUnitModifiers, unitNames, definitions);

            WorldResourceSet resources = MapResourceSet(Db.ResourceSet.Parse(ruin.Cash));

            ItemDefinitionId? item = ConvertOptionalId(ruin.Item, static id => new ItemDefinitionId(id));
            WorldPlayerId? looter = ConvertOptionalId(ruin.Looter, id => playerIds[id]);

            result.Add(ruinId, new WorldRuin(ruinId, nameTextId, footprint, new WorldObjectVariant(ruin.Image), garrison, resources, item, looter, ruin.AiPriority));
        }

        return result.ToImmutable();
    }

    private static ImmutableSortedDictionary<WorldStackId, WorldStack> LoadStacks(
        ScenarioDatabase scenarioDb,
        Db.ScenarioId scenarioId,
        IReadOnlyDictionary<Db.Id, WorldPlayerId> playerIds,
        IGameDefinitionProvider definitions,
        GlobalVariables globals,
        ImmutableSortedDictionary<WorldFortificationId, WorldFortification> fortifications,
        IReadOnlyDictionary<Db.Id, Db.ScenarioLocation> locations)
    {
        ImmutableSortedDictionary<WorldStackId, WorldStack>.Builder result = ImmutableSortedDictionary.CreateBuilder<WorldStackId, WorldStack>();

        IReadOnlyDictionary<Db.Id, Db.ScenarioUnit> units = GetScenarioObjects(scenarioDb.ScenarioUnitsByScenarioAndUnitId, scenarioId);
        IReadOnlyDictionary<Db.Id, Db.ScenarioUnitGroup> unitGroups = GetScenarioObjects(scenarioDb.ScenarioUnitGroupsByScenarioAndGroupId, scenarioId);
        ILookup<Db.Id, Db.ScenarioUnitModifier> unitModifiers = GetScenarioObjectsLookup(scenarioDb.ScenarioUnitModifiersByScenarioAndUnitId, scenarioId);
        ILookup<Db.Id, Db.TextScenario> unitNames = GetScenarioTextsLookup(scenarioDb, scenarioId, Db.TextScenario.NameTextId);
        ILookup<Db.Id, Db.ScenarioInventory> inventories = GetScenarioObjectsLookup(scenarioDb.ScenarioInventoriesByScenarioAndInventoryId, scenarioId);
        IReadOnlyDictionary<Db.Id, Db.ScenarioItem> items = GetScenarioObjects(scenarioDb.ScenarioItemsByScenarioAndItemId, scenarioId);
        IReadOnlyDictionary<Db.Id, Db.ScenarioTalismanCharges> talismanCharges = GetScenarioObjects(scenarioDb.ScenarioTalismanChargesByScenarioAndIdTalisman, scenarioId);

        foreach (Db.ScenarioStack stack in scenarioDb.ScenarioStacksByScenario[scenarioId])
        {
            WorldStackId stackId = new(stack.StackId);
            WorldPlayerId ownerId = playerIds[stack.OwnerId];
            WorldPosition position = GetStackPosition(stack, fortifications);
            LeaderEquipment equipment = LoadEquipment(stack, items, talismanCharges, definitions, globals);
            WorldInventory inventory = LoadInventory(stack.StackId, inventories, items, talismanCharges, definitions, globals);
            WorldSubraceId subraceId = new(stack.SubraceId);
            WorldCanonicalStackOrder order = MapWorldStackOrder(stack.Order, stack.OrderTargetId, stack.StackId, locations);
            ScenarioStackTemplateId? sourceTemplateId = ConvertOptionalId(stack.SourceTemplateId, static id => new ScenarioStackTemplateId(id));

            UnitGrid<WorldUnit> grid = LoadUnitGrid(unitGroups.GetValueOrDefault(stack.StackId), units, unitModifiers, unitNames, definitions);
            grid = LeaderEquipmentCalculator.UpdateUnitModifiers(grid, equipment, definitions);

            int movementPoints = ResolveMovementPoints(stack.Move, grid, definitions, globals);

            result.Add(stackId, new WorldStack(
                stackId,
                ownerId,
                position,
                (WorldStackDirection)stack.Facing.Mod(8),
                grid,
                movementPoints,
                inventory,
                equipment,
                subraceId,
                stack.AiPriority,
                stack.AiIgnore,
                stack.Invisible,
                order,
                null,
                stack.UpgradeCount,
                stack.NumberOfBattles,
                sourceTemplateId));
        }

        return result.ToImmutable();
    }

    private static ImmutableSortedDictionary<WorldStackId, WorldStack> ImportLeader(
        ImmutableSortedDictionary<WorldStackId, WorldStack> stacks,
        ImmutableSortedDictionary<WorldPlayerId, WorldPlayer> players,
        ImmutableSortedDictionary<WorldFortificationId, WorldFortification> fortifications,
        ScenarioLeader scenarioLeader,
        IGameDefinitionProvider definitions,
        GlobalVariables globals)
    {
        WorldStack stack = FindImportStack(stacks, players, fortifications);
        (WorldUnit placeholder, _, _) = stack.GetLeader(definitions);

        WorldStackBuilder builder = new(definitions);

        foreach ((WorldUnit unit, GridPosition position) in stack.Units.GetUnits())
        {
            if (ReferenceEquals(unit, placeholder))
            {
                builder.PlaceUnit(scenarioLeader.Unit with { Id = placeholder.Id }, position);
            }
            else
            {
                builder.PlaceUnit(unit, position);
            }
        }

        UnitGrid<WorldUnit> units = LeaderEquipmentCalculator.UpdateUnitModifiers(builder.Build(), scenarioLeader.Equipment, definitions);

        stack = stack with { Units = units };

        (WorldUnit leader, _, UnitDefinition leaderDefinition) = stack.GetLeader(definitions);
        UnitStats leaderStats = UnitStatsCalculator.GetStats(leaderDefinition, leader.Level, leader.Modifiers, definitions, globals);
        int movementPoints = UnitStatsCalculator.GetMaxMovementPoints(leaderDefinition, leader.Level, leader.Modifiers, definitions, globals);

        stack = stack with
        {
            Units = units.Replace(leader, leader with { Hitpoints = leaderStats.MaxHitpoints }),
            MovementPoints = movementPoints,
            Inventory = stack.Inventory.AddRange(stack.Equipment.Items.Values).AddRange(scenarioLeader.Inventory.Items),
            Equipment = scenarioLeader.Equipment,
            PendingLeaderUpgrades = scenarioLeader.PendingLeaderUpgrades,
            BattlesWon = scenarioLeader.BattlesWon
        };

        return stacks.SetItem(stack.Id, stack);
    }

    private static WorldStack FindImportStack(
        ImmutableSortedDictionary<WorldStackId, WorldStack> stacks,
        ImmutableSortedDictionary<WorldPlayerId, WorldPlayer> players,
        ImmutableSortedDictionary<WorldFortificationId, WorldFortification> fortifications)
    {
        foreach (WorldPlayer player in players.Values)
        {
            if (player.IsAi)
            {
                continue;
            }

            foreach (WorldFortification fortification in fortifications.Values)
            {
                if (fortification is WorldCapital capital && capital.OwnerId == player.Id)
                {
                    foreach (WorldStack stack in stacks.Values)
                    {
                        if (stack.OwnerId == player.Id && stack.Position == capital.Entrance)
                        {
                            return stack;
                        }
                    }
                }
            }
        }

        throw new InvalidDataException("Scenario has no initial leader.");
    }

    private static List<ScenarioStackTemplateDefinition> LoadStackTemplates(
        ScenarioDatabase scenarioDb,
        Db.ScenarioId scenarioId,
        IReadOnlyDictionary<Db.Id, WorldPlayerId> playerIds,
        IGameDefinitionProvider definitions,
        GlobalVariables globals,
        IReadOnlyDictionary<Db.Id, Db.ScenarioLocation> locations)
    {
        List<ScenarioStackTemplateDefinition> result = [];

        ILookup<Db.Id, Db.ScenarioStackTemplateModifier> templateModifiers =
            GetScenarioObjectsLookup(scenarioDb.ScenarioStackTemplateModifiersByScenarioAndTemplateId, scenarioId);

        foreach (Db.ScenarioStackTemplate template in scenarioDb.ScenarioStackTemplatesByScenario[scenarioId])
        {
            UnitGrid<ScenarioStackTemplateUnit> units = LoadStackTemplateUnits(template, templateModifiers[template.Id], definitions, globals);

            WorldStackDirection? direction = null;
            if (template.UseFacing)
            {
                direction = (WorldStackDirection)template.Facing.Mod(8);
            }

            result.Add(new ScenarioStackTemplateDefinition(
                new ScenarioStackTemplateId(template.Id),
                playerIds[template.OwnerId],
                new WorldSubraceId(template.SubraceId),
                units,
                MapWorldStackOrder(template.Order, template.OrderTargetId, template.Id, locations),
                direction,
                template.AiPriority));
        }

        return result;
    }

    private static UnitGrid<ScenarioStackTemplateUnit> LoadStackTemplateUnits(
        Db.ScenarioStackTemplate template,
        IEnumerable<Db.ScenarioStackTemplateModifier> templateModifiers,
        IGameDefinitionProvider definitions,
        GlobalVariables globals)
    {
        Db.Id?[] unitIds = [template.Unit0Id, template.Unit1Id, template.Unit2Id, template.Unit3Id, template.Unit4Id, template.Unit5Id];
        int[] unitLevels = [template.Unit0Level, template.Unit1Level, template.Unit2Level, template.Unit3Level, template.Unit4Level, template.Unit5Level];
        int[] positions = [template.Position0, template.Position1, template.Position2, template.Position3, template.Position4, template.Position5];
        ILookup<int, Db.ScenarioStackTemplateModifier> modifiersByPosition = templateModifiers.ToLookup(modifier => modifier.UnitPos);

        UnitGrid<ScenarioStackTemplateUnit>.Builder builder = new();
        HashSet<int> placedUnits = [];

        for (int position = 0; position < GridPosition.Count; position++)
        {
            int unitIndex = positions[position];

            if (unitIndex < 0 || unitIndex >= GridPosition.Count || !placedUnits.Add(unitIndex))
            {
                continue;
            }

            if (unitIds[unitIndex] is not { } unitId)
            {
                continue;
            }

            UnitDefinitionId definitionId = new(unitId);
            UnitDefinition definition = definitions.GetUnit(definitionId);
            ImmutableArray<UnitModifier> modifiers = LoadStackTemplateUnitModifiers(modifiersByPosition[position], definitions);
            int level = unitLevels[unitIndex];
            int hitpoints = UnitStatsCalculator.GetStats(definition, level, modifiers, definitions, globals).MaxHitpoints;

            builder.Place(
                new ScenarioStackTemplateUnit(definitionId, hitpoints, level, modifiers),
                new GridPosition(position % 2, position / 2),
                definition.IsLarge);
        }

        return builder.Build();
    }

    private static ImmutableArray<UnitModifier> LoadStackTemplateUnitModifiers(
        IEnumerable<Db.ScenarioStackTemplateModifier> modifiers,
        IGameDefinitionProvider definitions)
    {
        ImmutableArray<UnitModifier>.Builder result = ImmutableArray.CreateBuilder<UnitModifier>();

        foreach (Db.ScenarioStackTemplateModifier modifier in modifiers)
        {
            UnitModifierDefinitionId modifierId = new(modifier.ModifierId);
            definitions.GetUnitModifier(modifierId);

            result.Add(UnitModifier.FromScenario(modifierId));
        }

        return result.ToImmutable();
    }

    private static int ResolveMovementPoints(int movementPoints, UnitGrid<WorldUnit> grid, IGameDefinitionProvider definitions, GlobalVariables globals)
    {
        foreach ((WorldUnit unit, _) in grid.GetUnits())
        {
            UnitDefinition definition = definitions.GetUnit(unit.DefinitionId);

            if (definition.Leadership is > 0)
            {
                return Math.Min(movementPoints, UnitStatsCalculator.GetMaxMovementPoints(definition, unit.Level, unit.Modifiers, definitions, globals));
            }
        }

        return movementPoints;
    }

    private static ImmutableSortedDictionary<WorldRodId, WorldRod> LoadRods(
        ScenarioDatabase scenarioDb,
        Db.ScenarioId scenarioId,
        ImmutableDictionary<Db.Id, WorldPlayerId> playerIds)
    {
        ImmutableSortedDictionary<WorldRodId, WorldRod>.Builder result =
            ImmutableSortedDictionary.CreateBuilder<WorldRodId, WorldRod>();

        foreach (Db.ScenarioRod rod in scenarioDb.ScenarioRodsByScenario[scenarioId])
        {
            WorldRodId rodId = new(rod.RodId);
            WorldPosition position = new(rod.PositionX, rod.PositionY);

            result.Add(rodId, new WorldRod(rodId, playerIds[rod.OwnerId], position));
        }

        return result.ToImmutable();
    }

    private static ImmutableSortedDictionary<WorldBagId, WorldBag> LoadBags(
        ScenarioDatabase scenarioDb,
        Db.ScenarioId scenarioId,
        IGameDefinitionProvider definitions,
        GlobalVariables globals)
    {
        ImmutableSortedDictionary<WorldBagId, WorldBag>.Builder result =
            ImmutableSortedDictionary.CreateBuilder<WorldBagId, WorldBag>();

        ILookup<Db.Id, Db.ScenarioInventory> inventories = GetScenarioObjectsLookup(scenarioDb.ScenarioInventoriesByScenarioAndInventoryId, scenarioId);
        IReadOnlyDictionary<Db.Id, Db.ScenarioItem> items = GetScenarioObjects(scenarioDb.ScenarioItemsByScenarioAndItemId, scenarioId);
        IReadOnlyDictionary<Db.Id, Db.ScenarioTalismanCharges> talismanCharges = GetScenarioObjects(scenarioDb.ScenarioTalismanChargesByScenarioAndIdTalisman, scenarioId);

        foreach (Db.ScenarioBag bag in scenarioDb.ScenarioBagsByScenario[scenarioId])
        {
            WorldBagId bagId = new(bag.BagId);
            WorldPosition position = new(bag.PositionX, bag.PositionY);
            WorldInventory inventory = LoadInventory(bag.BagId, inventories, items, talismanCharges, definitions, globals);

            result.Add(bagId, new WorldBag(bagId, position, new WorldObjectVariant(bag.Image), inventory, bag.AiPriority));
        }

        return result.ToImmutable();
    }

    private static ImmutableSortedDictionary<WorldLandmarkId, WorldLandmark> LoadLandmarks(
        GlobalDatabase globalDb,
        ScenarioDatabase scenarioDb,
        Db.ScenarioId scenarioId)
    {
        ImmutableSortedDictionary<WorldLandmarkId, WorldLandmark>.Builder result =
            ImmutableSortedDictionary.CreateBuilder<WorldLandmarkId, WorldLandmark>();

        IReadOnlyDictionary<Db.Id, Db.ScenarioLandmark> landmarks =
            GetScenarioObjects(scenarioDb.ScenarioLandmarksByScenarioAndLandmarkId, scenarioId);

        foreach (Db.ScenarioLandmark landmark in landmarks.Values)
        {
            Db.Landmark definition = globalDb.Landmarks[landmark.TypeId];

            WorldLandmarkId landmarkId = new(landmark.LandmarkId);
            WorldPosition landmarkPosition = new(landmark.PositionX, landmark.PositionY);
            WorldLandmark worldLandmark = new(
                landmarkId,
                new WorldLandmarkDefinitionId(landmark.TypeId),
                new WorldFootprint(landmarkPosition, definition.Cx, definition.Cy));

            result.Add(landmarkId, worldLandmark);
        }

        return result.ToImmutable();
    }

    private static ImmutableSortedDictionary<WorldMountainId, WorldMountain> LoadMountains(
        ScenarioDatabase scenarioDb,
        Db.ScenarioId scenarioId,
        WorldMap map)
    {
        ImmutableSortedDictionary<WorldMountainId, WorldMountain>.Builder result =
            ImmutableSortedDictionary.CreateBuilder<WorldMountainId, WorldMountain>();

        foreach (Db.ScenarioMountain mountain in scenarioDb.ScenarioMountainsByScenario[scenarioId])
        {
            WorldFootprint footprint = new(
                new WorldPosition(mountain.PositionX, mountain.PositionY),
                mountain.SizeX,
                mountain.SizeY);

            if (mountain.PositionX < map.Width &&
                mountain.PositionY < map.Height &&
                mountain.PositionX + mountain.SizeX > 0 &&
                mountain.PositionY + mountain.SizeY > 0)
            {
                WorldMountainId mountainId = new(mountain.IdMountain);

                result.Add(mountainId, new WorldMountain(
                    mountainId,
                    footprint,
                    MapRace((Db.RaceType)mountain.Race),
                    new WorldObjectVariant(mountain.Image)));
            }
        }

        return result.ToImmutable();
    }

    private static ImmutableSortedDictionary<WorldRelationId, WorldRelation> LoadRelations(
        ScenarioDatabase scenarioDb,
        Db.ScenarioId scenarioId)
    {
        ImmutableSortedDictionary<WorldRelationId, WorldRelation>.Builder result =
            ImmutableSortedDictionary.CreateBuilder<WorldRelationId, WorldRelation>();

        foreach (Db.ScenarioRelation relation in scenarioDb.ScenarioRelationsByScenario[scenarioId])
        {
            WorldRelationId relationId = new(MapRace(relation.Race1), MapRace(relation.Race2));

            if (relationId.Race1 == relationId.Race2)
            {
                throw new InvalidDataException("Invalid diplomacy relation value.");
            }

            result.Add(relationId, ScenarioRelationDecoder.DecodeRelation(relationId, relation.Relation));
        }

        return result.ToImmutable();
    }

    private static ImmutableSortedDictionary<WorldSubraceId, WorldSubrace> LoadSubraces(
        ScenarioDatabase scenarioDb,
        Db.ScenarioId scenarioId,
        IReadOnlyDictionary<Db.Id, WorldPlayerId> playerIds)
    {
        ImmutableSortedDictionary<WorldSubraceId, WorldSubrace>.Builder result =
            ImmutableSortedDictionary.CreateBuilder<WorldSubraceId, WorldSubrace>();

        foreach (Db.ScenarioSubrace subrace in GetScenarioObjects(scenarioDb.ScenarioSubracesByScenarioAndSubraceId, scenarioId).Values)
        {
            WorldSubraceId subraceId = new(subrace.SubraceId);
            WorldPlayerId playerId = playerIds[subrace.PlayerId];

            result.Add(subraceId, new WorldSubrace(subraceId, playerId, MapSubrace(subrace.Subrace), new WorldObjectVariant(subrace.Banner)));
        }

        return result.ToImmutable();
    }

    private static ImmutableSortedDictionary<WorldSiteId, WorldSite> LoadSites(
        ScenarioDatabase scenarioDb,
        Db.ScenarioId scenarioId)
    {
        ImmutableSortedDictionary<WorldSiteId, WorldSite>.Builder result =
            ImmutableSortedDictionary.CreateBuilder<WorldSiteId, WorldSite>();

        ILookup<Db.Id, Db.ScenarioMerchant> merchantsBySite = GetScenarioObjectsLookup(scenarioDb.ScenarioMerchantsByScenarioAndSiteId, scenarioId);
        ILookup<Db.Id, Db.ScenarioMercenary> mercenariesBySite = GetScenarioObjectsLookup(scenarioDb.ScenarioMercenariesByScenarioAndSiteId, scenarioId);
        ILookup<Db.Id, Db.ScenarioMage> magesBySite = GetScenarioObjectsLookup(scenarioDb.ScenarioMagesByScenarioAndSiteId, scenarioId);

        foreach (Db.ScenarioSite site in scenarioDb.ScenarioSitesByScenario[scenarioId])
        {
            WorldSiteId siteId = new(site.SiteId);
            WorldFootprint footprint = new(
                new WorldPosition(site.PositionX, site.PositionY),
                FortificationCalculator.SiteFootprint,
                FortificationCalculator.SiteFootprint);

            WorldSite worldSite = MapSite(site, siteId, footprint, merchantsBySite, mercenariesBySite, magesBySite);
            result.Add(siteId, worldSite);
        }

        return result.ToImmutable();
    }

    private static WorldSite MapSite(
        Db.ScenarioSite site,
        WorldSiteId siteId,
        WorldFootprint footprint,
        ILookup<Db.Id, Db.ScenarioMerchant> merchantsBySite,
        ILookup<Db.Id, Db.ScenarioMercenary> mercenariesBySite,
        ILookup<Db.Id, Db.ScenarioMage> magesBySite)
    {
        ScenarioTextId titleTextId = ScenarioTextId.MakeInternal(site.SiteId, Db.TextScenario.SiteTitleTextId, TextSequenceZero);
        ScenarioTextId greetingTextId = ScenarioTextId.MakeInternal(site.SiteId, Db.TextScenario.SiteDescriptionTextId, TextSequenceZero);

        if (site.QuantityItem is not null)
        {
            ImmutableArray<WorldMerchantItem> items =
            [
                .. merchantsBySite[site.SiteId].Select(merchant =>
                {
                    ItemDefinitionId itemId = new(merchant.ItemId);
                    return new WorldMerchantItem(itemId, merchant.ItemCount);
                })
            ];

            return new WorldMerchantSite(
                siteId,
                footprint,
                new WorldObjectVariant(site.ImageIso),
                titleTextId,
                greetingTextId,
                items,
                VisitorIds: [],
                site.AiPriority);
        }
        else if (site.QuantityUnit is not null)
        {
            ImmutableArray<WorldMercenaryUnit> units =
            [
                .. mercenariesBySite[site.SiteId].Select(mercenary =>
                {
                    UnitDefinitionId unitDefinitionId = new(mercenary.UnitId);
                    return new WorldMercenaryUnit(unitDefinitionId, mercenary.UnitLevel, mercenary.UnitUniq);
                })
            ];

            return new WorldMercenarySite(
                siteId,
                footprint,
                new WorldObjectVariant(site.ImageIso),
                titleTextId,
                greetingTextId,
                units,
                VisitorIds: [],
                site.AiPriority);
        }
        else if (site.QuantitySpell is not null)
        {
            ImmutableArray<WorldSpellDefinitionId> spells =
            [
                .. magesBySite[site.SiteId].Select(mage => new WorldSpellDefinitionId(mage.SpellId))
            ];

            return new WorldMageSite(
                siteId,
                footprint,
                new WorldObjectVariant(site.ImageIso),
                titleTextId,
                greetingTextId,
                spells,
                VisitorIds: [],
                site.AiPriority);
        }
        else
        {
            return new WorldTrainerSite(
                siteId,
                footprint,
                new WorldObjectVariant(site.ImageIso),
                titleTextId,
                greetingTextId,
                VisitorIds: [],
                site.AiPriority);
        }
    }

    private static WorldAiAttitudeType MapAiAttitude(Db.AiAttitudeType attitude)
    {
        return attitude switch
        {
            Db.AiAttitudeType.Small => WorldAiAttitudeType.Small,
            Db.AiAttitudeType.Medium => WorldAiAttitudeType.Medium,
            Db.AiAttitudeType.Large => WorldAiAttitudeType.Large,
            Db.AiAttitudeType.Humongous => WorldAiAttitudeType.Humongous,
            Db.AiAttitudeType.SmallNoResource => WorldAiAttitudeType.SmallNoExpansion,
            Db.AiAttitudeType.MediumNoResource => WorldAiAttitudeType.MediumNoExpansion,
            Db.AiAttitudeType.MediumNoResourceRuin => WorldAiAttitudeType.MediumNoExpansionNoRuins
        };
    }

    private static WorldCanonicalStackOrder MapWorldStackOrder(
        Db.OrderType order,
        Db.Id? orderTargetId,
        Db.Id subjectId,
        IReadOnlyDictionary<Db.Id, Db.ScenarioLocation> locations)
    {
        return order switch
        {
            Db.OrderType.Normal => WorldStackOrder.Normal.Instance,
            Db.OrderType.Stand => WorldStackOrder.Stand.Instance,
            Db.OrderType.Guard => WorldStackOrder.Guard.Instance,
            Db.OrderType.AttackStack => new WorldStackOrder.AttackStack(new WorldStackId(RequireTarget())),
            Db.OrderType.DefendStack => new WorldStackOrder.DefendStack(new WorldStackId(RequireTarget())),
            Db.OrderType.SecureCity => new WorldStackOrder.SecureCity(new WorldFortificationId(RequireTarget())),
            Db.OrderType.Roam => WorldStackOrder.Roam.Instance,
            Db.OrderType.MoveToLocation => MapMoveToLocationOrder(),
            Db.OrderType.DefendLocation => MapDefendLocationOrder(),
            Db.OrderType.Berserk => WorldStackOrder.Berserk.Instance,
            Db.OrderType.Assist => new WorldStackOrder.Assist(new WorldStackId(RequireTarget())),
            Db.OrderType.Explore => WorldStackOrder.Explore.Instance,
            Db.OrderType.Steal => WorldStackOrder.Steal.Instance,
            Db.OrderType.DefendCity => new WorldStackOrder.DefendCity(new WorldFortificationId(RequireTarget()))
        };

        WorldStackOrder.MoveToLocation MapMoveToLocationOrder()
        {
            Db.ScenarioLocation location = RequireTargetLocation();

            return new WorldStackOrder.MoveToLocation(
                new ScenarioLocationId(location.LocationId),
                new WorldPosition(location.PositionX, location.PositionY),
                location.Radius);
        }

        WorldStackOrder.DefendLocation MapDefendLocationOrder()
        {
            Db.ScenarioLocation location = RequireTargetLocation();

            return new WorldStackOrder.DefendLocation(
                new ScenarioLocationId(location.LocationId),
                new WorldPosition(location.PositionX, location.PositionY),
                location.Radius);
        }

        Db.ScenarioLocation RequireTargetLocation()
        {
            Db.Id targetId = RequireTarget();

            if (!locations.TryGetValue(targetId, out Db.ScenarioLocation? location))
            {
                throw new InvalidDataException($"Order of '{subjectId}' has an unexpected target.");
            }

            return location;
        }

        Db.Id RequireTarget()
        {
            if (orderTargetId is null)
            {
                throw new InvalidDataException($"Order of '{subjectId}' has no target.");
            }

            return orderTargetId.Value;
        }
    }

    private static WorldInventory LoadInventory(
        Db.Id containerId,
        ILookup<Db.Id, Db.ScenarioInventory> inventories,
        IReadOnlyDictionary<Db.Id, Db.ScenarioItem> items,
        IReadOnlyDictionary<Db.Id, Db.ScenarioTalismanCharges> talismanCharges,
        IGameDefinitionProvider definitions,
        GlobalVariables globals)
    {
        ImmutableArray<WorldItem>.Builder result = ImmutableArray.CreateBuilder<WorldItem>();

        foreach (Db.ScenarioInventory entry in inventories[containerId])
        {
            result.Add(LoadItem(entry.ItemId, items, talismanCharges, definitions, globals));
        }

        return new WorldInventory(result.ToImmutable());
    }

    private static LeaderEquipment LoadEquipment(
        Db.ScenarioStack stack,
        IReadOnlyDictionary<Db.Id, Db.ScenarioItem> items,
        IReadOnlyDictionary<Db.Id, Db.ScenarioTalismanCharges> talismanCharges,
        IGameDefinitionProvider definitions,
        GlobalVariables globals)
    {
        (LeaderEquipmentSlot Slot, Db.Id? ItemId)[] slots =
        [
            (LeaderEquipmentSlot.Banner, stack.BannerId),
            (LeaderEquipmentSlot.Tome, stack.TomeId),
            (LeaderEquipmentSlot.Battle1, stack.Battle1Id),
            (LeaderEquipmentSlot.Battle2, stack.Battle2Id),
            (LeaderEquipmentSlot.Artifact1, stack.Artifact1Id),
            (LeaderEquipmentSlot.Artifact2, stack.Artifact2Id),
            (LeaderEquipmentSlot.Boots, stack.BootsId)
        ];

        LeaderEquipment result = LeaderEquipment.Empty;

        foreach ((LeaderEquipmentSlot slot, Db.Id? itemId) in slots)
        {
            if (itemId is not null)
            {
                result = result.With(slot, LoadItem(itemId.Value, items, talismanCharges, definitions, globals));
            }
        }

        return result;
    }

    private static WorldItem LoadItem(
        Db.Id itemId,
        IReadOnlyDictionary<Db.Id, Db.ScenarioItem> items,
        IReadOnlyDictionary<Db.Id, Db.ScenarioTalismanCharges> talismanCharges,
        IGameDefinitionProvider definitions,
        GlobalVariables globals)
    {
        Db.ScenarioItem item = items[itemId];
        WorldItem result = WorldItemFactory.Create(
            new WorldItemId(item.ItemId),
            definitions.GetItem(new ItemDefinitionId(item.ItemType)),
            globals);

        if (result.Charges is not null && talismanCharges.TryGetValue(item.ItemId, out Db.ScenarioTalismanCharges? charges))
        {
            result = result with { Charges = charges.Charges };
        }

        return result;
    }

    private static WorldPosition GetStackPosition(
        Db.ScenarioStack stack,
        ImmutableSortedDictionary<WorldFortificationId, WorldFortification> fortifications)
    {
        if (stack.InsideId is { } insideId)
        {
            return fortifications[new WorldFortificationId(insideId)].Entrance;
        }
        else
        {
            return new WorldPosition(stack.PositionX, stack.PositionY);
        }
    }

    private static UnitGrid<WorldUnit> LoadUnitGrid(
        Db.ScenarioUnitGroup? group,
        IReadOnlyDictionary<Db.Id, Db.ScenarioUnit> scenarioUnits,
        ILookup<Db.Id, Db.ScenarioUnitModifier> scenarioUnitModifiers,
        ILookup<Db.Id, Db.TextScenario> unitNamesByUnit,
        IGameDefinitionProvider definitions)
    {
        UnitGrid<WorldUnit>.Builder result = new();

        if (group is not null)
        {
            Db.Id?[] unitIds = [group.Unit0Id, group.Unit1Id, group.Unit2Id, group.Unit3Id, group.Unit4Id, group.Unit5Id];
            int[] positions = [group.Pos0, group.Pos1, group.Pos2, group.Pos3, group.Pos4, group.Pos5];
            HashSet<int> placedUnits = [];

            for (int position = 0; position < GridPosition.Count; position++)
            {
                int unitIndex = positions[position];
                if (unitIndex >= 0 && unitIndex < GridPosition.Count)
                {
                    if (placedUnits.Add(unitIndex))
                    {
                        TryAddUnit(result, unitIds[unitIndex], position, scenarioUnits, scenarioUnitModifiers, unitNamesByUnit, definitions);
                    }
                }
            }
        }

        return result.Build();
    }

    private static bool TryAddUnit(
        UnitGrid<WorldUnit>.Builder gridBuilder,
        Db.Id? unitId,
        int position,
        IReadOnlyDictionary<Db.Id, Db.ScenarioUnit> scenarioUnits,
        ILookup<Db.Id, Db.ScenarioUnitModifier> scenarioUnitModifiers,
        ILookup<Db.Id, Db.TextScenario> unitNamesByUnit,
        IGameDefinitionProvider definitions)
    {
        if (unitId is null)
        {
            return false;
        }

        Db.ScenarioUnit scenarioUnit = scenarioUnits[unitId.Value];
        UnitDefinitionId definitionId = new(scenarioUnit.TypeId);
        UnitDefinition definition = definitions.GetUnit(definitionId);
        WorldUnit worldUnit = new(
            new WorldUnitId(scenarioUnit.UnitId),
            definitionId,
            scenarioUnit.Hp,
            scenarioUnit.Xp,
            scenarioUnit.Level,
            LoadUnitModifiers(unitId.Value, scenarioUnitModifiers, definitions),
            LoadUnitName(unitNamesByUnit, unitId.Value),
            IsTypeLocked: false);

        if (position < 0 || position > 5)
        {
            return false;
        }

        GridPosition gridPosition = new(position % 2, position / 2);
        gridBuilder.Place(worldUnit, gridPosition, definition.IsLarge);

        return true;
    }

    private static string? LoadUnitName(ILookup<Db.Id, Db.TextScenario> unitNamesByUnit, Db.Id unitId)
    {
        foreach (Db.TextScenario text in unitNamesByUnit[unitId])
        {
            if (text.Text is { Length: > 0 } name)
            {
                return name;
            }
        }

        return null;
    }

    private static ImmutableArray<UnitModifier> LoadUnitModifiers(
        Db.Id unitId,
        ILookup<Db.Id, Db.ScenarioUnitModifier> scenarioUnitModifiers,
        IGameDefinitionProvider definitions)
    {
        ImmutableArray<UnitModifier>.Builder result = ImmutableArray.CreateBuilder<UnitModifier>();

        foreach (Db.ScenarioUnitModifier modifier in scenarioUnitModifiers[unitId])
        {
            UnitModifierDefinitionId modifierId = new(modifier.ModifierId);
            definitions.GetUnitModifier(modifierId);

            result.Add(UnitModifier.FromScenario(modifierId));
        }

        return result.ToImmutable();
    }

    private static WorldDifficulty MapDifficulty(Db.DifficultyType difficulty)
    {
        return difficulty switch
        {
            Db.DifficultyType.Easy => WorldDifficulty.Easy,
            Db.DifficultyType.Average => WorldDifficulty.Average,
            Db.DifficultyType.Hard => WorldDifficulty.Hard,
            Db.DifficultyType.VeryHard => WorldDifficulty.VeryHard
        };
    }

    private static WorldRace MapRace(Db.RaceType race)
    {
        return race switch
        {
            Db.RaceType.Human => WorldRace.Human,
            Db.RaceType.Undead => WorldRace.Undead,
            Db.RaceType.Heretic => WorldRace.Heretic,
            Db.RaceType.Dwarf => WorldRace.Dwarf,
            Db.RaceType.Neutral => WorldRace.Neutral,
            Db.RaceType.Elf => WorldRace.Elf
        };
    }

    private static WorldSubraceType MapSubrace(int subrace)
    {
        return (Db.SubraceType)subrace switch
        {
            Db.SubraceType.Custom => WorldSubraceType.Custom,
            Db.SubraceType.Human => WorldSubraceType.Human,
            Db.SubraceType.Undead => WorldSubraceType.Undead,
            Db.SubraceType.Heretic => WorldSubraceType.Heretic,
            Db.SubraceType.Dwarf => WorldSubraceType.Dwarf,
            Db.SubraceType.Neutral => WorldSubraceType.Neutral,
            Db.SubraceType.NeutralHuman => WorldSubraceType.NeutralHuman,
            Db.SubraceType.NeutralElf => WorldSubraceType.NeutralElf,
            Db.SubraceType.NeutralGreenSkin => WorldSubraceType.NeutralGreenSkin,
            Db.SubraceType.NeutralDragon => WorldSubraceType.NeutralDragon,
            Db.SubraceType.NeutralMarsh => WorldSubraceType.NeutralMarsh,
            Db.SubraceType.NeutralWater => WorldSubraceType.NeutralWater,
            Db.SubraceType.NeutralBarbarian => WorldSubraceType.NeutralBarbarian,
            Db.SubraceType.NeutralWolf => WorldSubraceType.NeutralWolf,
            Db.SubraceType.Elf => WorldSubraceType.Elf
        };
    }

    private static WorldResourceSet MapResourceSet(Db.ResourceSet resources)
    {
        return new WorldResourceSet(
            resources.Gold,
            resources.DeathMana,
            resources.RuneMana,
            resources.LifeMana,
            resources.InfernalMana,
            resources.GroveMana);
    }

    private static IReadOnlyDictionary<TKey, TValue> GetScenarioObjects<TKey, TValue>(
        IReadOnlyDictionary<Db.ScenarioId, IReadOnlyDictionary<TKey, TValue>> objectsByScenario,
        Db.ScenarioId scenarioId)
        where TKey : notnull
    {
        if (objectsByScenario.TryGetValue(scenarioId, out IReadOnlyDictionary<TKey, TValue>? result))
        {
            return result;
        }
        else
        {
            return ImmutableDictionary<TKey, TValue>.Empty;
        }
    }

    private static ILookup<TKey, TValue> GetScenarioObjectsLookup<TKey, TValue>(
        IReadOnlyDictionary<Db.ScenarioId, ILookup<TKey, TValue>> objectsByScenario,
        Db.ScenarioId scenarioId)
        where TKey : notnull
    {
        if (objectsByScenario.TryGetValue(scenarioId, out ILookup<TKey, TValue>? result))
        {
            return result;
        }
        else
        {
            return Enumerable.Empty<TValue>().ToLookup(_ => default(TKey)!);
        }
    }

    private static ILookup<Db.Id, Db.TextScenario> GetScenarioTextsLookup(
        ScenarioDatabase scenarioDb,
        Db.ScenarioId scenarioId,
        string textId)
    {
        return scenarioDb.TextScenariosByScenario[scenarioId]
            .Where(text => text.IdString == textId)
            .ToLookup(text => text.IdObject);
    }

    private static bool IsEmptyText(string text)
    {
        foreach (char ch in text)
        {
            if (char.IsWhiteSpace(ch) || char.IsControl(ch))
            {
                continue;
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    private static T? ConvertOptionalId<T>(Db.Id? id, Func<Db.Id, T> createFunc) where T : struct
    {
        if (id is null)
        {
            return null;
        }
        else
        {
            return createFunc(id.Value);
        }
    }
}
