using System.Collections.Immutable;
using Avalonia.Media;
using Gallean.Common;

namespace Gallean.Client;

internal sealed record UnitModifierStatusViewModel(
    UnitModifierCategory ModifierCategory,
    UnitModifierType ModifierType,
    IReadOnlyList<UnitModifierViewModel> Modifiers) : UnitStatusViewModel
{
    public override string Name => (ModifierCategory, ModifierType) switch
    {
        (UnitModifierCategory.Magic, UnitModifierType.Boost) => Tr("Boosted"),
        (UnitModifierCategory.Magic, UnitModifierType.Weaken) => Tr("Weakened"),
        (UnitModifierCategory.Potion, _) => Tr("Potion")
    };

    public override TextSpans Description
    {
        get
        {
            ImmutableArray<TextSpan>.Builder spans = ImmutableArray.CreateBuilder<TextSpan>();

            foreach (UnitModifierViewModel modifier in Modifiers)
            {
                if (spans.Count > 0)
                {
                    spans.Add(new TextSpan("\n", TextStyle.None));
                }

                spans.Add(new TextSpan(modifier.Effects, TextStyle.None));

                if (modifier.Duration is not null)
                {
                    spans.Add(new TextSpan(" – ", TextStyle.None));
                    spans.AddRange(modifier.Duration.Spans);
                }
            }

            return new TextSpans(spans.ToImmutable());
        }
    }

    public override UnitStatusCategory Category => ModifierType switch
    {
        UnitModifierType.Boost => UnitStatusCategory.Buff,
        UnitModifierType.Weaken => UnitStatusCategory.Debuff
    };

    public override IconMoniker Icon => ModifierCategory switch
    {
        UnitModifierCategory.Magic => IconMoniker.Magic,
        UnitModifierCategory.Potion => IconMoniker.Potion
    };

    public override Color Color => ModifierType switch
    {
        UnitModifierType.Boost => Theme.Battle.StatusEffect.BoostModifierColor,
        UnitModifierType.Weaken => Theme.Battle.StatusEffect.WeakenModifierColor
    };

    public override int Count => Modifiers.Count;

    public static IReadOnlyList<UnitModifierStatusViewModel> CreateAll(IReadOnlyList<UnitModifierViewModel> modifiers)
    {
        return
        [
            .. modifiers
                .GroupBy(modifier => (modifier.Category, modifier.Type))
                .OrderBy(group => group.Key.Category)
                .ThenBy(group => group.Key.Type)
                .Select(group => new UnitModifierStatusViewModel(group.Key.Category, group.Key.Type, [.. group]))
        ];
    }

    public bool Equals(UnitModifierStatusViewModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return ModifierCategory == other.ModifierCategory &&
               ModifierType == other.ModifierType &&
               Modifiers.SequenceEqual(other.Modifiers);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(ModifierCategory);
        hash.Add(ModifierType);
        hash.AddSequence(Modifiers);

        return hash.ToHashCode();
    }
}
