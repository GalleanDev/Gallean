using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed record ItemDetailsViewModel(
    AssetSpriteKey.ItemIcon IconKey,
    string Name,
    string CategoryText,
    string Description,
    IReadOnlyList<string> Effects,
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
               Effects.SequenceEqual(other.Effects) &&
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
        IReadOnlyList<string> effects = [];

        if (definition.EquipmentModifierId is { } modifierId)
        {
            UnitModifierDefinition modifier = definitions.GetUnitModifier(modifierId);
            effects = [.. UnitModifierFormatter.FormatEffects(modifier, texts)];
        }

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
            effects,
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
        hash.AddSequence(Effects);
        hash.Add(CastingCost);
        hash.Add(Value);

        return hash.ToHashCode();
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
}
