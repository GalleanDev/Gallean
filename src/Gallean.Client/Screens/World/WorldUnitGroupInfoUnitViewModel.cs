using Gallean.Common;
using Gallean.Engine;

namespace Gallean.Client;

internal sealed record WorldUnitGroupInfoUnitViewModel(
    AssetSpriteKey.UnitFace FaceKey,
    string Name,
    int Hitpoints,
    int MaxHitpoints,
    bool IsAlive,
    int Row,
    int Column,
    int ColumnSpan,
    IReadOnlyList<UnitStatusViewModel> Statuses)
{
    public static WorldUnitGroupInfoUnitViewModel Create(
        WorldUnit unit,
        GridPosition position,
        bool isLarge,
        bool isFacingLeft,
        IGameDefinitionProvider definitions,
        IGameTextProvider texts,
        IUnitStatsProvider stats)
    {
        UnitStats unitStats = stats.GetWorldUnitStats(unit);
        int row = 2 - position.Flank;

        int column;
        if (isLarge)
        {
            column = 0;
        }
        else if (isFacingLeft)
        {
            column = position.Line;
        }
        else
        {
            column = 1 - position.Line;
        }

        int columnSpan = isLarge ? 2 : 1;
        IReadOnlyList<UnitModifierViewModel> modifiers = UnitModifierViewModel.CreateAll(unit, definitions, texts);
        IReadOnlyList<UnitStatusViewModel> statuses = UnitStatusViewModel.CreateAll([], modifiers);

        return new WorldUnitGroupInfoUnitViewModel(
            new AssetSpriteKey.UnitFace(unit.DefinitionId),
            UnitNameResolver.Resolve(unit, definitions, texts),
            unit.Hitpoints,
            unitStats.MaxHitpoints,
            unit.IsAlive,
            row,
            column,
            columnSpan,
            statuses);
    }

    public bool Equals(WorldUnitGroupInfoUnitViewModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return FaceKey == other.FaceKey &&
               Name == other.Name &&
               Hitpoints == other.Hitpoints &&
               MaxHitpoints == other.MaxHitpoints &&
               IsAlive == other.IsAlive &&
               Row == other.Row &&
               Column == other.Column &&
               ColumnSpan == other.ColumnSpan &&
               Statuses.SequenceEqual(other.Statuses);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(FaceKey);
        hash.Add(Name);
        hash.Add(Hitpoints);
        hash.Add(MaxHitpoints);
        hash.Add(IsAlive);
        hash.Add(Row);
        hash.Add(Column);
        hash.Add(ColumnSpan);
        hash.AddSequence(Statuses);

        return hash.ToHashCode();
    }
}
