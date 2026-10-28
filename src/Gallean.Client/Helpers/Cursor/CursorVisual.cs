using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Gallean.Client;

internal sealed class CursorVisual : Control
{
    public static readonly StyledProperty<AssetAnimationKey.CursorType> CursorTypeProperty =
        AvaloniaProperty.Register<CursorVisual, AssetAnimationKey.CursorType>(nameof(CursorType));

    private readonly Dictionary<AssetAnimationKey.CursorType, CursorAnimation> cache = [];
    private readonly Cursor noneCursor = new(StandardCursorType.None);
    private CursorAnimation? animation;
    private int frameIndex;
    private bool frameRequested;
    private AssetAnimationKey.CursorType? hoverCursorType;
    private TimeSpan? startTime;
    private TopLevel? topLevel;

    public AssetAnimationKey.CursorType CursorType
    {
        get => GetValue(CursorTypeProperty);
        set => SetValue(CursorTypeProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == CursorTypeProperty)
        {
            Refresh();
        }
        else if (e.Property == AssetScope.AssetsProperty ||
                 e.Property == UiScaleDecorator.RenderScalingProperty)
        {
            Reset();
            Refresh();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        topLevel = TopLevel.GetTopLevel(this);

        if (topLevel is not null)
        {
            topLevel.ScalingChanged += OnScalingChanged;
            topLevel.PropertyChanged += OnTopLevelPropertyChanged;
            topLevel.AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel, true);
            topLevel.AddHandler(PointerExitedEvent, OnPointerExited, RoutingStrategies.Direct, true);
        }

        Refresh();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        Reset();

        if (topLevel is not null)
        {
            topLevel.SetCursorOverride(null);
            topLevel.ScalingChanged -= OnScalingChanged;
            topLevel.PropertyChanged -= OnTopLevelPropertyChanged;
            topLevel.RemoveHandler(PointerMovedEvent, OnPointerMoved);
            topLevel.RemoveHandler(PointerExitedEvent, OnPointerExited);
            topLevel = null;
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (e.Source is Control hovered)
        {
            UpdateHoverCursorType(CursorScope.GetCursorType(hovered));
        }
        else
        {
            UpdateHoverCursorType(null);
        }
    }

    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        UpdateHoverCursorType(null);
    }

    private void UpdateHoverCursorType(AssetAnimationKey.CursorType? value)
    {
        if (value != hoverCursorType)
        {
            hoverCursorType = value;
            Refresh();
        }
    }

    private void OnScalingChanged(object? sender, EventArgs e)
    {
        Reset();
        Refresh();
    }

    private void OnTopLevelPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == CursorScope.DragImageProperty)
        {
            Reset();
            Refresh();
        }
    }

    private void Reset()
    {
        topLevel?.SetCursorOverride(noneCursor);

        foreach (CursorAnimation cached in cache.Values)
        {
            cached.Dispose();
        }

        cache.Clear();
        animation = null;
    }

    private void Refresh()
    {
        IAssetProvider? assets = AssetScope.GetOptionalAssets(this);

        if (topLevel is null)
        {
            return;
        }

        if (assets is null)
        {
            topLevel.SetCursorOverride(noneCursor);
            return;
        }

        AssetAnimationKey.CursorType type = hoverCursorType ?? CursorType;

        if (!cache.TryGetValue(type, out CursorAnimation? current))
        {
            AssetAnimationKey.Cursor key = new(type);

            using AssetCache<Animation>.Lease arrow = assets.GetRequiredAnimation(key.Arrow);
            using AssetCache<Animation>.Lease? overlay = key.Overlay is null ? null : assets.GetRequiredAnimation(key.Overlay);

            CursorDragImage? dragImage = CursorScope.GetDragImage(topLevel);

            current = CursorAnimation.Create(arrow.Value, overlay?.Value, dragImage, UiScaleDecorator.GetRenderScaling(this));

            cache.Add(type, current);
        }

        animation = current;
        frameIndex = 0;
        startTime = null;
        topLevel.SetCursorOverride(current[0]);

        RequestFrame();
    }

    private void RequestFrame()
    {
        if (!frameRequested && topLevel is not null && animation is { FrameCount: > 1 })
        {
            frameRequested = true;
            topLevel.RequestAnimationFrame(OnAnimationFrame);
        }
    }

    private void OnAnimationFrame(TimeSpan frameTime)
    {
        frameRequested = false;

        if (topLevel is null || animation is null)
        {
            return;
        }

        startTime ??= frameTime;

        int index = AnimationRate.Sprites.GetFrameIndex(frameTime - startTime.Value) % animation.FrameCount;

        if (index != frameIndex)
        {
            frameIndex = index;
            topLevel.SetCursorOverride(animation[index]);
        }

        RequestFrame();
    }
}
