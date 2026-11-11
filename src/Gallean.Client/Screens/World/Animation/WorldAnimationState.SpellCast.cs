namespace Gallean.Client;

internal closed partial record WorldAnimationState
{
    public sealed record SpellCast(
        WorldSpellDefinitionId SpellId,
        WorldPosition Position,
        WorldStackId? SummonedStackId) : WorldAnimationState;
}
