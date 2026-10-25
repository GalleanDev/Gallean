using System.Collections.Immutable;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    WorldState IWorldEventVisitor<WorldState, WorldState>.Visit(WorldStackBattleEndedEvent ev, WorldState state)
    {
        ImmutableSortedDictionary<WorldStackId, WorldStack> updatedStacks = state.Stacks;

        if (ev.SurvivingAttackerUnits.HasAliveUnits())
        {
            WorldStack attackerStack = updatedStacks[ev.AttackerStackId];

            updatedStacks = updatedStacks.SetItem(ev.AttackerStackId, attackerStack with
            {
                Units = ev.SurvivingAttackerUnits
            });
        }

        if (ev.SurvivingDefenderUnits.HasAliveUnits())
        {
            WorldStack defenderStack = updatedStacks[ev.DefenderStackId];
            updatedStacks = updatedStacks.SetItem(ev.DefenderStackId, defenderStack with
            {
                Units = ev.SurvivingDefenderUnits
            });
        }

        WorldStackId winnerStackId;

        if (ev.WinningSide == BattleSide.Attacker)
        {
            winnerStackId = ev.AttackerStackId;
        }
        else
        {
            winnerStackId = ev.DefenderStackId;
        }

        WorldStack winnerStack = updatedStacks[winnerStackId];
        updatedStacks = updatedStacks.SetItem(winnerStackId, winnerStack with
        {
            BattlesWon = winnerStack.BattlesWon + 1
        });

        WorldState updatedState = state with
        {
            Stacks = updatedStacks
        };

        return FortificationCalculator.UpdateUnitModifiers(updatedState, Context.Definitions);
    }
}
