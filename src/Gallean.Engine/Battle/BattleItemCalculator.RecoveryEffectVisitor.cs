using Gallean.Common;

namespace Gallean.Engine;

internal static partial class BattleItemCalculator
{
    private sealed class RecoveryEffectVisitor : IItemEffectVisitor<IGameDefinitionProvider, bool>, IStaticInstance<RecoveryEffectVisitor>
    {
        public bool Visit(HealItemEffect effect, IGameDefinitionProvider definitions) => true;

        public bool Visit(AttackItemEffect effect, IGameDefinitionProvider definitions)
        {
            UnitAttackDefinition attack = definitions.GetAttack(effect.AttackId);
            UnitAttackTypeInfo attackInfo = attack.Type.GetInfo();

            return attackInfo.IsRecovery;
        }

        public bool Visit(ReviveItemEffect effect, IGameDefinitionProvider definitions) => true;
        public bool Visit(TemporaryBoostItemEffect effect, IGameDefinitionProvider definitions) => false;
        public bool Visit(PermanentBoostItemEffect effect, IGameDefinitionProvider definitions) => false;
        public bool Visit(SpellItemEffect effect, IGameDefinitionProvider definitions) => false;
    }
}
