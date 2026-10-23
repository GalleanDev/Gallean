using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

internal sealed class ChangeTerrainSpellResolver : IWorldSpellResolver<WorldChangeTerrainSpellEffect>, IStaticInstance<ChangeTerrainSpellResolver>
{
    public WorldCommandResult Resolve(
        WorldState state,
        IEngineContext context,
        WorldPlayerId? casterId,
        WorldSpellTarget? target,
        WorldSpellDefinition spell,
        WorldChangeTerrainSpellEffect effect)
    {
        WorldPlan plan = WorldPlan.For(state);
        ImmutableArray<WorldPosition>.Builder changed = ImmutableArray.CreateBuilder<WorldPosition>();

        foreach (WorldPosition tile in SpellTargetCalculator.GetArea(state, target?.Center, spell.AreaRadius))
        {
            if (CanChange(state.Map.GetCellAt(tile), effect) && !plan.HasStructureAt(tile))
            {
                changed.Add(tile);
            }
        }

        if (changed.Count > 0)
        {
            return WorldCommandResult.Success(
                Empty.Value,
                [new WorldMapFeatureChangedEvent(changed.ToImmutable(), effect.Feature)]);
        }
        else
        {
            return WorldCommandResult.Fail(WorldCommandError.SpellTargetInvalid);
        }
    }

    private static bool CanChange(WorldMapCell cell, WorldChangeTerrainSpellEffect effect)
    {
        return TerrainTransformCalculator.IsTransformable(cell) &&
               cell.Feature != WorldMapFeature.Road &&
               cell.Feature != effect.Feature;
    }
}
