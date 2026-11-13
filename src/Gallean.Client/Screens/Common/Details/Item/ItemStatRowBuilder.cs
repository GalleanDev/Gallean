using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal static partial class ItemStatRowBuilder
{
    public static IEnumerable<StatRowViewModel> Build(ItemDefinition definition, IGameDefinitionProvider definitions, IGameTextProvider texts)
    {
        if (definition.EquipmentModifierId is { } modifierId)
        {
            UnitModifierDefinition modifier = definitions.GetUnitModifier(modifierId);

            foreach (StatRowViewModel row in BuildModifierRows(modifier, texts))
            {
                yield return row;
            }
        }

        if (definition.EquipmentAttackId is { } attackId)
        {
            UnitAttackDefinition attack = definitions.GetAttack(attackId);

            foreach (StatRowViewModel row in BuildAttackRows(attack))
            {
                yield return row;
            }
        }

        if (definition.Effect is not null)
        {
            Context context = new(definitions, texts);

            foreach (StatRowViewModel row in definition.Effect.Accept(EffectVisitor.Instance, context))
            {
                yield return row;
            }
        }
    }

    private static StatRowViewModel CreateRow(IconMoniker icon, string label)
    {
        return new StatRowViewModel(icon, label, new StatValueViewModel(TextSpans.Empty, 0), null);
    }

    private static StatRowViewModel CreateRow(IconMoniker icon, string label, string value)
    {
        return new StatRowViewModel(icon, label, new StatValueViewModel(TextSpans.FromText(value), 0), null);
    }

    private static IEnumerable<StatRowViewModel> BuildAttackRows(UnitAttackDefinition attack)
    {
        if (attack.SummonUnitIds.IsEmpty)
        {
            StatValueViewModel targets = attack.Reach switch
            {
                UnitAttackReach.All => new StatValueViewModel(TrSpans("{0:b} {1:i}", GridPosition.Count, Tr("(All)")), 0),
                UnitAttackReach.Any => new StatValueViewModel(TrSpans("{0:b} {1:i}", 1, Tr("(Ranged)")), 0),
                UnitAttackReach.Adjacent => new StatValueViewModel(TrSpans("{0:b} {1:i}", 1, Tr("(Melee)")), 0)
            };

            yield return new StatRowViewModel(IconMoniker.Target, Tr("Targets"), targets, null);
            yield return CreateRow(IconMoniker.Accuracy, Tr("Accuracy"), $"{attack.Accuracy}%");
        }
    }

    private static IEnumerable<StatRowViewModel> BuildModifierRows(UnitModifierDefinition modifier, IGameTextProvider texts)
    {
        foreach (string value in UnitModifierFormatter.FormatValues(modifier.Hitpoints))
        {
            yield return CreateRow(IconMoniker.Hitpoint, Tr("Hit points"), value);
        }

        foreach (string value in UnitModifierFormatter.FormatValues(modifier.Armor))
        {
            yield return CreateRow(IconMoniker.Armor, Tr("Armor"), value);
        }

        foreach (string value in UnitModifierFormatter.FormatValues(modifier.Damage))
        {
            yield return CreateRow(IconMoniker.Damage, Tr("Damage"), value);
        }

        foreach (string value in UnitModifierFormatter.FormatValues(modifier.Accuracy))
        {
            yield return CreateRow(IconMoniker.Accuracy, Tr("Accuracy"), value);
        }

        foreach (string value in UnitModifierFormatter.FormatValues(modifier.Initiative))
        {
            yield return CreateRow(IconMoniker.Initiative, Tr("Initiative"), value);
        }

        if (modifier.RegenerationPercent != 0)
        {
            yield return CreateRow(IconMoniker.Heal, Tr("Regeneration"), $"{modifier.RegenerationPercent:+0;-0}%");
        }

        foreach (string value in UnitModifierFormatter.FormatValues(modifier.Leadership))
        {
            yield return CreateRow(IconMoniker.Leadership, Tr("Leadership"), value);
        }

        foreach (string value in UnitModifierFormatter.FormatValues(modifier.MaxMovement))
        {
            yield return CreateRow(IconMoniker.Movement, Tr("Movement"), value);
        }

        foreach (string value in UnitModifierFormatter.FormatValues(modifier.VisionRadius))
        {
            yield return CreateRow(IconMoniker.Vision, Tr("Vision"), value);
        }

        string? moveAbility = UnitMoveAbilityFormatter.Format(modifier.MoveAbility);

        if (moveAbility is not null)
        {
            yield return CreateRow(UnitMoveAbilityIconResolver.Resolve(modifier.MoveAbility), moveAbility);
        }

        foreach (UnitWard ward in modifier.Wards)
        {
            UnitWardItemViewModel item = UnitWardItemBuilder.Build(ward, isActive: true);

            yield return ward.Category switch
            {
                UnitWardCategory.Once => CreateRow(item.Icon, Tr("Ward against {0}", item.Text)),
                UnitWardCategory.Always => CreateRow(item.Icon, Tr("Immune to {0}", item.Text))
            };
        }

        if (modifier.DrainPercent != 0)
        {
            yield return CreateRow(IconMoniker.Hitpoint, Tr("Drain life {0}%", modifier.DrainPercent));
        }

        if (modifier.FastRetreat)
        {
            yield return CreateRow(IconMoniker.Retreat, Tr("Instant retreat"));
        }

        if (modifier.PurchaseDiscountPercent != 0)
        {
            yield return CreateRow(IconMoniker.Gold, Tr("{0}% lower prices", modifier.PurchaseDiscountPercent));
        }

        foreach (LeaderAbility ability in Enum.GetValues<LeaderAbility>())
        {
            if (ability != LeaderAbility.None && modifier.LeaderAbilities.HasFlag(ability))
            {
                yield return CreateRow(LeaderAbilityIconResolver.Resolve(ability), LeaderAbilityFormatter.FormatName(ability, texts));
            }
        }
    }
}
