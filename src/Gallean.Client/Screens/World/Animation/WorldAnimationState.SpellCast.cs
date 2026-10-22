namespace Gallean.Client;

internal abstract partial record WorldAnimationState
{
    public sealed record SpellCast(
        WorldSpellDefinitionId SpellId,
        WorldPosition Position,
        WorldStackId? SummonedStackId) : WorldAnimationState;
}
