using System.Collections.Immutable;
using System.Text.Json.Serialization;
using Gallean.Common;

namespace Gallean.Engine.Contracts;

public sealed record WorldUnit(
    WorldUnitId Id,
    UnitDefinitionId DefinitionId,
    int Hitpoints,
    int Experience,
    int Level,
    ImmutableArray<UnitModifier> Modifiers,
    string? Name,
    bool IsTypeLocked)
{
    [JsonIgnore]
    public bool IsAlive => Hitpoints > 0;

    public bool Equals(WorldUnit? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Id == other.Id &&
               DefinitionId == other.DefinitionId &&
               Hitpoints == other.Hitpoints &&
               Experience == other.Experience &&
               Level == other.Level &&
               Modifiers.SequenceEqual(other.Modifiers) &&
               Name == other.Name &&
               IsTypeLocked == other.IsTypeLocked;
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(Id);
        hash.Add(DefinitionId);
        hash.Add(Hitpoints);
        hash.Add(Experience);
        hash.Add(Level);
        hash.AddSequence(Modifiers);
        hash.Add(Name);
        hash.Add(IsTypeLocked);

        return hash.ToHashCode();
    }
}
