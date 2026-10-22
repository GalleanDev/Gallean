using System.Collections.Immutable;
using Avalonia.Media;
using Gallean.Common;

namespace Gallean.Client;

internal sealed record UnitModifierStatusViewModel(UnitModifierType Type, IReadOnlyList<UnitModifierViewModel> Modifiers) : UnitStatusViewModel
{
    public override string Name => Type switch
    {
        UnitModifierType.Boost => Tr("Boosted"),
        UnitModifierType.Weaken => Tr("Weakened")
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

    public override UnitStatusCategory Category => Type switch
    {
        UnitModifierType.Boost => UnitStatusCategory.Buff,
        UnitModifierType.Weaken => UnitStatusCategory.Debuff
    };

    public override IconMoniker Icon => IconMoniker.TemporaryModifier;

    public override Color Color => Type switch
    {
        UnitModifierType.Boost => Theme.Battle.StatusEffect.BoostModifierColor,
        UnitModifierType.Weaken => Theme.Battle.StatusEffect.WeakenModifierColor
    };

    public override int Count => Modifiers.Count;

    public static IReadOnlyList<UnitModifierStatusViewModel> CreateAll(IReadOnlyList<UnitModifierViewModel> modifiers)
    {
        List<UnitModifierStatusViewModel> result = [];

        foreach (UnitModifierType type in Enum.GetValues<UnitModifierType>())
        {
            IReadOnlyList<UnitModifierViewModel> matchingModifiers = [.. modifiers.Where(modifier => modifier.Type == type)];

            if (matchingModifiers.Count > 0)
            {
                result.Add(new UnitModifierStatusViewModel(type, matchingModifiers));
            }
        }

        return result;
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

        return Type == other.Type &&
               Modifiers.SequenceEqual(other.Modifiers);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(Type);
        hash.AddSequence(Modifiers);

        return hash.ToHashCode();
    }
}
