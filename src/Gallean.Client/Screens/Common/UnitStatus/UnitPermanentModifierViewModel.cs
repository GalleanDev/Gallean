using Gallean.Common;

namespace Gallean.Client;

internal sealed record UnitPermanentModifierViewModel(
    UnitPermanentModifierType Type,
    IconMoniker Icon,
    string Name,
    string? Description,
    string? Effects,
    int Count,
    IReadOnlyList<UnitPermanentModifierSourceViewModel> Sources)
{
    public string CountText => $"×{Count}";

    public bool Equals(UnitPermanentModifierViewModel? other)
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
               Icon == other.Icon &&
               Name == other.Name &&
               Description == other.Description &&
               Effects == other.Effects &&
               Count == other.Count &&
               Sources.SequenceEqual(other.Sources);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(Type);
        hash.Add(Icon);
        hash.Add(Name);
        hash.Add(Description);
        hash.Add(Effects);
        hash.Add(Count);
        hash.AddSequence(Sources);

        return hash.ToHashCode();
    }
}
