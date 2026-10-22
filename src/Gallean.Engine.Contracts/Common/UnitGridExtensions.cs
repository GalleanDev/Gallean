namespace Gallean.Engine.Contracts;

public static class UnitGridExtensions
{
    extension<T>(UnitGrid<T>) where T : class
    {
        public static bool IsValidPosition(GridPosition position)
        {
            return position.Line is >= 0 and < GridPosition.LineCount &&
                   position.Flank is >= 0 and < GridPosition.FlankCount;
        }
    }

    extension<T>(UnitGrid<T> grid) where T : class
    {
        public bool IsLargeAt(GridPosition position)
        {
            T? front = grid[GridPosition.AtFrontLine(position.Flank)];
            T? back = grid[GridPosition.AtBackLine(position.Flank)];

            return front is not null &&
                   back is not null &&
                   ReferenceEquals(front, back);
        }

        public IEnumerable<(T Unit, GridPosition Position)> GetUnits()
        {
            foreach (GridPosition position in GridPosition.All)
            {
                T? unit = grid[position];

                if (unit is null)
                {
                    continue;
                }

                if (position.IsBackLine && grid.IsLargeAt(position))
                {
                    continue;
                }

                yield return (unit, position);
            }
        }

        public UnitGrid<T> Replace(T oldUnit, T newUnit)
        {
            return grid.Replace(unit => ReferenceEquals(unit, oldUnit), newUnit);
        }

        public UnitGrid<T> Replace(Func<T, bool> predicate, T newUnit)
        {
            UnitGrid<T> baseGrid = grid.GetBaseGrid();
            UnitGrid<T> overlayGrid = grid.GetOverlayGrid();
            UnitGrid<T>.Builder builder = new();

            foreach ((T unit, GridPosition position) in baseGrid.GetUnits())
            {
                if (predicate(unit))
                {
                    builder.Place(newUnit, position, baseGrid.IsLargeAt(position));
                }
                else
                {
                    builder.Place(unit, position, baseGrid.IsLargeAt(position));
                }
            }

            foreach ((T unit, GridPosition position) in overlayGrid.GetUnits())
            {
                if (predicate(unit))
                {
                    builder.PlaceOverlay(newUnit, position, overlayGrid.IsLargeAt(position));
                }
                else
                {
                    builder.PlaceOverlay(unit, position, overlayGrid.IsLargeAt(position));
                }
            }

            return builder.Build();
        }
    }
}
