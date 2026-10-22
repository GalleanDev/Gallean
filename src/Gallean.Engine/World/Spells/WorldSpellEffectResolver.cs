using Gallean.Common;

namespace Gallean.Engine;

internal static class WorldSpellEffectResolver
{
    private readonly record struct Context(
        WorldState State,
        IEngineContext EngineContext,
        WorldPlayerId CasterId,
        WorldSpellTarget? Target,
        WorldSpellDefinition Spell);

    private sealed class EffectVisitor :
        IWorldSpellEffectVisitor<Context, WorldCommandResult>,
        IStaticInstance<EffectVisitor>
    {
        public WorldCommandResult Visit(WorldAttackSpellEffect effect, Context context) => Resolve(AttackSpellResolver.Instance, context, effect);
        public WorldCommandResult Visit(WorldWeakenSpellEffect effect, Context context) => Resolve(WeakenSpellResolver.Instance, context, effect);
        public WorldCommandResult Visit(WorldHealSpellEffect effect, Context context) => Resolve(HealSpellResolver.Instance, context, effect);
        public WorldCommandResult Visit(WorldBoostSpellEffect effect, Context context) => Resolve(BoostSpellResolver.Instance, context, effect);
        public WorldCommandResult Visit(WorldSummonStackSpellEffect effect, Context context) => Resolve(SummonSpellResolver.Instance, context, effect);
        public WorldCommandResult Visit(WorldFogSpellEffect effect, Context context) => Resolve(FogSpellResolver.Instance, context, effect);
        public WorldCommandResult Visit(WorldUnfogSpellEffect effect, Context context) => Resolve(UnfogSpellResolver.Instance, context, effect);
        public WorldCommandResult Visit(WorldRestoreMovementSpellEffect effect, Context context) => Resolve(RestoreMovementSpellResolver.Instance, context, effect);
        public WorldCommandResult Visit(WorldInvisibilitySpellEffect effect, Context context) => Resolve(InvisibilitySpellResolver.Instance, context, effect);
        public WorldCommandResult Visit(WorldRemoveRodSpellEffect effect, Context context) => Resolve(RemoveRodSpellResolver.Instance, context, effect);
        public WorldCommandResult Visit(WorldChangeTerrainSpellEffect effect, Context context) => Resolve(ChangeTerrainSpellResolver.Instance, context, effect);
    }

    public static WorldCommandResult Resolve(
        WorldState state,
        IEngineContext context,
        WorldPlayerId casterId,
        WorldSpellTarget? target,
        WorldSpellDefinition spell)
    {
        return spell.Effect.Accept(EffectVisitor.Instance, new Context(state, context, casterId, target, spell));
    }

    private static WorldCommandResult Resolve<TEffect>(
        IWorldSpellResolver<TEffect> resolver,
        Context context,
        TEffect effect)
        where TEffect : WorldAbstractSpellEffect
    {
        return resolver.Resolve(
            context.State,
            context.EngineContext,
            context.CasterId,
            context.Target,
            context.Spell,
            effect);
    }
}
