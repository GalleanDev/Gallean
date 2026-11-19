using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine;

public sealed record ScenarioStackTemplateUnit(
    UnitDefinitionId DefinitionId,
    int Hitpoints,
    int Level,
    ImmutableArray<UnitModifier> Modifiers,
    UnitName? Name)
{
    public bool Equals(ScenarioStackTemplateUnit? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return DefinitionId == other.DefinitionId &&
               Hitpoints == other.Hitpoints &&
               Level == other.Level &&
               Modifiers.SequenceEqual(other.Modifiers) &&
               Name == other.Name;
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(DefinitionId);
        hash.Add(Hitpoints);
        hash.Add(Level);
        hash.AddSequence(Modifiers);
        hash.Add(Name);

        return hash.ToHashCode();
    }
}
