using System;
using Godot;

// Owns navigation only. The scene supplies world bounds through an explicit reference.
public partial class WorldCamera : Camera2D
{
    [Export] public Sprite2D BoundsSource { get; set; } = null!;

    // Optional limits measured in pixels of the design reference, independent of world scale.
    [Export] public Vector2 ReferenceSize { get; set; }
    [Export] public Rect2 NavigationRegion { get; set; }
    [Export] public Vector2 OverviewSize { get; set; }
    [Export] public Vector2 CloseupSize { get; set; }

    private const float MaxZoom = 1.8f;
    private const float ZoomStep = 1.12f;
    private const float ZoomSmoothness = 12f;
    private Viewport _viewport = null!;
    private bool _dragging;
    private float _targetZoom;
    private Vector2 _zoomAnchor;
    private Tween _overviewTween;
    private Tween _focusTween;
    public event Action<Vector2> UserPanned;
    public event Action UserZoomed;
    public event Action OverviewReset;
    public bool IsFocusing => _focusTween?.IsRunning() == true;
    public bool IsReturningToOverview => _overviewTween?.IsRunning() == true;
    private Rect2 WorldBounds => BoundsSource.GlobalTransform * BoundsSource.GetRect();
    private bool HasReference => ReferenceSize.X > 0 && ReferenceSize.Y > 0;
    private Vector2 ReferenceToWorld(Vector2 size) => size / ReferenceSize * WorldBounds.Size;
    private Rect2 Bounds => HasReference && NavigationRegion.HasArea()
        ? new Rect2(WorldBounds.Position + ReferenceToWorld(NavigationRegion.Position), ReferenceToWorld(NavigationRegion.Size))
        : WorldBounds;
    private float MinZoom
    {
        get
        {
            Vector2 largest = HasReference && OverviewSize.X > 0 && OverviewSize.Y > 0
                ? ReferenceToWorld(OverviewSize) : Bounds.Size;
            Vector2 ratio = GetViewportRect().Size / largest.Min(Bounds.Size);
            return Mathf.Max(ratio.X, ratio.Y);
        }
    }
    private float ZoomLimit
    {
        get
        {
            if (!HasReference || CloseupSize.X <= 0 || CloseupSize.Y <= 0) return Mathf.Max(MaxZoom, MinZoom);
            Vector2 ratio = GetViewportRect().Size / ReferenceToWorld(CloseupSize);
            return Mathf.Max(MinZoom, Mathf.Min(ratio.X, ratio.Y));
        }
    }

    public override void _Ready()
    {
        _viewport = GetViewport();
        _viewport.SizeChanged += OnViewportSizeChanged;
        ResetToOverview();
    }

    public void ResetToOverview()
    {
        CancelFocusTransition();
        CancelOverviewTransition();
        CancelDrag();
        _targetZoom = MinZoom;
        Zoom = Vector2.One * _targetZoom;
        Position = WorldBounds.GetCenter();
        _zoomAnchor = GetViewportRect().Size / 2f;
        ClampToBounds();
        OverviewReset?.Invoke();
    }

    public void ReturnToOverview(double duration)
    {
        CancelFocusTransition();
        CancelOverviewTransition();
        CancelDrag();
        float zoom = MinZoom;
        Vector2 destination = ClampPosition(WorldBounds.GetCenter(), zoom);
        _overviewTween = CreateTween().SetPauseMode(Tween.TweenPauseMode.Process);
        _overviewTween.SetParallel().SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
        _overviewTween.TweenProperty(this, "position", destination, duration);
        _overviewTween.TweenProperty(this, "zoom", Vector2.One * zoom, duration);
        _overviewTween.Chain().TweenCallback(Callable.From(() =>
        {
            _overviewTween = null;
            _targetZoom = Zoom.X;
            _zoomAnchor = GetViewportRect().Size / 2f;
            ClampToBounds();
        }));
    }

    public void CancelOverviewTransition()
    {
        if (_overviewTween?.IsValid() == true) _overviewTween.Kill();
        _overviewTween = null;
        _targetZoom = Zoom.X;
    }

