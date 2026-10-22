using Gallean.Common;

namespace Gallean.Engine;

internal static partial class WorldHealCalculator
{
    private sealed class RegenerationVisitor : IWorldFortificationVisitor<RegenerationVisitor.Context, UnitGrid<WorldUnit>>, IStaticInstance<RegenerationVisitor>
    {
        public readonly record struct Context(WorldState State, UnitGrid<WorldUnit> Units, IEngineContext EngineContext);

        public UnitGrid<WorldUnit> Visit(WorldCapital capital, Context context)
        {
            WorldPlayer owner = context.State.Players[capital.OwnerId];
            WorldRaceDefinition ownerRace = context.EngineContext.Definitions.GetRace(owner.Race);
            int lordRegeneration = GetLordRegeneration(context.State, owner, context.EngineContext);

            return RegenerateUnits(context.Units, lordRegeneration + ownerRace.CapitalRegeneration, context.EngineContext);
        }

        public UnitGrid<WorldUnit> Visit(WorldCity city, Context context)
        {
            WorldPlayer owner = context.State.Players[city.OwnerId];
            WorldCityDefinition definition = context.EngineContext.Definitions.GetCity(city.DefinitionId);
            int lordRegeneration = GetLordRegeneration(context.State, owner, context.EngineContext);

            return RegenerateUnits(context.Units, lordRegeneration + definition.Regeneration, context.EngineContext);
        }

        public UnitGrid<WorldUnit> Visit(WorldRuin ruin, Context context)
        {
            UnitGrid<WorldUnit>.Builder builder = new();

            foreach ((WorldUnit unit, GridPosition position) in context.Units.GetUnits())
            {
                UnitDefinition definition = context.EngineContext.Definitions.GetUnit(unit.DefinitionId);
                UnitStats stats = context.EngineContext.UnitStats.GetWorldUnitStats(unit);
                WorldUnit updatedUnit = RegenerateUnit(unit, stats, context.EngineContext.Globals.RuinRegeneration);

                builder.Place(updatedUnit, position, definition.IsLarge);
            }

            return builder.Build();
        }
    }
}
