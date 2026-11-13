using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed record ItemDetailsViewModel(
    AssetSpriteKey.ItemIcon IconKey,
    string Name,
    string CategoryText,
    string Description,
    ItemAttackViewModel? Attack,
    IReadOnlyList<ItemSummonViewModel> Summons,
    IReadOnlyList<StatRowViewModel> Stats,
    string? SpellEffect,
    OfferPriceViewModel? CastingCost,
    ResourceSetViewModel Value)
{
    public bool Equals(ItemDetailsViewModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return IconKey == other.IconKey &&
               Name == other.Name &&
               CategoryText == other.CategoryText &&
               Description == other.Description &&
               Attack == other.Attack &&
               Summons.SequenceEqual(other.Summons) &&
               Stats.SequenceEqual(other.Stats) &&
               SpellEffect == other.SpellEffect &&
               CastingCost == other.CastingCost &&
               Value == other.Value;
    }

    public static ItemDetailsViewModel Create(
        ItemDefinitionId definitionId,
        int? charges,
        WorldResourceSet resources,
        IGameDefinitionProvider definitions,
        IGameTextProvider texts)
    {
        ItemDefinition definition = definitions.GetItem(definitionId);
        UnitAttackDefinition? attack = GetAttack(definition, definitions);
        IReadOnlyList<StatRowViewModel> stats = [.. ItemStatRowBuilder.Build(definition, definitions, texts)];

        WorldResourceSet? castingCost = GetCastingCost(definition);
        OfferPriceViewModel? castingCostViewModel;

        if (castingCost is not null)
        {
            castingCostViewModel = new OfferPriceViewModel(castingCost.Value, resources);
        }
        else
        {
            castingCostViewModel = null;
        }

        return new ItemDetailsViewModel(
            ItemIconResolver.Resolve(definitions, definitionId),
            ItemFormatter.FormatName(definitions, texts, definitionId, charges),
            ItemFormatter.FormatCategory(definition.Category),
            ItemFormatter.FormatDescription(definitions, texts, definitionId),
            CreateAttack(attack),
            CreateSummons(attack, definitions, texts),
            stats,
            GetSpellEffect(definition, definitions, texts),
            castingCostViewModel,
            new ResourceSetViewModel(definition.Value, ResourceSetProjection.All));
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(IconKey);
        hash.Add(Name);
        hash.Add(CategoryText);
        hash.Add(Description);
        hash.Add(Attack);
        hash.AddSequence(Summons);
        hash.AddSequence(Stats);
        hash.Add(SpellEffect);
        hash.Add(CastingCost);
        hash.Add(Value);

        return hash.ToHashCode();
    }

    private static UnitAttackDefinition? GetAttack(ItemDefinition definition, IGameDefinitionProvider definitions)
    {
        UnitAttackDefinitionId? attackId;

        if (definition.Effect is AttackItemEffect effect)
        {
            attackId = effect.AttackId;
        }
        else
        {
            attackId = definition.EquipmentAttackId;
        }

        if (attackId is null)
        {
            return null;
        }

        return definitions.GetAttack(attackId.Value);
    }

    private static string? GetSpellEffect(ItemDefinition definition, IGameDefinitionProvider definitions, IGameTextProvider texts)
    {
        if (definition.Effect is SpellItemEffect effect)
        {
            WorldSpellDefinition spell = definitions.GetSpell(effect.SpellId);
            GlobalText text = texts.GetGlobalText(spell.EffectDescriptionTextId);

            return text.Text;
        }
        else
        {
            return null;
        }
    }

    private static WorldResourceSet? GetCastingCost(ItemDefinition definition)
    {
        if (definition.Effect is SpellItemEffect effect && effect.CastingCost != WorldResourceSet.Empty)
        {
            return effect.CastingCost;
        }
        else
        {
            return null;
        }
    }

    private static ItemAttackViewModel? CreateAttack(UnitAttackDefinition? attack)
    {
        if (attack is not null && attack.SummonUnitIds.IsEmpty)
        {
            return ItemAttackViewModel.Create(attack, attacksTwice: false);
        }
        else
        {
            return null;
        }
    }

    private static IReadOnlyList<ItemSummonViewModel> CreateSummons(
        UnitAttackDefinition? attack,
        IGameDefinitionProvider definitions,
        IGameTextProvider texts)
    {
        if (attack is null)
        {
            return [];
        }

        List<ItemSummonViewModel> summons = [];

        foreach (UnitDefinitionId unitId in attack.SummonUnitIds)
        {
            UnitDefinition unit = definitions.GetUnit(unitId);

            summons.Add(ItemSummonViewModel.Create(unit, texts));
        }

        return summons;
    }
}
