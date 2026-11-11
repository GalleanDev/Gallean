using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal static partial class ItemStatRowBuilder
{
    private sealed record Context(IGameDefinitionProvider Definitions, IGameTextProvider Texts);

    private sealed class EffectVisitor : IItemEffectVisitor<Context, IEnumerable<StatRowViewModel>>, IStaticInstance<EffectVisitor>
    {
        public IEnumerable<StatRowViewModel> Visit(HealItemEffect effect, Context context)
        {
            yield return CreateRow(IconMoniker.Hitpoint, Tr("Hit points"), $"{effect.Hitpoints:+0;-0}");
        }

        public IEnumerable<StatRowViewModel> Visit(AttackItemEffect effect, Context context)
        {
            UnitAttackDefinition attack = context.Definitions.GetAttack(effect.AttackId);

            return BuildAttackRows(attack);
        }

        public IEnumerable<StatRowViewModel> Visit(ReviveItemEffect effect, Context context)
        {
            yield return CreateRow(IconMoniker.Resurrect, UnitAttackFormatter.FormatType(UnitAttackType.Revive));
        }

        public IEnumerable<StatRowViewModel> Visit(TemporaryBoostItemEffect effect, Context context)
        {
            UnitModifierDefinition modifier = context.Definitions.GetUnitModifier(effect.ModifierId);

            return BuildModifierRows(modifier, context.Texts);
        }

        public IEnumerable<StatRowViewModel> Visit(PermanentBoostItemEffect effect, Context context)
        {
            UnitModifierDefinition modifier = context.Definitions.GetUnitModifier(effect.ModifierId);

            return BuildModifierRows(modifier, context.Texts);
        }

        public IEnumerable<StatRowViewModel> Visit(SpellItemEffect effect, Context context)
        {
            return [];
        }
    }
}
