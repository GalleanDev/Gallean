using System.Collections.Immutable;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldFortificationBattleEndedEvent ev, WorldState state)
    {
        WorldFortification fortification = state.Fortifications[ev.DefenderFortificationId];
        WorldState resolvedState = fortification switch
        {
            WorldCapital capital => EndCityBattle(state, ev, capital),
            WorldCity city => EndCityBattle(state, ev, city),
            WorldRuin ruin => EndRuinBattle(state, ev, ruin)
        };

        if (ev.WinningSide == BattleSide.Attacker)
        {
            WorldStack attackerStack = resolvedState.Stacks[ev.AttackerStackId];

            resolvedState = resolvedState with
            {
                Stacks = resolvedState.Stacks.SetItem(ev.AttackerStackId, attackerStack with
                {
                    BattlesWon = attackerStack.BattlesWon + 1
                })
            };
        }

        return FortificationCalculator.UpdateUnitModifiers(resolvedState, Context.Definitions);
    }

    private static WorldState EndCityBattle(WorldState state, WorldFortificationBattleEndedEvent ev, WorldFortification fortification)
    {
        ImmutableSortedDictionary<WorldStackId, WorldStack> updatedStacks = state.Stacks;

        if (ev.SurvivingAttackerUnits.HasAliveUnits())
        {
            WorldStack attackerStack = state.Stacks[ev.AttackerStackId];

            updatedStacks = updatedStacks.SetItem(ev.AttackerStackId, attackerStack with
            {
                Units = ev.SurvivingAttackerUnits
            });
        }

        WorldFortification updatedFortification = fortification with { Garrison = ev.SurvivingDefenderUnits };

        return state with
        {
            Stacks = updatedStacks,
            Fortifications = state.Fortifications.SetItem(ev.DefenderFortificationId, updatedFortification)
        };
    }

    private static WorldState EndRuinBattle(WorldState state, WorldFortificationBattleEndedEvent ev, WorldRuin ruin)
    {
        ImmutableSortedDictionary<WorldStackId, WorldStack> updatedStacks = state.Stacks;

        if (ev.SurvivingAttackerUnits.HasAliveUnits())
        {
            WorldStack attackerStack = state.Stacks[ev.AttackerStackId];

            updatedStacks = updatedStacks.SetItem(ev.AttackerStackId, attackerStack with
            {
                Units = ev.SurvivingAttackerUnits
            });
        }

        UnitGrid<WorldUnit> updatedGarrison;

        if (ev.SurvivingDefenderUnits.HasAliveUnits())
        {
            updatedGarrison = ev.SurvivingDefenderUnits;
        }
        else
        {
            updatedGarrison = UnitGrid<WorldUnit>.Empty;
        }

        return state with
        {
            Stacks = updatedStacks,
            Fortifications = state.Fortifications.SetItem(ev.DefenderFortificationId, ruin with { Garrison = updatedGarrison })
        };
    }
}
