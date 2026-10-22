using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Gallean.Common;

namespace Gallean.Client;

internal sealed class BattleGridViewModel : ObservableObject
{
    public ObservableCollection<BattleGridUnitViewModel> Units { get; } = [];
    public ObservableCollection<BattleGridTileViewModel> Tiles { get; } = [];

    public void UpdateUnits(IEnumerable<BattleGridUnitViewModel> units)
    {
        List<BattleGridUnitViewModel> newItems = [.. units];

        int index = 0;

        foreach (CollectionJoin.Item<BattleGridUnitViewModel> item in CollectionJoin.Make(Units, newItems, unit => unit.UnitId))
        {
            if (item.Right is null)
            {
                Units.RemoveAt(index);
            }
            else if (item.Left is null)
            {
                Units.Insert(index, item.Right);
                index++;
            }
            else if (!Equals(item.Left, item.Right))
            {
                Units[index].Update(item.Right);
                index++;
            }
            else
            {
                index++;
            }
        }
    }

    public void UpdateTiles(IEnumerable<BattleGridTileViewModel> tiles)
    {
        List<BattleGridTileViewModel> newItems = [.. tiles];

        int index = 0;

        foreach (CollectionJoin.Item<BattleGridTileViewModel> item in CollectionJoin.Make(Tiles, newItems, tile => tile.Tile.Position.ToIndex()))
        {
            if (item.Right is null)
            {
                Tiles.RemoveAt(index);
            }
            else if (item.Left is null)
            {
                Tiles.Insert(index, item.Right);
                index++;
            }
            else if (!Equals(item.Left, item.Right))
            {
                Tiles[index].Update(item.Right);
                index++;
            }
            else
            {
                index++;
            }
        }
    }
}
