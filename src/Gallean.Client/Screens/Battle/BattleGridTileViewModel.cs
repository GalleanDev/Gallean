using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Gallean.Client;

internal sealed partial class BattleGridTileViewModel(
    BattleGridTile tile,
    int row,
    int column,
    Color? marker) : ObservableObject, IEquatable<BattleGridTileViewModel>
{
    public BattleGridTile Tile { get; } = tile;

    [ObservableProperty]
    public partial int Row { get; private set; } = row;

    [ObservableProperty]
    public partial int Column { get; private set; } = column;

    [ObservableProperty]
    public partial Color? Marker { get; private set; } = marker;

    public void Update(BattleGridTileViewModel other)
    {
        Row = other.Row;
        Column = other.Column;
        Marker = other.Marker;
    }

    public bool Equals(BattleGridTileViewModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Tile == other.Tile &&
               Row == other.Row &&
               Column == other.Column &&
               Marker == other.Marker;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as BattleGridTileViewModel);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Tile, Row, Column, Marker);
    }
}
