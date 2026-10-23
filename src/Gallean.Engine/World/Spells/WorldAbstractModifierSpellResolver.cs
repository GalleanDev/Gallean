using Gallean.Common;

namespace Gallean.Engine;

internal abstract class WorldAbstractModifierSpellResolver<TEffect> : WorldAbstractUnitSpellResolver<TEffect>
    where TEffect : WorldAbstractSpellEffect
{
    protected override WorldCommandResult OnHit(
        IEngineContext context,
        WorldPlayerId? casterId,
        WorldStack stack,
        int powerPercent,
        TEffect effect)
    {
        List<WorldEvent> events = [];
        UnitModifierDefinitionId modifier = GetModifier(effect);

        foreach ((WorldUnit unit, GridPosition _) in stack.Units.GetUnits())
        {
            if (unit.IsAlive && unit.Modifiers.All(unitModifier => unitModifier.DefinitionId != modifier))
            {
                events.Add(new WorldUnitModifierAppliedEvent(
                    new WorldUnitGroup.Stack(stack.Id),
                    unit.Id,
                    new UnitModifier(modifier, context.Globals.SpellModifierDurationTurns)));
            }
        }

        return WorldCommandResult.Success(Empty.Value, events);
    }

    protected abstract UnitModifierDefinitionId GetModifier(TEffect effect);
}
