using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class FortificationBattleEndedResolver : IWorldFortificationVisitor<FortificationBattleEndedResolver.Context, WorldState>, IStaticInstance<FortificationBattleEndedResolver>
    {
        public sealed record Context(WorldState State, WorldFortificationBattleEndedEvent Event);

        public WorldState Visit(WorldCapital capital, Context context)
        {
            return HandleCity(context.State, context.Event, capital);
        }

        public WorldState Visit(WorldCity city, Context context)
        {
            return HandleCity(context.State, context.Event, city);
        }

        public WorldState Visit(WorldRuin ruin, Context context)
        {
            return HandleRuin(context.State, context.Event, ruin);
        }

        private static WorldState HandleCity(WorldState state, WorldFortificationBattleEndedEvent ev, WorldFortification fortification)
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

        private static WorldState HandleRuin(WorldState state, WorldFortificationBattleEndedEvent ev, WorldRuin ruin)
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

    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldFortificationBattleEndedEvent ev, WorldState state)
    {
        WorldFortification fortification = state.Fortifications[ev.DefenderFortificationId];
        WorldState resolvedState = fortification.Accept(
            FortificationBattleEndedResolver.Instance,
            new FortificationBattleEndedResolver.Context(state, ev));

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
}
