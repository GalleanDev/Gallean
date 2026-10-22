using System.Collections.Immutable;

namespace Gallean.Engine.Contracts;

public sealed partial record UnitGrid<T>
{
    public sealed class Builder
    {
        private readonly T?[] baseUnits = new T?[GridPosition.Count];
        private readonly T?[] overlayUnits = new T?[GridPosition.Count];

        public Builder()
        {
        }

        public Builder(UnitGrid<T> grid)
        {
            grid.BaseUnits.CopyTo(baseUnits, 0);

            if (!grid.OverlayUnits.IsDefaultOrEmpty)
            {
                grid.OverlayUnits.CopyTo(overlayUnits, 0);
            }
        }

        public Builder Place(T unit, GridPosition position, bool isLarge)
        {
            if (!TryPlace(unit, position, isLarge))
            {
                throw new InvalidOperationException($"Position ({position.Line}, {position.Flank}) is already occupied.");
            }

            return this;
        }

        public bool TryPlace(T unit, GridPosition position, bool isLarge)
        {
            return PlaceTo(baseUnits, unit, position, isLarge);
        }

        public Builder PlaceOverlay(T unit, GridPosition position, bool isLarge)
        {
            if (!TryPlaceOverlay(unit, position, isLarge))
            {
                throw new InvalidOperationException($"Overlay position ({position.Line}, {position.Flank}) is already occupied.");
            }

            return this;
        }

        private bool TryPlaceOverlay(T unit, GridPosition position, bool isLarge)
        {
            return PlaceTo(overlayUnits, unit, position, isLarge);
        }

        public Builder RemoveOverlay(GridPosition position)
        {
            RemoveFrom(overlayUnits, position);

            return this;
        }

        public UnitGrid<T> Build()
        {
            if (Array.TrueForAll(overlayUnits, static unit => unit is null))
            {
                return new UnitGrid<T>([.. baseUnits], ImmutableArray<T?>.Empty);
            }
            else
            {
                return new UnitGrid<T>([.. baseUnits], [.. overlayUnits]);
            }
        }

        private static bool PlaceTo(T?[] grid, T unit, GridPosition position, bool isLarge)
        {
            if (isLarge)
            {
                int frontIndex = GridPosition.AtFrontLine(position.Flank).ToIndex();
                int backIndex = GridPosition.AtBackLine(position.Flank).ToIndex();

                if (grid[frontIndex] is not null || grid[backIndex] is not null)
                {
                    return false;
                }

                grid[frontIndex] = unit;
                grid[backIndex] = unit;
            }
            else
            {
                int index = position.ToIndex();

                if (grid[index] is not null)
                {
                    return false;
                }

                grid[index] = unit;
            }

            return true;
        }

        private static void RemoveFrom(T?[] grid, GridPosition position)
        {
            int index = position.ToIndex();

            if (grid[index] is null)
            {
                return;
            }

            int frontIndex = GridPosition.AtFrontLine(position.Flank).ToIndex();
            int backIndex = GridPosition.AtBackLine(position.Flank).ToIndex();

            if (grid[frontIndex] is not null && ReferenceEquals(grid[frontIndex], grid[backIndex]))
            {
                grid[frontIndex] = null;
                grid[backIndex] = null;
            }
            else
            {
                grid[index] = null;
            }
        }
    }
}
