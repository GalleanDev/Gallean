using Gallean.Common;

namespace Gallean.Engine;

internal sealed class WorldAiSpellEffectTypeVisitor : IWorldSpellEffectVisitor<Empty, WorldAiSpellEffectType?>, IStaticInstance<WorldAiSpellEffectTypeVisitor>
{
    public WorldAiSpellEffectType? Visit(WorldAttackSpellEffect effect, Empty context)
    {
        return WorldAiSpellEffectType.Attack;
    }

    public WorldAiSpellEffectType? Visit(WorldWeakenSpellEffect effect, Empty context)
    {
        return null;
    }

    public WorldAiSpellEffectType? Visit(WorldHealSpellEffect effect, Empty context)
    {
        return WorldAiSpellEffectType.Heal;
    }

    public WorldAiSpellEffectType? Visit(WorldBoostSpellEffect effect, Empty context)
    {
        return WorldAiSpellEffectType.Boost;
    }

    public WorldAiSpellEffectType? Visit(WorldSummonStackSpellEffect effect, Empty context)
    {
        return WorldAiSpellEffectType.Summon;
    }

    public WorldAiSpellEffectType? Visit(WorldFogSpellEffect effect, Empty context)
    {
        return null;
    }

    public WorldAiSpellEffectType? Visit(WorldUnfogSpellEffect effect, Empty context)
    {
        return null;
    }

    public WorldAiSpellEffectType? Visit(WorldRestoreMovementSpellEffect effect, Empty context)
    {
        return null;
    }

    public WorldAiSpellEffectType? Visit(WorldInvisibilitySpellEffect effect, Empty context)
    {
        return WorldAiSpellEffectType.Invisibility;
    }

    public WorldAiSpellEffectType? Visit(WorldRemoveRodSpellEffect effect, Empty context)
    {
        return null;
    }

    public WorldAiSpellEffectType? Visit(WorldChangeTerrainSpellEffect effect, Empty context)
    {
        return null;
    }
}
