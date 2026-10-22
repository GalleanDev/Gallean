using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed record ItemDetailsViewModel(
    AssetSpriteKey.ItemIcon IconKey,
    string Name,
    string CategoryText,
    string Description,
    IReadOnlyList<string> Effects,
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
               Value == other.Value;
    }

    public static ItemDetailsViewModel Create(
        ItemDefinitionId definitionId,
        int? charges,
        IGameDefinitionProvider definitions,
        IGameTextProvider texts)
    {
        ItemDefinition definition = definitions.GetItem(definitionId);
        IReadOnlyList<string> effects = [];

        if (definition.EquipmentModifierId is { } modifierId)
        {
            UnitModifierDefinition modifier = definitions.GetUnitModifier(modifierId);
            effects = [.. UnitModifierFormatter.FormatEffects(modifier)];
        }

        return new ItemDetailsViewModel(
            ItemIconResolver.Resolve(definitions, definitionId),
            ItemFormatter.FormatName(definitions, texts, definitionId, charges),
            ItemFormatter.FormatCategory(definition.Category),
            ItemFormatter.FormatDescription(definitions, texts, definitionId),
            effects,
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
        hash.Add(Value);

        return hash.ToHashCode();
    }
}
