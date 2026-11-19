using System.Collections.Immutable;
using Avalonia.Media;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class WorldRenderComposer
{
    private static class MinimapBuilder
    {
        public static MinimapState Build(
            WorldMap map,
            ImmutableSortedDictionary<WorldFortificationId, WorldFortification> fortifications,
            ImmutableSortedDictionary<WorldSiteId, WorldSite> sites,
            ImmutableSortedDictionary<WorldResourceNodeId, WorldResourceNode> resourceNodes,
            ImmutableSortedDictionary<WorldStackId, WorldStack> stacks,
            PlayerRaces playerRaces,
            WorldVisibility visibility,
            WorldPlayerId viewPlayerId,
            bool fogEnabled)
        {
            List<WorldPosition> hiddenPositions = [];

            if (fogEnabled)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    for (int x = 0; x < map.Width; x++)
                    {
                        WorldPosition position = new(x, y);

                        if (!visibility.IsRevealed(position))
                        {
                            hiddenPositions.Add(position);
                        }
                    }
                }
            }

            List<MinimapState.Marker> markers =
            [
                .. EnumerateSiteMarkers(sites, visibility),
                .. EnumerateRuinMarkers(fortifications, visibility),
                .. EnumerateResourceMarkers(resourceNodes, visibility),
                .. EnumerateCityMarkers(fortifications, playerRaces, visibility),
                .. EnumerateStackMarkers(stacks, playerRaces, visibility, viewPlayerId)
            ];

            return new MinimapState(map.Width, map.Height, hiddenPositions, markers);
        }

        private static IEnumerable<MinimapState.Marker> EnumerateSiteMarkers(
            ImmutableSortedDictionary<WorldSiteId, WorldSite> sites,
            WorldVisibility visibility)
        {
            foreach (WorldSite site in sites.Values)
            {
                if (visibility.IsRevealed(site.Entrance))
                {
                    yield return new MinimapState.Marker(site.Entrance, Theme.Minimap.SiteColor, MinimapState.MarkerType.Site);
                }
            }
        }

        private static IEnumerable<MinimapState.Marker> EnumerateRuinMarkers(
            ImmutableSortedDictionary<WorldFortificationId, WorldFortification> fortifications,
            WorldVisibility visibility)
        {
            foreach (WorldFortification fortification in fortifications.Values)
            {
                if (fortification is WorldRuin ruin && visibility.IsRevealed(ruin.Entrance))
                {
                    yield return new MinimapState.Marker(ruin.Entrance, GetRuinColor(ruin), MinimapState.MarkerType.Ruin);
                }
            }
        }

        private static IEnumerable<MinimapState.Marker> EnumerateResourceMarkers(
            ImmutableSortedDictionary<WorldResourceNodeId, WorldResourceNode> resourceNodes,
            WorldVisibility visibility)
        {
            foreach (WorldResourceNode node in resourceNodes.Values)
            {
                if (visibility.IsRevealed(node.Position))
                {
                    yield return new MinimapState.Marker(node.Position, GetResourceColor(node.ResourceType), MinimapState.MarkerType.Resource);
                }
            }
        }

        private static IEnumerable<MinimapState.Marker> EnumerateCityMarkers(
            ImmutableSortedDictionary<WorldFortificationId, WorldFortification> fortifications,
            PlayerRaces playerRaces,
            WorldVisibility visibility)
        {
            foreach (WorldFortification fortification in fortifications.Values)
            {
                if (visibility.IsRevealed(fortification.Entrance))
                {
                    switch (fortification)
                    {
                        case WorldCapital capital:
                            yield return new MinimapState.Marker(
                                capital.Entrance, GetOwnerColor(playerRaces[capital.OwnerId]), MinimapState.MarkerType.Capital);
                            break;

                        case WorldCity city:
                            yield return new MinimapState.Marker(
                                city.Entrance, GetOwnerColor(playerRaces[city.OwnerId]), MinimapState.MarkerType.City);
                            break;
                    }
                }
            }
        }

        private static IEnumerable<MinimapState.Marker> EnumerateStackMarkers(
            ImmutableSortedDictionary<WorldStackId, WorldStack> stacks,
            PlayerRaces playerRaces,
            WorldVisibility visibility,
            WorldPlayerId viewPlayerId)
        {
            foreach (WorldStack stack in stacks.Values)
            {
                if (visibility.IsStackVisible(stack, viewPlayerId))
                {
                    yield return new MinimapState.Marker(stack.Position, GetOwnerColor(playerRaces[stack.OwnerId]), MinimapState.MarkerType.Stack);
                }
            }
        }

        private static Color GetRuinColor(WorldRuin ruin)
        {
            if (ruin.LooterId is null)
            {
                return Theme.Minimap.RuinColor;
            }
            else
            {
                return Theme.Minimap.LootedRuinColor;
            }
        }

        private static Color GetResourceColor(WorldResourceType type)
        {
            return type switch
            {
                WorldResourceType.Gold => Theme.Minimap.GoldColor,
                WorldResourceType.DeathMana => Theme.Minimap.DeathManaColor,
                WorldResourceType.RuneMana => Theme.Minimap.RuneManaColor,
                WorldResourceType.LifeMana => Theme.Minimap.LifeManaColor,
                WorldResourceType.InfernalMana => Theme.Minimap.InfernalManaColor,
                WorldResourceType.GroveMana => Theme.Minimap.GroveManaColor
            };
        }

        private static Color GetOwnerColor(WorldRace race)
        {
            return race switch
            {
                WorldRace.Human => Theme.World.Race.HumanColor,
                WorldRace.Undead => Theme.World.Race.UndeadColor,
                WorldRace.Heretic => Theme.World.Race.HereticColor,
                WorldRace.Dwarf => Theme.World.Race.DwarfColor,
                WorldRace.Neutral => Theme.World.Race.NeutralColor,
                WorldRace.Elf => Theme.World.Race.ElfColor
            };
        }
    }
}
