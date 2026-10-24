using System.Collections.Immutable;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed record WorldTileInfoViewModel(
    string Title,
    IReadOnlyList<TextSpans> Lines) : WorldPositionInfoViewModel
{
    public WorldUnitGroupInfoGridViewModel? Garrison { get; init; }
    public WorldTileInfoItemViewModel? Item { get; init; }
    public ResourceSetViewModel? Reward { get; init; }
    public IReadOnlyList<TextSpans> Tombs { get; init; } = [];

    public bool HasDetails => Lines.Count > 0 || Tombs.Count > 0 || Garrison is not null || Item is not null || Reward is not null;

    public static WorldTileInfoViewModel? TryCreate(
        WorldState world,
        WorldPlayerId viewPlayerId,
        WorldPosition position,
        WorldVisibility visibility,
        IGameDefinitionProvider definitions,
        IGameTextProvider texts,
        IUnitStatsProvider stats)
    {
        WorldPlan plan = WorldPlan.For(world);

        WorldFortification? fortification = plan.GetFortificationAt(position);

        if (fortification is WorldRuin ruin)
        {
            if (visibility.IsAnyRevealed(ruin.Footprint))
            {
                return CreateRuinInfo(ruin, world, viewPlayerId, definitions, texts, stats);
            }
            else
            {
                return null;
            }
        }

        WorldSite? site = plan.GetSiteAt(position);

        if (site is not null)
        {
            if (visibility.IsAnyRevealed(site.Footprint))
            {
                return CreateSiteInfo(site, texts);
            }
            else
            {
                return null;
            }
        }

        WorldLandmark? landmark = plan.GetLandmarkAt(position);

        if (landmark is not null)
        {
            if (visibility.IsAnyRevealed(landmark.Footprint))
            {
                WorldLandmarkDefinition definition = definitions.GetLandmark(landmark.DefinitionId);

                return new WorldTileInfoViewModel(texts.GetGlobalText(definition.NameTextId).Text, []);
            }
            else
            {
                return null;
            }
        }

        if (!visibility.IsRevealed(position))
        {
            return null;
        }

        WorldResourceNode? resourceNode = plan.GetResourceNodeAt(position);

        if (resourceNode is not null)
        {
            return CreateResourceNodeInfo(resourceNode, plan, world, definitions, texts);
        }

        WorldRod? rod = plan.GetRodAt(position);

        if (rod is not null)
        {
            return new WorldTileInfoViewModel(
                Tr("Rod"),
                [FormatOwnerLine(rod.OwnerId, world, definitions, texts)]);
        }

        WorldBag? bag = plan.GetBagAt(position);

        if (bag is not null)
        {
            return CreateBagInfo();
        }

        ImmutableArray<WorldTomb> tombs = plan.GetTombsAt(position);

        if (!tombs.IsEmpty)
        {
            return CreateTombInfo(tombs, world, definitions, texts);
        }

        return null;
    }

    private static WorldTileInfoViewModel CreateResourceNodeInfo(
        WorldResourceNode resourceNode,
        WorldPlan plan,
        WorldState world,
        IGameDefinitionProvider definitions,
        IGameTextProvider texts)
    {
        string title = FormatResourceNodeTitle(resourceNode.ResourceType);
        WorldPlayerId? ownerId = plan.GetLandOwner(resourceNode.Position);

        if (ownerId is not null)
        {
            return new WorldTileInfoViewModel(title, [FormatOwnerLine(ownerId.Value, world, definitions, texts)]);
        }
        else
        {
            return new WorldTileInfoViewModel(title, []);
        }
    }

    private static WorldTileInfoViewModel CreateRuinInfo(
        WorldRuin ruin,
        WorldState world,
        WorldPlayerId viewPlayerId,
        IGameDefinitionProvider definitions,
        IGameTextProvider texts,
        IUnitStatsProvider stats)
    {
        string name = texts.GetScenarioText(ruin.NameTextId);

        if (ruin.LooterId is not null)
        {
            return new WorldTileInfoViewModel(name, [TrSpans("Plundered")]);
        }
        else if (!WorldVisibility.KnowsRuinContents(world, viewPlayerId, ruin.Id))
        {
            return new WorldTileInfoViewModel(name, [TrSpans("Unexplored")]);
        }
        else
        {
            return new WorldTileInfoViewModel(name, [])
            {
                Garrison = WorldUnitGroupInfoGridViewModel.Create(ruin.Garrison, definitions, texts, stats),
                Item = CreateItem(ruin.Item, definitions, texts),
                Reward = CreateReward(ruin.Resources)
            };
        }
    }

    private static WorldTileInfoItemViewModel? CreateItem(
        ItemDefinitionId? itemId,
        IGameDefinitionProvider definitions,
        IGameTextProvider texts)
    {
        if (itemId is not null)
        {
            string name = ItemFormatter.FormatName(definitions, texts, itemId.Value, charges: null);

            return new WorldTileInfoItemViewModel(name, ItemIconResolver.Resolve(definitions, itemId.Value));
        }
        else
        {
            return null;
        }
    }

    private static ResourceSetViewModel? CreateReward(WorldResourceSet resources)
    {
        if (resources != WorldResourceSet.Empty)
        {
            return new ResourceSetViewModel(resources, ResourceSetProjection.All);
        }
        else
        {
            return null;
        }
    }

    private static WorldTileInfoViewModel CreateSiteInfo(WorldSite site, IGameTextProvider texts)
    {
        string name = texts.GetScenarioText(site.TitleTextId);
        string greeting = texts.GetScenarioText(site.GreetingTextId);
        string title = Tr("{0} – {1}", name, FormatSiteType(site));

        if (!string.IsNullOrWhiteSpace(greeting))
        {
            return new WorldTileInfoViewModel(title, [TextSpans.FromText(greeting)]);
        }
        else
        {
            return new WorldTileInfoViewModel(title, []);
        }
    }

    private static string FormatSiteType(WorldSite site)
    {
        return site switch
        {
            WorldMerchantSite => Tr("Merchant"),
            WorldMageSite => Tr("Mage tower"),
            WorldMercenarySite => Tr("Mercenary camp"),
            WorldTrainerSite => Tr("Trainer"),
            _ => throw new ArgumentOutOfRangeException(nameof(site), site, null)
        };
    }

    private static WorldTileInfoViewModel CreateBagInfo()
    {
        return new WorldTileInfoViewModel(Tr("Bag"), [TrSpans("Unknown treasure")]);
    }

    private static WorldTileInfoViewModel CreateTombInfo(
        ImmutableArray<WorldTomb> tombs,
        WorldState world,
        IGameDefinitionProvider definitions,
        IGameTextProvider texts)
    {
        List<TextSpans> lines = [];

        foreach (WorldTomb tomb in tombs)
        {
            lines.Add(CreateTombLine(tomb, world, definitions, texts));
        }

        return new WorldTileInfoViewModel(Tr("Here lies:"), []) { Tombs = lines };
    }

    private static TextSpans CreateTombLine(
        WorldTomb tomb,
        WorldState world,
        IGameDefinitionProvider definitions,
        IGameTextProvider texts)
    {
        string name = tomb.LeaderName ?? FormatUnitTypeName(tomb.LeaderDefinitionId, definitions, texts);

        if (tomb.DestroyerPlayerId is not null)
        {
            WorldPlayer destroyer = world.Players[tomb.DestroyerPlayerId.Value];

            return TrSpans("{0:*}, defeated by {1}", name, FormatDestroyer(tomb, destroyer, definitions, texts));
        }
        else
        {
            return TrSpans("{0:*}", name);
        }
    }

    private static string FormatDestroyer(
        WorldTomb tomb,
        WorldPlayer destroyer,
        IGameDefinitionProvider definitions,
        IGameTextProvider texts)
    {
        if (tomb.DestroyerLeaderDefinitionId is not null)
        {
            return tomb.DestroyerLeaderName ?? FormatUnitTypeName(tomb.DestroyerLeaderDefinitionId.Value, definitions, texts);
        }
        else
        {
            return WorldPlayerFormatter.FormatRaceName(definitions, texts, destroyer);
        }
    }

    private static string FormatUnitTypeName(UnitDefinitionId definitionId, IGameDefinitionProvider definitions, IGameTextProvider texts)
    {
        UnitDefinition definition = definitions.GetUnit(definitionId);

        return texts.GetGlobalText(definition.NameTextId).Text;
    }

    private static string FormatResourceNodeTitle(WorldResourceType resourceType)
    {
        return resourceType switch
        {
            WorldResourceType.Gold => Tr("Gold mine"),
            WorldResourceType.LifeMana => Tr("Life mana crystal"),
            WorldResourceType.DeathMana => Tr("Death mana crystal"),
            WorldResourceType.RuneMana => Tr("Rune mana crystal"),
            WorldResourceType.InfernalMana => Tr("Infernal mana crystal"),
            WorldResourceType.GroveMana => Tr("Grove mana crystal")
        };
    }

    private static TextSpans FormatOwnerLine(
        WorldPlayerId ownerId,
        WorldState world,
        IGameDefinitionProvider definitions,
        IGameTextProvider texts)
    {
        return TrSpans("Owner: {0:*}", FormatRaceName(world.Players[ownerId].Race, definitions, texts));
    }

    private static string FormatRaceName(WorldRace race, IGameDefinitionProvider definitions, IGameTextProvider texts)
    {
        WorldRaceDefinition definition = definitions.GetRace(race);

        return texts.GetGlobalText(definition.NameTextId).Text;
    }
}
