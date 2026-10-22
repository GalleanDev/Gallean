using Gallean.Engine;
using Gallean.Engine.Tests;

namespace Gallean.Client.Tests;

public sealed class ItemIconResolverTests
{
    private static ItemDefinition Item(string id, ItemCategory category, ItemEffect? effect)
    {
        return new ItemDefinition(
            new ItemDefinitionId(id),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            category,
            WorldResourceSet.Empty,
            EquipmentModifierId: null,
            EquipmentAttackId: null,
            effect,
            AnimationUnitId: null);
    }

    private static WorldSpellDefinition Spell(string id, WorldRace race)
    {
        return new WorldSpellDefinition(
            new WorldSpellDefinitionId(id),
            new GlobalTextId("name"),
            new GlobalTextId("desc"),
            new GlobalTextId("effect"),
            race,
            Level: 1,
            CastingCost: WorldResourceSet.Empty,
            PurchaseCost: WorldResourceSet.Empty,
            AreaRadius: null,
            new WorldRestoreMovementSpellEffect(50),
            AiType: WorldAiSpellType.All);
    }

    [Test]
    public void ScrollTest()
    {
        WorldSpellDefinition spell = Spell("spell", WorldRace.Elf);
        ItemDefinition scroll = Item("scroll", ItemCategory.Scroll, new SpellItemEffect(spell.Id, WorldResourceSet.Empty));

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddSpell(spell)
            .AddItem(scroll)
            .Build();

        Assert.That(ItemIconResolver.Resolve(definitions, scroll.Id), Is.EqualTo(
            new AssetSpriteKey.ItemIcon(new AssetSpriteKey.ItemIconSource.Scroll(WorldRace.Elf))));
    }

    [Test]
    public void WandTest()
    {
        WorldSpellDefinition spell = Spell("spell", WorldRace.Elf);
        ItemDefinition wand = Item("wand", ItemCategory.Wand, new SpellItemEffect(spell.Id, WorldResourceSet.Empty));

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddSpell(spell)
            .AddItem(wand)
            .Build();

        Assert.That(ItemIconResolver.Resolve(definitions, wand.Id), Is.EqualTo(
            new AssetSpriteKey.ItemIcon(new AssetSpriteKey.ItemIconSource.Item(wand.Id))));
    }

    [Test]
    public void PotionTest()
    {
        ItemDefinition potion = Item("potion", ItemCategory.PotionHeal, new HealItemEffect(50));

        IGameDefinitionProvider definitions = new TestGameDefinitionProviderBuilder()
            .AddItem(potion)
            .Build();

        Assert.That(ItemIconResolver.Resolve(definitions, potion.Id), Is.EqualTo(
            new AssetSpriteKey.ItemIcon(new AssetSpriteKey.ItemIconSource.Item(potion.Id))));
    }
}
