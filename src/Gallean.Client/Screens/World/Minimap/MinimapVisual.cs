using Avalonia;
using Avalonia.Input;
using Avalonia.Media;

namespace Gallean.Client;

internal sealed class MinimapVisual : AbstractAnimatedVisual
{
    public static readonly StyledProperty<double> MapScaleProperty =
        AvaloniaProperty.Register<MinimapVisual, double>(nameof(MapScale), 1.0);

    private readonly MinimapRenderer renderer = new();
    private WorldCamera? camera;
    private bool dragging;
    private Point lastCameraCenter;
    private double lastCameraZoom = double.NaN;
    private WorldScreenSnapshot? lastSnapshot;
    private bool redrawRequested = true;
    private WorldScreenSnapshotWorker snapshotWorker = null!;

    public MinimapVisual()
    {
        ClipToBounds = true;
        Focusable = false;
    }

    static MinimapVisual()
    {
        AffectsRender<MinimapVisual>(MapScaleProperty);
    }

    public double MapScale
    {
        get => GetValue(MapScaleProperty);
        set => SetValue(MapScaleProperty, value);
    }

    public void Attach(WorldCamera worldCamera, WorldScreenSnapshotWorker worker)
    {
        camera = worldCamera;
        snapshotWorker = worker;
        redrawRequested = true;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        PointerPoint point = e.GetCurrentPoint(this);

        if (point.Properties.IsLeftButtonPressed)
        {
            dragging = true;
            e.Pointer.Capture(this);
            e.Handled = true;

            RecenterFrom(e.GetPosition(this));
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (dragging)
        {
            RecenterFrom(e.GetPosition(this));
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (dragging)
        {
            dragging = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    protected override void OnFrame(TimeSpan frameTime)
    {
        bool redraw = false;

        if (redrawRequested)
        {
            redraw = true;
        }

        if (camera is not null && (camera.Center != lastCameraCenter || camera.Zoom != lastCameraZoom))
        {
            redraw = true;
        }

        if (!ReferenceEquals(snapshotWorker.Current, lastSnapshot))
        {
            redraw = true;
        }

        if (redraw)
        {
            redrawRequested = false;

            if (camera is not null)
            {
                lastCameraCenter = camera.Center;
                lastCameraZoom = camera.Zoom;
            }

            lastSnapshot = snapshotWorker.Current;
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        WorldScreenSnapshot? snapshot = snapshotWorker.Current;

        if (snapshot is null)
        {
            return;
        }

        MinimapLayout? layout = ComputeLayout(snapshot.WorldState.Terrain.Map);

        if (layout is null)
        {
            return;
        }

        IReadOnlyList<Point> viewportCells = ComputeViewportCells();

        context.Custom(new MinimapRenderOperation(new Rect(Bounds.Size), snapshot.WorldContent, snapshot.WorldState.Minimap, layout.Value, viewportCells, renderer));
    }

    private MinimapLayout? ComputeLayout(WorldMap map)
    {
        double width = Math.Min(Bounds.Width, Bounds.Height * 2) * MapScale;
        double height = width / 2.0;

        if (width > 0 && height > 0)
        {
            Rect box = new((Bounds.Width - width) / 2.0, (Bounds.Height - height) / 2.0, width, height);

            return MinimapLayout.Create(map.Width, map.Height, box);
        }
        else
        {
            return null;
        }
    }

    private IReadOnlyList<Point> ComputeViewportCells()
    {
        if (camera is null)
        {
            return [];
        }

        Size viewport = camera.ViewportSize;

        if (viewport.Width <= 0 || viewport.Height <= 0)
        {
            return [];
        }

        Point[] corners =
        [
            new(0, 0),
            new(viewport.Width, 0),
            new(viewport.Width, viewport.Height),
            new(0, viewport.Height)
        ];

        List<Point> cells = new(corners.Length);

        foreach (Point corner in corners)
        {
            Point world = camera.ScreenToWorld(corner);
            cells.Add(WorldTransform.ToFractionalCell(world));
        }

        return cells;
    }

    private void RecenterFrom(Point point)
    {
        WorldScreenSnapshot? snapshot = snapshotWorker.Current;

        if (camera is null || snapshot is null)
        {
            return;
        }

        MinimapLayout? layout = ComputeLayout(snapshot.WorldState.Terrain.Map);

        if (layout is null)
        {
            return;
        }

        WorldMap map = snapshot.WorldState.Terrain.Map;
        WorldPosition cell = layout.Value.ToCell(point);

        int clampedX = Math.Clamp(cell.X, 0, Math.Max(0, map.Width - 1));
        int clampedY = Math.Clamp(cell.Y, 0, Math.Max(0, map.Height - 1));

        camera.PanTo(WorldTransform.ToWorldPoint(new WorldPosition(clampedX, clampedY)));
    }
}
