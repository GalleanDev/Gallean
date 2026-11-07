using System.Collections.Immutable;

namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class TurnStartedGrantAiAutoExperienceRule(IEngineContext context) : WorldEventRule(context)
    {
        public override EventRuleResult<WorldEvent> Visit(WorldTurnStartedEvent ev, WorldState state)
        {
            WorldPlayer player = state.Players[ev.PlayerId];

            if (!player.IsAi)
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            if (player.Race == WorldRace.Neutral &&
                Context.Scenario.Definition.SuggestedLevel > Context.Globals.AiAutoExperienceNeutralMaxSuggestedLevel)
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            WorldAiAttitudeDefinition attitude = Context.Definitions.GetAiAttitude(player.AiAttitude);
            int stackPercent = GetAutoExperiencePercent(attitude, state.Difficulty);
            int garrisonPercent = stackPercent * Context.Globals.AiAutoExperienceGarrisonMultiplier;
            List<WorldEvent> events = [];

            foreach (WorldStack stack in state.Stacks.Values)
            {
                if (stack.OwnerId == ev.PlayerId && IsUnexplored(stack.Position, state))
                {
                    events.AddRange(CreateExperienceEvents(new WorldUnitGroup.Stack(stack.Id), stack.Units, stackPercent));
                }
            }

            foreach (WorldFortification fortification in state.Fortifications.Values)
            {
                if (fortification.Owner == ev.PlayerId && IsUnexplored(fortification.Entrance, state))
                {
                    events.AddRange(CreateExperienceEvents(new WorldUnitGroup.Fortification(fortification.Id), fortification.Garrison, garrisonPercent));
                }
            }

            return EventRuleResult<WorldEvent>.Append([.. events]);
        }

        private static int GetAutoExperiencePercent(WorldAiAttitudeDefinition attitude, WorldDifficulty difficulty)
        {
            return difficulty switch
            {
                WorldDifficulty.Easy => attitude.EasyAutoExperiencePercent,
                WorldDifficulty.Average => attitude.AverageAutoExperiencePercent,
                WorldDifficulty.Hard => attitude.HardAutoExperiencePercent,
                WorldDifficulty.VeryHard => attitude.VeryHardAutoExperiencePercent
            };
        }

        private static bool IsUnexplored(WorldPosition position, WorldState state)
        {
            ImmutableArray<WorldPosition> tiles = [position, .. position.GetAdjacentPositions()];

            foreach (WorldPlayer player in state.Players.Values)
            {
                if (!player.IsAi)
                {
                    foreach (WorldPosition tile in tiles)
                    {
                        if (state.Map.IsValidPosition(tile) && player.FogOfWar.Get(tile.X, tile.Y))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private List<WorldEvent> CreateExperienceEvents(WorldUnitGroup group, UnitGrid<WorldUnit> units, int percent)
        {
            List<WorldEvent> events = [];

            foreach ((WorldUnit unit, GridPosition _) in units.GetUnits())
            {
                int experience = GetExperienceGained(unit, percent);

                if (experience > 0)
                {
                    events.Add(new WorldUnitExperienceGainedEvent(group, unit.Id, experience));
                }
            }

            return events;
        }

        private int GetExperienceGained(WorldUnit unit, int percent)
        {
            UnitExperience experience = Context.UnitStats.GetWorldUnitExperience(unit);

            if (unit.IsAlive && !experience.IsMaximumLevel)
            {
                int gained = experience.NextLevel * percent / 100;

                return Math.Max(0, Math.Min(gained, experience.NextLevel - 1 - experience.Current));
            }
            else
            {
                return 0;
            }
        }
    }
}
