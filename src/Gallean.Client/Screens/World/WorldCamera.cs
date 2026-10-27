using Avalonia;

namespace Gallean.Client;

internal sealed class WorldCamera
{
    private readonly record struct State(
        Point Center,
        Point Target,
        double Zoom,
        double TargetZoom,
        double SmoothTime,
        Vector PanVelocity,
        TimeSpan TimeToTarget,
        Point? ZoomAnchor,
        Size ViewportSize,
        double UiScale,
        int MapWidth,
        int MapHeight);

    private const double DefaultZoom = 1.0;
    private const double MinimumZoom = 0.75;
    private const double MaximumZoom = 1.25;
    private const double PanSmoothTime = 0.0625;
    private const double FocusSmoothTime = 0.22;
    private const double ZoomSmoothing = 20.0;
    private const double SettleThreshold = 0.1;
    private const double SettleSpeedThreshold = 1.0;
    private const double ZoomSettleThreshold = 0.0005;

    private State currentState = new(
        Center: default,
        Target: default,
        Zoom: DefaultZoom,
        TargetZoom: DefaultZoom,
        SmoothTime: PanSmoothTime,
        PanVelocity: default,
        TimeToTarget: TimeSpan.Zero,
        ZoomAnchor: null,
        ViewportSize: default,
        UiScale: 1.0,
        MapWidth: 0,
        MapHeight: 0);

    public Point Center => currentState.Center;
    public Point Target => currentState.Target;
    public double Zoom => currentState.Zoom;
    public double TargetZoom => currentState.TargetZoom;

    public Size ViewportSize
    {
        get => currentState.ViewportSize;
        set => SetState(currentState with { ViewportSize = value });
    }

    public double UiScale
    {
        get => currentState.UiScale;
        set => SetState(currentState with { UiScale = value });
    }

    public int MapWidth
    {
        get => currentState.MapWidth;
        set => SetState(currentState with { MapWidth = value });
    }

    public int MapHeight
    {
        get => currentState.MapHeight;
        set => SetState(currentState with { MapHeight = value });
    }

    public event Action? Changed;

    public bool LookAt(Point point)
    {
        Point center = EnsureWorldBounds(currentState, point);

        return SetState(currentState with
        {
            Center = center,
            Target = center,
            PanVelocity = default,
            TimeToTarget = TimeSpan.Zero
        });
    }

    public bool PanTo(Point point)
    {
        return SetState(currentState with
        {
            Target = EnsureWorldBounds(currentState, point),
            SmoothTime = PanSmoothTime,
            TimeToTarget = TimeSpan.Zero
        });
    }

    public bool FocusOn(Point point, TimeSpan duration)
    {
        return SetState(currentState with
        {
            Target = EnsureWorldBounds(currentState, point),
            SmoothTime = FocusSmoothTime,
            TimeToTarget = duration
        });
    }

    public bool Move(Vector delta)
    {
        return SetState(Translate(currentState, delta));
    }

    public bool PanBy(Vector delta)
    {
        return SetState(currentState with
        {
            Target = EnsureWorldBounds(currentState, currentState.Target + delta),
            SmoothTime = PanSmoothTime,
            TimeToTarget = TimeSpan.Zero
        });
    }

    public bool ZoomTo(double zoom, Point anchor)
    {
        return SetState(currentState with
        {
            TargetZoom = Math.Clamp(zoom, MinimumZoom, MaximumZoom),
            ZoomAnchor = anchor
        });
    }

    public bool SetZoom(double zoom)
    {
        zoom = Math.Clamp(zoom, MinimumZoom, MaximumZoom);

        return SetState(currentState with
        {
            Zoom = zoom,
            TargetZoom = zoom,
            ZoomAnchor = null
        });
    }

    public bool Animate(TimeSpan delta)
    {
        State zoomed = AnimateZoom(currentState, delta);
        State panned = AnimatePan(zoomed, delta);

        return SetState(panned);
    }

    public Matrix GetViewMatrix()
    {
        return GetViewMatrix(currentState);
    }

