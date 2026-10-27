using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Gallean.Common;

namespace Gallean.Client;

internal sealed partial class BattleGridUnitViewModel(
    BattleUnitId unitId,
    BattleGridTile tile,
    AssetSpriteKey faceKey,
    int row,
    int column,
    int columnSpan,
    int hitpoints,
    int maxHitpoints,
    BattleUnitState.Presence presence,
    bool isPendingUpgrade,
    bool isCurrent,
    Color? marker,
    IReadOnlyList<BattleFeedbackViewModel> feedback,
    IReadOnlyList<UnitStatusViewModel> statuses,
    AnimationRange hitpointsAnimation) : ObservableObject, IEquatable<BattleGridUnitViewModel>
{
    public BattleUnitId UnitId { get; } = unitId;

    [ObservableProperty]
    public partial BattleGridTile Tile { get; private set; } = tile;

    [ObservableProperty]
    public partial AssetSpriteKey FaceKey { get; private set; } = faceKey;

    [ObservableProperty]
    public partial int Row { get; private set; } = row;

    [ObservableProperty]
    public partial int Column { get; private set; } = column;

    [ObservableProperty]
    public partial int ColumnSpan { get; private set; } = columnSpan;

    [ObservableProperty]
    public partial int Hitpoints { get; private set; } = hitpoints;

    [ObservableProperty]
    public partial int MaxHitpoints { get; private set; } = maxHitpoints;

    [ObservableProperty]
    public partial BattleUnitState.Presence Presence { get; private set; } = presence;

    [ObservableProperty]
    public partial bool IsPendingUpgrade { get; private set; } = isPendingUpgrade;

    [ObservableProperty]
    public partial bool IsCurrent { get; private set; } = isCurrent;

    [ObservableProperty]
    public partial Color? Marker { get; private set; } = marker;

    [ObservableProperty]
    public partial IReadOnlyList<BattleFeedbackViewModel> Feedback { get; private set; } = feedback;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Overlay))]
    public partial IReadOnlyList<UnitStatusViewModel> Statuses { get; private set; } = statuses;

    [ObservableProperty]
    public partial AnimationRange HitpointsAnimation { get; private set; } = hitpointsAnimation;

    public Color? Overlay => ResolveOverlay(Statuses);

    public void Update(BattleGridUnitViewModel other)
    {
        Tile = other.Tile;
        FaceKey = other.FaceKey;
        Row = other.Row;
        Column = other.Column;
        ColumnSpan = other.ColumnSpan;
        Hitpoints = other.Hitpoints;
        MaxHitpoints = other.MaxHitpoints;
        Presence = other.Presence;
        IsPendingUpgrade = other.IsPendingUpgrade;
        IsCurrent = other.IsCurrent;
        Marker = other.Marker;
        Feedback = other.Feedback;
        Statuses = other.Statuses;
        HitpointsAnimation = other.HitpointsAnimation;
    }

    public bool Equals(BattleGridUnitViewModel? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return UnitId == other.UnitId &&
               Tile == other.Tile &&
               FaceKey == other.FaceKey &&
               Row == other.Row &&
               Column == other.Column &&
               ColumnSpan == other.ColumnSpan &&
               Hitpoints == other.Hitpoints &&
               MaxHitpoints == other.MaxHitpoints &&
               Presence == other.Presence &&
               IsPendingUpgrade == other.IsPendingUpgrade &&
               IsCurrent == other.IsCurrent &&
               Marker == other.Marker &&
               Feedback.SequenceEqual(other.Feedback) &&
               Statuses.SequenceEqual(other.Statuses) &&
               HitpointsAnimation == other.HitpointsAnimation;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as BattleGridUnitViewModel);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();

        hash.Add(UnitId);
        hash.Add(Tile);
        hash.Add(FaceKey);
        hash.Add(Row);
        hash.Add(Column);
        hash.Add(ColumnSpan);
        hash.Add(Hitpoints);
        hash.Add(MaxHitpoints);
        hash.Add(Presence);
        hash.Add(IsPendingUpgrade);
        hash.Add(IsCurrent);
        hash.Add(Marker);
        hash.AddSequence(Feedback);
        hash.AddSequence(Statuses);
        hash.Add(HitpointsAnimation);

        return hash.ToHashCode();
    }

    private static Color? ResolveOverlay(IReadOnlyList<UnitStatusViewModel> statuses)
    {
        foreach (UnitStatusViewModel status in statuses)
        {
            if (status.Overlay is not null)
            {
                return status.Overlay;
            }
        }

        return null;
    }
}
