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
                    UnitGrid<WorldUnit> trainedUnits = TrainUnits(stack.Units, stackPercent, player.Buildings);

                    events.AddRange(CreateTrainingEvents(new WorldUnitGroup.Stack(stack.Id), stack.Units, trainedUnits));
                    events.AddRange(LeaderUpgradeCalculator.CreateUpgradeEvents(stack, trainedUnits, Context.Definitions, Context.Globals));
                }
            }

            foreach (WorldFortification fortification in state.Fortifications.Values)
            {
                if (fortification.Owner == ev.PlayerId && IsUnexplored(fortification.Entrance, state))
                {
                    UnitGrid<WorldUnit> trainedUnits = TrainUnits(fortification.Garrison, garrisonPercent, player.Buildings);

                    events.AddRange(CreateTrainingEvents(new WorldUnitGroup.Fortification(fortification.Id), fortification.Garrison, trainedUnits));
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

        private static List<WorldEvent> CreateTrainingEvents(
            WorldUnitGroup group,
            UnitGrid<WorldUnit> units,
            UnitGrid<WorldUnit> trainedUnits)
        {
            List<WorldEvent> events = [];

            foreach ((WorldUnit unit, GridPosition _) in units.GetUnits())
            {
                WorldUnit trainedUnit = trainedUnits.GetUnit(unit.Id);

                if (trainedUnit.DefinitionId != unit.DefinitionId || trainedUnit.Level != unit.Level)
                {
                    events.Add(new WorldUnitUpgradedEvent(group, unit.Id, trainedUnit.DefinitionId, trainedUnit.Level, trainedUnit.Hitpoints));
                }
                else if (trainedUnit.Experience != unit.Experience)
                {
                    events.Add(new WorldUnitExperienceGainedEvent(group, unit.Id, trainedUnit.Experience - unit.Experience));
                }
            }

            return events;
        }

        private UnitGrid<WorldUnit> TrainUnits(UnitGrid<WorldUnit> units, int percent, WorldPlayerBuildings buildings)
        {
            UnitGrid<WorldUnit> trainedUnits = units;

            foreach ((WorldUnit unit, GridPosition _) in units.GetUnits())
            {
                trainedUnits = trainedUnits.Replace(unit, TrainUnit(unit, percent, buildings));
            }

            return trainedUnits;
        }

        private WorldUnit TrainUnit(WorldUnit unit, int percent, WorldPlayerBuildings buildings)
        {
            UnitExperience experience = Context.UnitStats.GetWorldUnitExperience(unit);

            if (!unit.IsAlive || experience.IsMaximumLevel)
            {
                return unit;
            }

            int newExperience = experience.Current + (experience.NextLevel * percent / 100);

            if (newExperience >= experience.NextLevel)
            {
                WorldUnit? upgradedUnit = FindUpgradedUnit(unit, buildings);

                if (upgradedUnit is not null)
                {
                    return upgradedUnit;
                }
            }

            return unit with { Experience = Math.Max(experience.Current, Math.Min(newExperience, experience.NextLevel - 1)) };
        }

        private WorldUnit? FindUpgradedUnit(WorldUnit unit, WorldPlayerBuildings buildings)
        {
            UnitUpgrade upgrade = UnitUpgradeCalculator.GetUpgrade(unit.DefinitionId, unit.IsTypeLocked, buildings, Context.Definitions);

            switch (upgrade)
            {
                case UnitUpgrade.TypeUpgrade typeUpgrade:
                    UnitDefinition target = Context.Definitions.GetUnit(typeUpgrade.ToUnitId);
                    return CreateUpgradedUnit(unit, target.Id, target.Level);
                case UnitUpgrade.DynamicUpgrade:
                    return CreateUpgradedUnit(unit, unit.DefinitionId, unit.Level + 1);
                case UnitUpgrade.Blocked:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException(nameof(unit), unit, null);
            }
        }

        private WorldUnit CreateUpgradedUnit(WorldUnit unit, UnitDefinitionId definitionId, int level)
        {
            WorldUnit upgradedUnit = unit with { DefinitionId = definitionId, Level = level, Experience = 0 };
            UnitStats stats = Context.UnitStats.GetWorldUnitStats(upgradedUnit);

            return upgradedUnit with { Hitpoints = stats.MaxHitpoints };
        }
    }
}
