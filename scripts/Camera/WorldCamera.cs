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
        _targetZoom = Mathf.Clamp(Zoom.X, MinZoom, ZoomLimit);
        Zoom = Vector2.One * _targetZoom;
        _zoomAnchor = GetViewportRect().Size / 2f;
        _viewport.SizeChanged += ClampToBounds;
        ClampToBounds();
    }

    public override void _ExitTree()
    {
        if (_viewport != null) _viewport.SizeChanged -= ClampToBounds;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationPaused) CancelDrag();
    }

    public void CancelDrag() => _dragging = false;

    public override void _Process(double delta)
    {
        if (GetTree().Paused) return;
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
        if (GetTree().Paused) return;
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
            Position -= motion.Relative / Zoom;
            ClampToBounds();
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (GetTree().Paused) return;
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
            _dragging = true;
    }

    private void ZoomAt(Vector2 mousePosition, float factor)
    {
        float newTarget = Mathf.Clamp(_targetZoom * factor, MinZoom, ZoomLimit);
        if (Mathf.IsEqualApprox(newTarget, _targetZoom)) return;
        _targetZoom = newTarget;
        _zoomAnchor = mousePosition;
    }

    public void ClampToBounds()
    {
        Rect2 bounds = Bounds;
        float zoom = Mathf.Clamp(Zoom.X, MinZoom, ZoomLimit);
        Zoom = Vector2.One * zoom;
        Vector2 halfView = GetViewportRect().Size / (2f * Zoom);
        Vector2 position = Position;
        position.X = halfView.X >= bounds.Size.X / 2f
            ? bounds.GetCenter().X
            : Mathf.Clamp(position.X, bounds.Position.X + halfView.X, bounds.End.X - halfView.X);
        position.Y = halfView.Y >= bounds.Size.Y / 2f
            ? bounds.GetCenter().Y
            : Mathf.Clamp(position.Y, bounds.Position.Y + halfView.Y, bounds.End.Y - halfView.Y);
        Position = position;
    }
}