    public Point ScreenToWorld(Point screen)
    {
        return ScreenToWorld(currentState, screen);
    }

    private bool SetState(State newState)
    {
        if (newState != currentState)
        {
            currentState = newState;
            Changed?.Invoke();
            return true;
        }
        else
        {
            return false;
        }
    }

    private static State Translate(State state, Vector delta)
    {
        return state with
        {
            Center = EnsureWorldBounds(state, state.Center + delta),
            Target = EnsureWorldBounds(state, state.Target + delta)
        };
    }

    private static State AnimatePan(State state, TimeSpan delta)
    {
        if (state.TimeToTarget > TimeSpan.Zero)
        {
            return AnimateTimed(state, delta);
        }
        else
        {
            return AnimateSmooth(state, delta);
        }
    }

    private static State AnimateTimed(State state, TimeSpan delta)
    {
        double progress = Math.Min(delta / state.TimeToTarget, 1);

        return state with
        {
            Center = EnsureWorldBounds(state, state.Center + ((state.Target - state.Center) * progress)),
            TimeToTarget = state.TimeToTarget - delta,
            PanVelocity = default
        };
    }

    private static State AnimateSmooth(State state, TimeSpan delta)
    {
        Vector remaining = state.Target - state.Center;

        bool moving = Math.Abs(remaining.X) >= SettleThreshold ||
                      Math.Abs(remaining.Y) >= SettleThreshold ||
                      Math.Abs(state.PanVelocity.X) >= SettleSpeedThreshold ||
                      Math.Abs(state.PanVelocity.Y) >= SettleSpeedThreshold;

        if (moving)
        {
            double frequency = 2.0 / state.SmoothTime;
            double decay = Math.Exp(-frequency * delta.TotalSeconds);
            Vector offset = state.Center - state.Target;
            Vector damping = (state.PanVelocity + (frequency * offset)) * delta.TotalSeconds;

            return state with
            {
                PanVelocity = (state.PanVelocity - (frequency * damping)) * decay,
                Center = EnsureWorldBounds(state, state.Target + ((offset + damping) * decay))
            };
        }
        else
        {
            return state with
            {
                PanVelocity = default,
                Center = state.Target
            };
        }
    }

    private static State AnimateZoom(State state, TimeSpan delta)
    {
        double remaining = state.TargetZoom - state.Zoom;

        if (Math.Abs(remaining) >= ZoomSettleThreshold)
        {
            return ApplyZoom(state, state.Zoom + remaining * GetApproachFactor(ZoomSmoothing, delta));
        }
        else
        {
            return ApplyZoom(state, state.TargetZoom);
        }
    }

    private static State ApplyZoom(State state, double zoom)
    {
        if (state.ZoomAnchor is { } anchor)
        {
            Point anchorWorld = ScreenToWorld(state, anchor);
            State zoomed = state with { Zoom = zoom };

            return Translate(zoomed, anchorWorld - ScreenToWorld(zoomed, anchor));
        }
        else
        {
            return state with { Zoom = zoom };
        }
    }

    private static Point EnsureWorldBounds(State state, Point point)
    {
        return WorldTransform.ClampToMap(point, state.MapWidth, state.MapHeight);
    }

    private static Matrix GetViewMatrix(State state)
    {
        double scale = state.Zoom / state.UiScale;

        return Matrix.CreateTranslation(-state.Center.X, -state.Center.Y) *
               Matrix.CreateScale(scale, scale) *
               Matrix.CreateTranslation(state.ViewportSize.Width / 2.0, state.ViewportSize.Height / 2.0);
    }

    private static Point ScreenToWorld(State state, Point screen)
    {
        Matrix matrix = GetViewMatrix(state);

        if (matrix.TryInvert(out Matrix inverse))
        {
            return screen.Transform(inverse);
        }
        else
        {
            return screen;
        }
    }

    private static double GetApproachFactor(double smoothing, TimeSpan delta)
    {
        return 1 - Math.Exp(-smoothing * delta.TotalSeconds);
    }
}
