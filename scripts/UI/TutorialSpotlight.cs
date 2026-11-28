using System;
using System.Collections.Generic;
using Godot;

public partial class TutorialSpotlight : ColorRect
{
    private const float TransitionSeconds = 0.22f;
    private const float Darkness = 0.50f;
    private Control _target;
    private Func<Rect2> _targetBounds;
    private Control _panel, _sale;
    private bool _targetOutline = true;
    private float _accessoryGlow;
    private readonly HashSet<Control> _tooltips = new();
    private readonly Vector4[] _accessories = new Vector4[6];
    private ShaderMaterial _shader;
    private Rect2 _focus;
    private Rect2 _from;
    private float _elapsed = TransitionSeconds;
    private float _opacity;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ProcessMode = ProcessModeEnum.Always;
        ProcessPriority = 20;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _shader = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/tutorial_spotlight.gdshader") };
        Material = _shader;
        Hide();
    }

    public void SetTarget(Control target, Func<Rect2> bounds = null, bool outline = true)
    {
        if (target != null) _targetOutline = outline;
        if (_target == target && _targetBounds == bounds) return;
        _target = target;
        _targetBounds = bounds;
        _from = _focus;
        _elapsed = _opacity > 0 ? 0 : TransitionSeconds;
    }

    public static Rect2 TargetBounds(Control target) =>
        target.GetGlobalTransformWithCanvas() * new Rect2(Vector2.Zero, target.Size);

    public void SetAccessories(Control panel, Control sale)
    {
        _panel = panel;
        _sale = sale;
    }

    public void TrackTooltip(Control tooltip) => _tooltips.Add(tooltip);

    private void AddAccessory(Control control, ref int count)
    {
        if (count == _accessories.Length || !IsInstanceValid(control) || !control.IsVisibleInTree()) return;
        Rect2 bounds = TargetBounds(control);
        if (!bounds.Intersects(GetViewportRect())) return;
        Vector2 center = bounds.GetCenter();
        Vector2 half = bounds.Size / 2;
        _accessories[count++] = new Vector4(center.X, center.Y, half.X, half.Y);
    }

    public override void _Process(double delta)
    {
        Rect2 screen = GetViewportRect();
        Rect2 target = IsInstanceValid(_target) && _target.IsVisibleInTree()
            ? (_targetBounds?.Invoke() ?? TargetBounds(_target)).Grow(8) : default;
        bool shown = target.Intersects(screen);
        _opacity = Mathf.MoveToward(_opacity, shown ? Darkness : 0, (float)delta * Darkness / TransitionSeconds);
        int count = 0;
        AddAccessory(_panel, ref count);
        AddAccessory(_sale, ref count);
        if (IsInstanceValid(_panel) && _panel.IsVisibleInTree())
            foreach (var tooltip in _tooltips)
                AddAccessory(tooltip, ref count);
        _accessoryGlow = Mathf.MoveToward(_accessoryGlow, count > 0 ? Darkness : 0,
            (float)delta * Darkness / TransitionSeconds);
        Visible = _opacity > 0 || _accessoryGlow > 0;
        if (!Visible) return;

        if (shown)
        {
            _elapsed = Math.Min(_elapsed + (float)delta, TransitionSeconds);
            float blend = Mathf.SmoothStep(0, 1, _elapsed / TransitionSeconds);
            _focus = new Rect2(_from.Position.Lerp(target.Position, blend), _from.Size.Lerp(target.Size, blend));
        }
        _shader.SetShaderParameter("screen_size", Size);
        _shader.SetShaderParameter("focus_center", _focus.GetCenter());
        _shader.SetShaderParameter("focus_half_size", _focus.Size / 2);
        _shader.SetShaderParameter("darkness", _opacity);
        _shader.SetShaderParameter("focus_outline", _targetOutline);
        _shader.SetShaderParameter("accessory_glow", _accessoryGlow);
        if (count > 0)
            _shader.SetShaderParameter("accessories", _accessories);
        _shader.SetShaderParameter("accessory_count", count);
    }
}
