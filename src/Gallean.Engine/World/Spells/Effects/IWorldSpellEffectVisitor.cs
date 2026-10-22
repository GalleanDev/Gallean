namespace Gallean.Engine;

public interface IWorldSpellEffectVisitor<in TContext, out TResult>
{
    TResult Visit(WorldAttackSpellEffect effect, TContext context);
    TResult Visit(WorldWeakenSpellEffect effect, TContext context);
    TResult Visit(WorldHealSpellEffect effect, TContext context);
    TResult Visit(WorldBoostSpellEffect effect, TContext context);
    TResult Visit(WorldSummonStackSpellEffect effect, TContext context);
    TResult Visit(WorldFogSpellEffect effect, TContext context);
    TResult Visit(WorldUnfogSpellEffect effect, TContext context);
    TResult Visit(WorldRestoreMovementSpellEffect effect, TContext context);
    TResult Visit(WorldInvisibilitySpellEffect effect, TContext context);
    TResult Visit(WorldRemoveRodSpellEffect effect, TContext context);
    TResult Visit(WorldChangeTerrainSpellEffect effect, TContext context);
}
