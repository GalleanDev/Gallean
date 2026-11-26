using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Templates;
using Avalonia.Media;

namespace Gallean.Client;

[PseudoClasses(
    DeadPseudoClass,
    FreePseudoClass,
    HighLevel1PseudoClass,
    HighLevel2PseudoClass,
    HighLevel3PseudoClass,
    HighlightedPseudoClass,
    MarkedPseudoClass,
    SelectedPseudoClass)]
internal sealed partial class UnitPortrait : ContentControl
{
    private const string DeadPseudoClass = ":dead";
    private const string FreePseudoClass = ":free";
    private const string HighLevel1PseudoClass = ":high-level1";
    private const string HighLevel2PseudoClass = ":high-level2";
    private const string HighLevel3PseudoClass = ":high-level3";
    private const string HighlightedPseudoClass = ":highlighted";
    private const string MarkedPseudoClass = ":marked";
    private const string SelectedPseudoClass = ":selected";

    public static readonly StyledProperty<AnimationRange> HitpointsAnimationProperty =
        AvaloniaProperty.Register<UnitPortrait, AnimationRange>(nameof(HitpointsAnimation), AnimationRange.None);

    public UnitPortrait()
    {
        UpdatePseudoClasses();
    }

    [GeneratedStyledProperty]
    public partial AssetSpriteKey? FaceKey { get; set; }

    [GeneratedStyledProperty]
    public partial double Hitpoints { get; set; }

    [GeneratedStyledProperty]
    public partial double MaxHitpoints { get; set; }

    public AnimationRange HitpointsAnimation
    {
        get => GetValue(HitpointsAnimationProperty);
        set => SetValue(HitpointsAnimationProperty, value);
    }

    [GeneratedStyledProperty(DefaultValue = true)]
    public partial bool IsHitpointsVisible { get; set; }

    [GeneratedStyledProperty]
    public partial bool IsSelected { get; set; }

    [GeneratedStyledProperty]
    public partial bool IsHighlighted { get; set; }

    [GeneratedStyledProperty]
    public partial bool IsDead { get; set; }

    [GeneratedStyledProperty]
    public partial bool IsPendingUpgrade { get; set; }

    [GeneratedStyledProperty]
    public partial int HighLevelRank { get; set; }

    [GeneratedStyledProperty]
    public partial IBrush? Marker { get; set; }

    [GeneratedStyledProperty]
    public partial IBrush? Overlay { get; set; }

    [GeneratedStyledProperty]
    public partial IEnumerable? Statuses { get; set; }

    [GeneratedStyledProperty]
    public partial IDataTemplate? StatusTemplate { get; set; }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == FaceKeyProperty ||
            e.Property == IsSelectedProperty ||
            e.Property == IsHighlightedProperty ||
            e.Property == IsDeadProperty ||
            e.Property == HighLevelRankProperty ||
            e.Property == MarkerProperty)
        {
            UpdatePseudoClasses();
        }
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(DeadPseudoClass, IsDead);
        PseudoClasses.Set(FreePseudoClass, FaceKey is null);
        PseudoClasses.Set(HighLevel1PseudoClass, HighLevelRank == 1);
        PseudoClasses.Set(HighLevel2PseudoClass, HighLevelRank == 2);
        PseudoClasses.Set(HighLevel3PseudoClass, HighLevelRank == 3);
        PseudoClasses.Set(HighlightedPseudoClass, IsHighlighted);
        PseudoClasses.Set(MarkedPseudoClass, Marker is not null);
        PseudoClasses.Set(SelectedPseudoClass, IsSelected);
    }
}
