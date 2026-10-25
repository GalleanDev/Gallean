using Gallean.Common;

namespace Gallean.Engine;

internal static partial class FortificationCalculator
{
    private sealed record ProtectionContext(WorldState State, IGameDefinitionProvider Definitions);

    private sealed class ProtectionModifierVisitor : IWorldFortificationVisitor<ProtectionContext, UnitModifierDefinitionId?>, IStaticInstance<ProtectionModifierVisitor>
    {
        public UnitModifierDefinitionId? Visit(WorldCapital capital, ProtectionContext context)
        {
            WorldPlayer owner = context.State.Players[capital.OwnerId];

            return context.Definitions.GetRace(owner.Race).CapitalProtectionModifierId;
        }

        public UnitModifierDefinitionId? Visit(WorldCity city, ProtectionContext context)
        {
            return context.Definitions.GetCity(city.DefinitionId).ProtectionModifierId;
        }

        public UnitModifierDefinitionId? Visit(WorldRuin ruin, ProtectionContext context)
        {
            return null;
        }
    }
}
