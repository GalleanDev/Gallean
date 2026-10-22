using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Templates;
using Avalonia.Media;

namespace Gallean.Client;

[PseudoClasses(DeadPseudoClass, FreePseudoClass, HighlightedPseudoClass, MarkedPseudoClass, SelectedPseudoClass)]
internal sealed class UnitPortrait : ContentControl
{
    private const string DeadPseudoClass = ":dead";
    private const string FreePseudoClass = ":free";
    private const string HighlightedPseudoClass = ":highlighted";
    private const string MarkedPseudoClass = ":marked";
    private const string SelectedPseudoClass = ":selected";

    public static readonly StyledProperty<AssetSpriteKey?> FaceKeyProperty =
        AvaloniaProperty.Register<UnitPortrait, AssetSpriteKey?>(nameof(FaceKey));

    public static readonly StyledProperty<double> HitpointsProperty =
        AvaloniaProperty.Register<UnitPortrait, double>(nameof(Hitpoints));

    public static readonly StyledProperty<double> MaxHitpointsProperty =
        AvaloniaProperty.Register<UnitPortrait, double>(nameof(MaxHitpoints));

    public static readonly StyledProperty<IReadOnlyList<AnimationProperty>> HitpointsAnimationProperty =
        AvaloniaProperty.Register<UnitPortrait, IReadOnlyList<AnimationProperty>>(nameof(HitpointsAnimation), []);

    public static readonly StyledProperty<bool> IsHitpointsVisibleProperty =
        AvaloniaProperty.Register<UnitPortrait, bool>(nameof(IsHitpointsVisible), true);

    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<UnitPortrait, bool>(nameof(IsSelected));

    public static readonly StyledProperty<bool> IsHighlightedProperty =
        AvaloniaProperty.Register<UnitPortrait, bool>(nameof(IsHighlighted));

    public static readonly StyledProperty<bool> IsDeadProperty =
        AvaloniaProperty.Register<UnitPortrait, bool>(nameof(IsDead));

    public static readonly StyledProperty<bool> IsPendingUpgradeProperty =
        AvaloniaProperty.Register<UnitPortrait, bool>(nameof(IsPendingUpgrade));

    public static readonly StyledProperty<IBrush?> MarkerProperty =
        AvaloniaProperty.Register<UnitPortrait, IBrush?>(nameof(Marker));

    public static readonly StyledProperty<IBrush?> OverlayProperty =
        AvaloniaProperty.Register<UnitPortrait, IBrush?>(nameof(Overlay));

    public static readonly StyledProperty<IEnumerable?> StatusesProperty =
        AvaloniaProperty.Register<UnitPortrait, IEnumerable?>(nameof(Statuses));

    public static readonly StyledProperty<IDataTemplate?> StatusTemplateProperty =
        AvaloniaProperty.Register<UnitPortrait, IDataTemplate?>(nameof(StatusTemplate));

    public UnitPortrait()
    {
        UpdatePseudoClasses();
    }

    public AssetSpriteKey? FaceKey
    {
        get => GetValue(FaceKeyProperty);
        set => SetValue(FaceKeyProperty, value);
    }

    public double Hitpoints
    {
        get => GetValue(HitpointsProperty);
        set => SetValue(HitpointsProperty, value);
    }

    public double MaxHitpoints
    {
        get => GetValue(MaxHitpointsProperty);
        set => SetValue(MaxHitpointsProperty, value);
    }

    public IReadOnlyList<AnimationProperty> HitpointsAnimation
    {
        get => GetValue(HitpointsAnimationProperty);
        set => SetValue(HitpointsAnimationProperty, value);
    }

    public bool IsHitpointsVisible
    {
        get => GetValue(IsHitpointsVisibleProperty);
        set => SetValue(IsHitpointsVisibleProperty, value);
    }

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public bool IsHighlighted
    {
        get => GetValue(IsHighlightedProperty);
        set => SetValue(IsHighlightedProperty, value);
    }

    public bool IsDead
    {
        get => GetValue(IsDeadProperty);
        set => SetValue(IsDeadProperty, value);
    }

    public bool IsPendingUpgrade
    {
        get => GetValue(IsPendingUpgradeProperty);
        set => SetValue(IsPendingUpgradeProperty, value);
    }

    public IBrush? Marker
    {
        get => GetValue(MarkerProperty);
        set => SetValue(MarkerProperty, value);
    }

    public IBrush? Overlay
    {
        get => GetValue(OverlayProperty);
        set => SetValue(OverlayProperty, value);
    }

    public IEnumerable? Statuses
    {
        get => GetValue(StatusesProperty);
        set => SetValue(StatusesProperty, value);
    }

    public IDataTemplate? StatusTemplate
    {
        get => GetValue(StatusTemplateProperty);
        set => SetValue(StatusTemplateProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == FaceKeyProperty ||
            e.Property == IsSelectedProperty ||
            e.Property == IsHighlightedProperty ||
            e.Property == IsDeadProperty ||
            e.Property == MarkerProperty)
        {
            UpdatePseudoClasses();
        }
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(DeadPseudoClass, IsDead);
        PseudoClasses.Set(FreePseudoClass, FaceKey is null);
        PseudoClasses.Set(HighlightedPseudoClass, IsHighlighted);
        PseudoClasses.Set(MarkedPseudoClass, Marker is not null);
        PseudoClasses.Set(SelectedPseudoClass, IsSelected);
    }
}
