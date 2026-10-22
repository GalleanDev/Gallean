using Gallean.Common;

namespace Gallean.Engine;

internal sealed class BoostSpellResolver : WorldAbstractModifierSpellResolver<WorldBoostSpellEffect>, IStaticInstance<BoostSpellResolver>
{
    protected override bool IsTarget(
        WorldState state,
        IGameDefinitionProvider definitions,
        WorldPlayerId casterId,
        WorldStack stack,
        WorldSpellDefinition spell)
    {
        if (spell.AreaRadius is not null)
        {
            return !DiplomacyCalculator.IsHostile(state, casterId, stack.OwnerId);
        }
        else
        {
            return stack.OwnerId == casterId;
        }
    }

    protected override UnitModifierDefinitionId GetModifier(WorldBoostSpellEffect effect)
    {
        return effect.Modifier;
    }
}
