using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed partial class BattleAssetPreloadPlanBuilder
{
    private sealed class ItemEffectVisitor : IItemEffectVisitor<IGameDefinitionProvider, UnitAttackType?>, IStaticInstance<ItemEffectVisitor>
    {
        public UnitAttackType? Visit(HealItemEffect effect, IGameDefinitionProvider definitions)
        {
            return UnitAttackType.Heal;
        }

        public UnitAttackType? Visit(AttackItemEffect effect, IGameDefinitionProvider definitions)
        {
            UnitAttackDefinition attack = definitions.GetAttack(effect.AttackId);

            return attack.Type;
        }

        public UnitAttackType? Visit(ReviveItemEffect effect, IGameDefinitionProvider definitions)
        {
            return UnitAttackType.Revive;
        }

        public UnitAttackType? Visit(TemporaryBoostItemEffect effect, IGameDefinitionProvider definitions)
        {
            return null;
        }

        public UnitAttackType? Visit(PermanentBoostItemEffect effect, IGameDefinitionProvider definitions)
        {
            return null;
        }

        public UnitAttackType? Visit(SpellItemEffect effect, IGameDefinitionProvider definitions)
        {
            return null;
        }
    }
}
