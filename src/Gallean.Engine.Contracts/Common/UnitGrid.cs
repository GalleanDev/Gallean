using System.Collections.Immutable;
using Gallean.Common;

namespace Gallean.Engine.Contracts;

public sealed partial record UnitGrid<T> where T : class
{
    public static readonly UnitGrid<T> Empty = new([.. new T?[GridPosition.Count]], ImmutableArray<T?>.Empty);

    private UnitGrid(ImmutableArray<T?> baseUnits, ImmutableArray<T?> overlayUnits)
    {
        BaseUnits = baseUnits;
        OverlayUnits = overlayUnits;
    }

    private ImmutableArray<T?> BaseUnits { get; }
    private ImmutableArray<T?> OverlayUnits { get; }

    public bool IsEmpty => OverlayUnits.IsEmpty && BaseUnits.All(unit => unit is null);

    public T? this[GridPosition position]
    {
        get
        {
            if (OverlayUnits.IsEmpty)
            {
                return BaseUnits[position.ToIndex()];
            }
            else
            {
                return GetUnitAtPosition(position);
            }
        }
    }

    public bool Equals(UnitGrid<T>? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return BaseUnits.SequenceEqual(other.BaseUnits) &&
               OverlayUnits.SequenceEqual(other.OverlayUnits);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.AddSequence(BaseUnits);
        hash.AddSequence(OverlayUnits);

        return hash.ToHashCode();
    }

    public UnitGrid<T> GetBaseGrid()
    {
        if (OverlayUnits.IsEmpty)
        {
            return this;
        }
        else
        {
            return new UnitGrid<T>(BaseUnits, ImmutableArray<T?>.Empty);
        }
    }

    public UnitGrid<T> GetOverlayGrid()
    {
        if (OverlayUnits.IsEmpty)
        {
            return Empty;
        }
        else
        {
            return new UnitGrid<T>(OverlayUnits, ImmutableArray<T?>.Empty);
        }
    }

    private T? GetUnitAtPosition(GridPosition position)
    {
        int index = position.ToIndex();

        T? overlayUnit = OverlayUnits[index];
        if (overlayUnit is not null)
        {
            return overlayUnit;
        }

        T? baseUnit = BaseUnits[index];
        if (baseUnit is null)
        {
            return null;
        }

        int frontIndex = GridPosition.AtFrontLine(position.Flank).ToIndex();
        int backIndex = GridPosition.AtBackLine(position.Flank).ToIndex();

        if (ReferenceEquals(BaseUnits[frontIndex], BaseUnits[backIndex]) &&
            (OverlayUnits[frontIndex] is not null || OverlayUnits[backIndex] is not null))
        {
            return null;
        }

        return baseUnit;
    }
}
