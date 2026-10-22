using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

internal static class BattleUnitWardCalculator
{
    private sealed class BlocksAttackVisitor : IUnitWardVisitor<BlocksAttackVisitor.Context, bool>, IStaticInstance<BlocksAttackVisitor>
    {
        public sealed record Context(UnitAttackDefinition Attack, UnitWardCategory Category);

        public bool Visit(UnitAttackClassWard ward, Context context)
        {
            return ward.Category == context.Category && ward.AttackType == context.Attack.Type;
        }

        public bool Visit(UnitAttackSourceWard ward, Context context)
        {
            return ward.Category == context.Category && ward.Source == context.Attack.Source;
        }
    }

    public static ImmutableArray<UnitWard> GetInitialWards(BattleUnit unit, IGameDefinitionProvider definitions)
    {
        ImmutableArray<UnitWard>.Builder wards = unit.Wards.ToBuilder();

        wards.AddRange(UnitStatsCalculator.GetWards(definitions.GetUnit(unit.DefinitionId), unit.Modifiers, definitions));

        return wards.ToImmutable();
    }

    public static IReadOnlyList<BattleUnitWardModifier> GetGrantableWards(
        BattleUnit unit,
        BattleUnitId casterId,
        UnitAttackDefinition attack,
        IGameDefinitionProvider definitions)
    {
        List<BattleUnitWardModifier> grants = [];

        foreach (UnitModifierDefinitionId modifierId in attack.WardModifierIds)
        {
            if (unit.WardModifiers.Any(existing => existing.ModifierId == modifierId))
            {
                continue;
            }

            UnitModifierDefinition modifier = definitions.GetUnitModifier(modifierId);

            foreach (UnitWard ward in modifier.Wards)
            {
                if (!unit.Wards.Contains(ward) && grants.All(grant => grant.Ward != ward))
                {
                    grants.Add(new BattleUnitWardModifier(casterId, modifierId, ward));
                    break;
                }
            }
        }

        return grants;
    }

    public static UnitWard? FindBlockingWard(BattleUnit target, UnitAttackDefinition attack)
    {
        UnitWard? ward;

        ward = FindWardByCategory(target, attack, UnitWardCategory.Always);
        if (ward is not null)
        {
            return ward;
        }

        ward = FindWardByCategory(target, attack, UnitWardCategory.Once);
        if (ward is not null)
        {
            return ward;
        }

        return null;
    }

    private static UnitWard? FindWardByCategory(BattleUnit target, UnitAttackDefinition attack, UnitWardCategory category)
    {
        BlocksAttackVisitor.Context context = new(attack, category);

        foreach (UnitWard ward in target.Wards)
        {
            if (ward.Accept(BlocksAttackVisitor.Instance, context))
            {
                return ward;
            }
        }

        return null;
    }
}
