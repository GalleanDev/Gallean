using System.Collections.Immutable;

namespace Gallean.Engine;

internal static class WorldUnitWardCalculator
{
    public static ImmutableArray<UnitWard> GetWards(WorldUnit unit, IGameDefinitionProvider definitions)
    {
        ImmutableArray<UnitWard>.Builder wards = ImmutableArray.CreateBuilder<UnitWard>();

        wards.AddRange(definitions.GetUnit(unit.DefinitionId).Wards);

        foreach (UnitModifier modifier in unit.Modifiers)
        {
            wards.AddRange(definitions.GetUnitModifier(modifier.DefinitionId).Wards);
        }

        return wards.ToImmutable();
    }
}
