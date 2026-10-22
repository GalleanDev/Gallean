namespace Gallean.Engine;

internal partial class WorldEngine
{
    private sealed class SpellCastEffectRule(IEngineContext context) : WorldEventRule(context)
    {
        public override EventRuleResult<WorldEvent> Visit(WorldSpellCastEvent ev, WorldState state)
        {
            WorldSpellDefinition definition = Context.Definitions.GetSpell(ev.SpellId);
            WorldCommandResult result = WorldSpellEffectResolver.Resolve(state, Context, ev.CasterId, ev.Target, definition);

            if (result.Failed)
            {
                return EventRuleResult<WorldEvent>.Empty;
            }

            return new EventRuleResult<WorldEvent>(false, result.Events);
        }
    }
}
