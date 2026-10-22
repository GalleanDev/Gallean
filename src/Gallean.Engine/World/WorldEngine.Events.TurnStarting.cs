using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldTurnStartingEvent ev, WorldState state)
    {
        if (state.TurnInfo.TurnNumber > 1)
        {
            WorldMap updatedMap = ExpansionCalculator.ExpandPlayerTerrain(state, ev.PlayerId, Context);
            state = state with { Map = updatedMap };
        }

        WorldPlayer player = state.Players[ev.PlayerId];
        WorldPlan plan = WorldPlan.For(state);

        ImmutableSortedDictionary<WorldStackId, WorldStack> updatedStacks = state.Stacks;

        foreach (WorldStack stack in state.Stacks.Values)
        {
            if (stack.OwnerId == ev.PlayerId)
            {
                WorldFortification? fortification = plan.GetFortificationAt(stack.Position);
                UnitGrid<WorldUnit> updatedUnits;

                if (fortification is not null &&
                    FortificationCalculator.GetOwner(fortification) == ev.PlayerId &&
                    FortificationCalculator.IsRioting(fortification))
                {
                    updatedUnits = stack.Units;
                }
                else
                {
                    updatedUnits = WorldHealCalculator.RegenerateStack(state, stack, Context);
                }

                updatedStacks = updatedStacks.SetItem(stack.Id, stack with { Units = updatedUnits });
            }
        }

        ImmutableSortedDictionary<WorldFortificationId, WorldFortification> updatedFortifications = state.Fortifications;

        foreach (WorldFortification fortification in state.Fortifications.Values)
        {
            if (FortificationCalculator.GetOwner(fortification) == ev.PlayerId)
            {
                UnitGrid<WorldUnit> updatedGarrison;

                if (FortificationCalculator.IsRioting(fortification))
                {
                    updatedGarrison = fortification.Garrison;
                }
                else
                {
                    updatedGarrison = WorldHealCalculator.RegenerateGarrison(state, fortification, Context);
                }

                updatedFortifications = updatedFortifications.SetItem(fortification.Id, fortification with { Garrison = updatedGarrison });
            }
        }

        state = state with { Stacks = updatedStacks, Fortifications = updatedFortifications };

        ImmutableBitmap updatedFog = FogOfWarCalculator.Update(state, ev.PlayerId, Context);

        ImmutableSortedDictionary<WorldPlayerId, WorldPlayer> updatedPlayers = state.Players.SetItem(ev.PlayerId, player with
        {
            FogOfWar = updatedFog,
            Buildings = EnsureLordBuilding(player),
            TurnActions = new WorldPlayerTurnActions()
        });

        return state with
        {
            Players = UpdateDiplomacyProposals(updatedPlayers, ev.PlayerId),
            TurnInfo = state.TurnInfo with { CurrentPlayerId = ev.PlayerId }
        };
    }

    private static ImmutableSortedDictionary<WorldPlayerId, WorldPlayer> UpdateDiplomacyProposals(
        ImmutableSortedDictionary<WorldPlayerId, WorldPlayer> players,
        WorldPlayerId playerId)
    {
        ImmutableSortedDictionary<WorldPlayerId, WorldPlayer> result = players;

        foreach (WorldPlayer targetPlayer in players.Values)
        {
            ImmutableArray<WorldDiplomacyProposal> remaining =
                targetPlayer.IncomingDiplomacyProposals.RemoveAll(proposal => proposal.PlayerId == playerId);

            if (remaining.Length != targetPlayer.IncomingDiplomacyProposals.Length)
            {
                result = result.SetItem(targetPlayer.Id, targetPlayer with { IncomingDiplomacyProposals = remaining });
            }
        }

        return result;
    }

    private WorldPlayerBuildings EnsureLordBuilding(WorldPlayer player)
    {
        LordDefinition lord = Context.Definitions.GetLord(player.LordId);

        WorldPlayerBuildingCategory? category = lord.Category switch
        {
            LordCategory.Warrior => null,
            LordCategory.Mage => WorldPlayerBuildingCategory.Spell,
            LordCategory.Guildmaster => WorldPlayerBuildingCategory.Guild
        };

        if (category is null)
        {
            return player.Buildings;
        }

        WorldPlayerBuildingDefinitionId? buildingId =
            PlayerBuildingsCalculator.FindAvailable(player.Buildings, category.Value, Context.Definitions);

        if (buildingId is null)
        {
            return player.Buildings;
        }

        return player.Buildings with { Acquired = player.Buildings.Acquired.Add(buildingId.Value) };
    }
}
