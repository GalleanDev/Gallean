namespace Gallean.Engine;

internal interface IWorldSpellResolver<in TEffect> where TEffect : WorldAbstractSpellEffect
{
    WorldCommandResult Resolve(
        WorldState state,
        IEngineContext context,
        WorldPlayerId? casterId,
        WorldSpellTarget? target,
        WorldSpellDefinition spell,
        TEffect effect);
}
