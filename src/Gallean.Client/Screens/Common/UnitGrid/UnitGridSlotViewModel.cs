using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Gallean.Client;

internal sealed partial class UnitGridSlotViewModel(
    UnitGridViewModel grid,
    GridPosition position,
    int row,
    int column,
    bool isLarge,
    WorldUnitId? unitId,
    AssetSpriteKey.UnitFace? faceKey,
    int hitpoints,
    int maxHitpoints,
    bool isAlive,
    bool isPendingUpgrade,
    bool isVisible,
    bool isSelected,
    bool isHighlighted,
    bool showsPurchaseHint,
    UnitGridSelectionViewModel selection,
    UnitDetailsViewModel? details) : ObservableObject, IDragSource, IDropTarget, IDetailsSource, IEquatable<UnitGridSlotViewModel>
{
    public UnitGridViewModel Grid { get; } = grid;
    public GridPosition Position { get; } = position;
    public WorldUnitId? UnitId { get; } = unitId;

    public UnitGridSlotReference Reference => new(Grid, Position);

    public DragPreview? DragPreview
    {
        get
        {
            if (FaceKey is null)
            {
                return null;
            }
            else if (IsLarge)
            {
                return new DragPreview(FaceKey, DragPreviewType.LargeUnit);
            }
            else
            {
                return new DragPreview(FaceKey, DragPreviewType.SmallUnit);
            }
        }
    }

    IDragSourceReference IDragSource.DragReference => Reference;

    object? IDetailsSource.DetailsContent => Details;

    IDragTargetReference IDropTarget.DropReference => Reference;

    [ObservableProperty]
    public partial int Row { get; private set; } = row;

    [ObservableProperty]
    public partial int Column { get; private set; } = column;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ColumnSpan))]
    public partial bool IsLarge { get; private set; } = isLarge;

    public int ColumnSpan => IsLarge ? GridPosition.LineCount : 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUnit))]
    public partial AssetSpriteKey.UnitFace? FaceKey { get; private set; } = faceKey;

    [ObservableProperty]
    public partial int Hitpoints { get; private set; } = hitpoints;

    [ObservableProperty]
    public partial int MaxHitpoints { get; private set; } = maxHitpoints;

    [ObservableProperty]
    public partial bool IsAlive { get; private set; } = isAlive;

    [ObservableProperty]
    public partial bool IsPendingUpgrade { get; private set; } = isPendingUpgrade;

    public bool HasUnit => FaceKey is not null;

    [ObservableProperty]
    public partial bool IsVisible { get; private set; } = isVisible;

    [ObservableProperty]
    public partial bool IsSelected { get; private set; } = isSelected;

    [ObservableProperty]
    public partial bool IsHighlighted { get; private set; } = isHighlighted;

    [ObservableProperty]
    public partial bool ShowsPurchaseHint { get; private set; } = showsPurchaseHint;

    [ObservableProperty]
    public partial UnitDetailsViewModel? Details { get; private set; } = details;

    public bool ShowsSameUnit(UnitGridSlotViewModel other)
    {
        return UnitId == other.UnitId && IsAlive == other.IsAlive;
    }

    public void Update(UnitGridSlotViewModel other)
    {
        Row = other.Row;
        Column = other.Column;
        IsLarge = other.IsLarge;
        FaceKey = other.FaceKey;
        Hitpoints = other.Hitpoints;
        MaxHitpoints = other.MaxHitpoints;
        IsAlive = other.IsAlive;
        IsPendingUpgrade = other.IsPendingUpgrade;
        IsVisible = other.IsVisible;
        IsSelected = other.IsSelected;
        IsHighlighted = other.IsHighlighted;
        ShowsPurchaseHint = other.ShowsPurchaseHint;
        Details = other.Details;
    }

    [RelayCommand]
    private async Task SelectAsync()
    {
        await selection.SelectSlotAsync(this);
    }

    public bool Equals(UnitGridSlotViewModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Grid == other.Grid &&
               Position == other.Position &&
               UnitId == other.UnitId &&
               Row == other.Row &&
               Column == other.Column &&
               IsLarge == other.IsLarge &&
               FaceKey == other.FaceKey &&
               Hitpoints == other.Hitpoints &&
               MaxHitpoints == other.MaxHitpoints &&
               IsAlive == other.IsAlive &&
               IsPendingUpgrade == other.IsPendingUpgrade &&
               IsVisible == other.IsVisible &&
               IsSelected == other.IsSelected &&
               IsHighlighted == other.IsHighlighted &&
               ShowsPurchaseHint == other.ShowsPurchaseHint &&
               Details == other.Details;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as UnitGridSlotViewModel);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(Grid);
        hash.Add(Position);
        hash.Add(UnitId);
        hash.Add(Row);
        hash.Add(Column);
        hash.Add(IsLarge);
        hash.Add(FaceKey);
        hash.Add(Hitpoints);
        hash.Add(MaxHitpoints);
        hash.Add(IsAlive);
        hash.Add(IsPendingUpgrade);
        hash.Add(IsVisible);
        hash.Add(IsSelected);
        hash.Add(IsHighlighted);
        hash.Add(ShowsPurchaseHint);
        hash.Add(Details);

        return hash.ToHashCode();
    }
}
