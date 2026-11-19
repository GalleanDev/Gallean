using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed record UnitSummonsViewModel(string Title, IReadOnlyList<SummonViewModel> Items)
{
    public bool IsCompact => Items.Count > 1;

    public bool Equals(UnitSummonsViewModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Title == other.Title &&
               Items.SequenceEqual(other.Items);
    }

    public static UnitSummonsViewModel? Create(UnitAttackDefinition attack, IGameDefinitionProvider definitions, IGameTextProvider texts)
    {
        return attack.Type switch
        {
            UnitAttackType.Summon => new UnitSummonsViewModel(Tr("Summons"), CreateItems(attack.SummonUnitIds, definitions, texts)),
            UnitAttackType.TransformSelf => new UnitSummonsViewModel(Tr("Transforms into"), CreateItems(GetTransformUnitIds(attack), definitions, texts)),
            UnitAttackType.TransformEnemy => new UnitSummonsViewModel(Tr("Transforms enemies into"), CreateItems(GetTransformUnitIds(attack), definitions, texts)),
            UnitAttackType.Damage => null,
            UnitAttackType.DrainLife => null,
            UnitAttackType.Paralyze => null,
            UnitAttackType.Heal => null,
            UnitAttackType.Fear => null,
            UnitAttackType.IncreaseDamage => null,
            UnitAttackType.Petrify => null,
            UnitAttackType.ReduceDamage => null,
            UnitAttackType.ReduceInitiative => null,
            UnitAttackType.Poison => null,
            UnitAttackType.Frostbite => null,
            UnitAttackType.Revive => null,
            UnitAttackType.DrainLifeOverflow => null,
            UnitAttackType.Cure => null,
            UnitAttackType.ReduceLevel => null,
            UnitAttackType.GiveAdditionalAttack => null,
            UnitAttackType.Doppelganger => null,
            UnitAttackType.Blister => null,
            UnitAttackType.GiveProtection => null,
            UnitAttackType.ReduceArmor => null
        };
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(Title);
        hash.AddSequence(Items);

        return hash.ToHashCode();
    }

    private static IReadOnlyList<UnitDefinitionId> GetTransformUnitIds(UnitAttackDefinition attack)
    {
        List<UnitDefinitionId> unitIds = [];

        if (attack.SmallTransformUnitId is not null)
        {
            unitIds.Add(attack.SmallTransformUnitId.Value);
        }

        if (attack.LargeTransformUnitId is not null)
        {
            unitIds.Add(attack.LargeTransformUnitId.Value);
        }

        return unitIds;
    }

    private static IReadOnlyList<SummonViewModel> CreateItems(
        IReadOnlyList<UnitDefinitionId> unitIds,
        IGameDefinitionProvider definitions,
        IGameTextProvider texts)
    {
        List<SummonViewModel> items = [];

        foreach (UnitDefinitionId unitId in unitIds)
        {
            UnitDefinition unit = definitions.GetUnit(unitId);

            items.Add(SummonViewModel.Create(unit, texts));
        }

        return items;
    }
}
