using System;
using Godot;

// Owns navigation only. The scene supplies world bounds through an explicit reference.
public partial class WorldCamera : Camera2D
{
    [Export] public Sprite2D BoundsSource { get; set; } = null!;

    private const float MaxZoom = 1.8f;
    private const float ZoomStep = 1.12f;
    private const float ZoomSmoothness = 12f;
    private Viewport _viewport = null!;
    private bool _dragging;
    private float _targetZoom;
    private Vector2 _zoomAnchor;
    private Rect2 Bounds => BoundsSource.GlobalTransform * BoundsSource.GetRect();
    private float MinZoom => GetViewportRect().Size.X / Bounds.Size.X;
    private float ZoomLimit => Mathf.Max(MaxZoom, MinZoom);

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
