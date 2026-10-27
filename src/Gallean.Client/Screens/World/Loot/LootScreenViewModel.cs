using System.Collections.Immutable;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed class LootScreenViewModel : AbstractScreenViewModel
{
    private LootScreenViewModel(string title, string text, ResourceSetViewModel? resources, ImmutableArray<LootItem> items)
    {
        Title = title;
        Text = text;
        Resources = resources;
        Items = items;
    }

    public string Title { get; }
    public string Text { get; }
    public ResourceSetViewModel? Resources { get; }
    public ImmutableArray<LootItem> Items { get; }

    public static LootScreenViewModel CreateForRuin(GameModel gameModel, WorldResourceSet resources, WorldItem? item)
    {
        ImmutableArray<WorldItem> items = [];

        if (item is not null)
        {
            items = [item];
        }

        string text;
        ResourceSetViewModel? foundResources;

        if (resources != WorldResourceSet.Empty)
        {
            text = Tr("You found treasure.");
            foundResources = new ResourceSetViewModel(resources, ResourceSetProjection.All);
        }
        else if (items.Length > 0)
        {
            text = Tr("You found an item.");
            foundResources = null;
        }
        else
        {
            text = Tr("You found nothing.");
            foundResources = null;
        }

        return new LootScreenViewModel(Tr("You are victorious!"), text, foundResources, CreateItems(gameModel, items));
    }

    public static LootScreenViewModel CreateForItems(GameModel gameModel, WorldInventory inventory)
    {
        string text;

        if (inventory.Items.Length == 1)
        {
            text = Tr("You received an item.");
        }
        else
        {
            text = Tr("You received {0} items.", inventory.Items.Length);
        }

        return new LootScreenViewModel(Tr("Treasure"), text, null, CreateItems(gameModel, inventory.Items));
    }

    public static LootScreenViewModel CreateForSpells(GameModel gameModel, ImmutableArray<WorldSpellDefinitionId> spells)
    {
        string text;

        if (spells.Length == 1)
        {
            text = Tr("You received a spell.");
        }
        else
        {
            text = Tr("You received {0} spells.", spells.Length);
        }

        return new LootScreenViewModel(Tr("New spells"), text, null, [.. spells.Select(spellId => CreateSpell(gameModel, spellId))]);
    }

    protected override bool TryClose()
    {
        CloseSelf();

        return true;
    }

    private static ImmutableArray<LootItem> CreateItems(GameModel gameModel, ImmutableArray<WorldItem> items)
    {
        List<LootItem> result = [];

        foreach (ItemGroup group in ItemGroup.Build(items))
        {
            result.Add(CreateItem(gameModel, group.DefinitionId, group.Charges, group.Items.Length));
        }

        return [.. result];
    }

    private static LootItem CreateItem(
        GameModel gameModel,
        ItemDefinitionId definitionId,
        int? charges,
        int quantity)
    {
        string name = ItemFormatter.FormatName(gameModel.Definitions, gameModel.Texts, definitionId, charges);
        string description = ItemFormatter.FormatDescription(gameModel.Definitions, gameModel.Texts, definitionId);
        IconViewModel.Item icon = new(ItemIconResolver.Resolve(gameModel.Definitions, definitionId));

        if (quantity > 1)
        {
            return new LootItem($"{quantity} × {name}", description, icon);
        }
        else
        {
            return new LootItem(name, description, icon);
        }
    }

    private static LootItem CreateSpell(GameModel gameModel, WorldSpellDefinitionId spellId)
    {
        WorldSpellDefinition definition = gameModel.Definitions.GetSpell(spellId);
        string name = gameModel.Texts.GetGlobalText(definition.NameTextId).Text;
        string description = gameModel.Texts.GetGlobalText(definition.DescriptionTextId).Text;

        return new LootItem(name, description, SpellIconResolver.Resolve(gameModel.Definitions, spellId));
    }
}
