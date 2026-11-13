using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed record ItemSummonViewModel(
    AssetSpriteKey.UnitPortrait PortraitKey,
    string Name,
    int Level,
    ItemAttackViewModel Attack,
    IReadOnlyList<StatRowViewModel> Stats)
{
    public string LevelText => Tr("Level {0}", Level);

    public bool Equals(ItemSummonViewModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return PortraitKey == other.PortraitKey &&
               Name == other.Name &&
               Level == other.Level &&
               Attack == other.Attack &&
               Stats.SequenceEqual(other.Stats);
    }

    public static ItemSummonViewModel Create(UnitDefinition definition, IGameTextProvider texts)
    {
        GlobalText name = texts.GetGlobalText(definition.NameTextId);
        UnitAttackDefinition attack = definition.Attack;

        return new ItemSummonViewModel(
            new AssetSpriteKey.UnitPortrait(definition.Id),
            name.Text,
            definition.Level,
            ItemAttackViewModel.Create(attack, definition.AttacksTwice),
            [
                new StatRowViewModel(IconMoniker.Hitpoint, Tr("Hit points"), CreateValue(definition.MaxHitpoints.ToString()), null),
                new StatRowViewModel(IconMoniker.Armor, Tr("Armor"), CreateValue(definition.Armor.ToString()), null),
                new StatRowViewModel(IconMoniker.Target, Tr("Targets"), CreateTargets(attack.Reach), null),
                new StatRowViewModel(IconMoniker.Accuracy, Tr("Accuracy"), CreateValue($"{attack.Accuracy}%"), null),
                new StatRowViewModel(IconMoniker.Initiative, Tr("Initiative"), CreateValue(attack.Initiative.ToString()), null)
            ]);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(PortraitKey);
        hash.Add(Name);
        hash.Add(Level);
        hash.Add(Attack);
        hash.AddSequence(Stats);

        return hash.ToHashCode();
    }

    private static StatValueViewModel CreateValue(string text)
    {
        return new StatValueViewModel(TextSpans.FromText(text), 0);
    }

    private static StatValueViewModel CreateTargets(UnitAttackReach reach)
    {
        return reach switch
        {
            UnitAttackReach.All => new StatValueViewModel(TrSpans("{0:b} {1:i}", GridPosition.Count, Tr("(All)")), 0),
            UnitAttackReach.Any => new StatValueViewModel(TrSpans("{0:b} {1:i}", 1, Tr("(Ranged)")), 0),
            UnitAttackReach.Adjacent => new StatValueViewModel(TrSpans("{0:b} {1:i}", 1, Tr("(Melee)")), 0)
        };
    }
}
