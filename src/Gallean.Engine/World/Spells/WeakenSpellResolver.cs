using Gallean.Common;

namespace Gallean.Engine;

internal sealed class WeakenSpellResolver : WorldAbstractModifierSpellResolver<WorldWeakenSpellEffect>, IStaticInstance<WeakenSpellResolver>
{
    protected override bool IsTarget(
        WorldState state,
        IGameDefinitionProvider definitions,
        WorldPlayerId casterId,
        WorldStack stack,
        WorldSpellDefinition spell)
    {
        return DiplomacyCalculator.IsHostile(state, casterId, stack.OwnerId);
    }

    protected override UnitModifierDefinitionId GetModifier(WorldWeakenSpellEffect effect)
    {
        return effect.Modifier;
    }
}