    public void FocusOn(Vector2 worldPosition, Vector2 referenceViewSize, double duration)
    {
        CancelOverviewTransition();
        CancelFocusTransition();
        CancelDrag();
        Vector2 view = HasReference ? ReferenceToWorld(referenceViewSize) : referenceViewSize;
        Vector2 ratio = GetViewportRect().Size / view;
        float destinationZoom = Mathf.Clamp(Mathf.Max(ratio.X, ratio.Y), MinZoom, ZoomLimit);
        Vector2 destination = ClampPosition(worldPosition, destinationZoom);
        Vector2 startPosition = Position;
        float startZoom = Zoom.X;
        _focusTween = CreateTween().SetPauseMode(Tween.TweenPauseMode.Stop)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
        _focusTween.TweenMethod(Callable.From<float>(blend =>
        {
            Zoom = Vector2.One * Mathf.Lerp(startZoom, destinationZoom, blend);
            Position = ClampPosition(startPosition.Lerp(destination, blend), Zoom.X);
        }), 0f, 1f, duration);
        _focusTween.TweenCallback(Callable.From(() =>
        {
            _focusTween = null;
            _targetZoom = Zoom.X;
            _zoomAnchor = GetViewportRect().Size / 2f;
            ClampToBounds();
        }));
    }

    public void CancelFocusTransition()
    {
        if (_focusTween?.IsValid() == true) _focusTween.Kill();
        _focusTween = null;
        _targetZoom = Zoom.X;
    }

    private void OnViewportSizeChanged()
    {
        CancelFocusTransition();
        ClampToBounds();
    }

    public override void _ExitTree()
    {
        CancelFocusTransition();
        CancelOverviewTransition();
        if (_viewport != null) _viewport.SizeChanged -= OnViewportSizeChanged;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationPaused) CancelDrag();
    }

    public void CancelDrag() => _dragging = false;

    public override void _Process(double delta)
    {
        if (GetTree().Paused || IsReturningToOverview || IsFocusing) return;
        ClampToBounds();
        _targetZoom = Mathf.Clamp(float.IsFinite(_targetZoom) ? _targetZoom : Zoom.X, MinZoom, ZoomLimit);
        float oldZoom = Zoom.X;
        if (Mathf.IsEqualApprox(oldZoom, _targetZoom)) return;
        float blend = 1f - MathF.Exp(-ZoomSmoothness * (float)delta);
        float newZoom = Mathf.Lerp(oldZoom, _targetZoom, blend);
        if (Mathf.Abs(newZoom - _targetZoom) < 0.0001f) newZoom = _targetZoom;
        Vector2 offset = _zoomAnchor - GetViewportRect().Size / 2f;
        Position += offset * (1f / oldZoom - 1f / newZoom);
        Zoom = Vector2.One * newZoom;
        ClampToBounds();
    }

    public override void _Input(InputEvent @event)
    {
        if (GetTree().Paused || IsReturningToOverview || IsFocusing) return;
        if (@event is InputEventMouseButton { Pressed: true } wheel)
        {
            switch (wheel.ButtonIndex)
            {
                case MouseButton.WheelUp: ZoomAt(wheel.Position, ZoomStep); break;
                case MouseButton.WheelDown: ZoomAt(wheel.Position, 1f / ZoomStep); break;
            }
        }
        else if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left })
            CancelDrag();
        else if (@event is InputEventMouseMotion motion && _dragging)
        {
            Vector2 before = Position;
            Position -= motion.Relative / Zoom;
            ClampToBounds();
            if (Position != before) UserPanned?.Invoke((Position - before) * Zoom);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (GetTree().Paused || IsReturningToOverview || IsFocusing) return;
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
            _dragging = true;
    }

    private void ZoomAt(Vector2 mousePosition, float factor)
    {
        float newTarget = Mathf.Clamp(_targetZoom * factor, MinZoom, ZoomLimit);
        if (Mathf.IsEqualApprox(newTarget, _targetZoom)) return;
        _targetZoom = newTarget;
        _zoomAnchor = mousePosition;
        UserZoomed?.Invoke();
    }

    public void ClampToBounds()
    {
        float zoom = Mathf.Clamp(Zoom.X, MinZoom, ZoomLimit);
        Zoom = Vector2.One * zoom;
        Position = ClampPosition(Position, zoom);
    }

    private Vector2 ClampPosition(Vector2 position, float zoom)
    {
        Rect2 bounds = Bounds;
        Vector2 halfView = GetViewportRect().Size / (2f * zoom);
        position.X = halfView.X >= bounds.Size.X / 2f
            ? bounds.GetCenter().X
            : Mathf.Clamp(position.X, bounds.Position.X + halfView.X, bounds.End.X - halfView.X);
        position.Y = halfView.Y >= bounds.Size.Y / 2f
            ? bounds.GetCenter().Y
            : Mathf.Clamp(position.Y, bounds.Position.Y + halfView.Y, bounds.End.Y - halfView.Y);
        return position;
    }
}
