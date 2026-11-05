using Godot;

public partial class ProgressPanel : PanelContainer
{
    private Button _toggle = null!;
    private bool _resizeQueued;

    public override void _Ready()
    {
        _toggle = GetNode<Button>("../ProgressToggleButton");
        _toggle.Pressed += Toggle;
        GetNode<Control>("Content").MinimumSizeChanged += QueueResize;
        Resized += PositionToggle;
        SetExpanded(true);
        QueueResize();
    }

    private void Toggle() => SetExpanded(!Visible);

    private void SetExpanded(bool expanded)
    {
        Visible = expanded;
        _toggle.Text = expanded ? "<" : ">";
        _toggle.TooltipText = expanded ? "Свернуть панель" : "Показать панель";
        PositionToggle();
    }

    private void PositionToggle()
    {
        if (_toggle == null) return;
        _toggle.Position = Position + new Vector2(Visible ? Size.X + 6 : 0, 0);
    }

    private void QueueResize()
    {
        if (_resizeQueued) return;
        _resizeQueued = true;
        Callable.From(() =>
        {
            _resizeQueued = false;
            // Keep the width, but let wrapped status text determine the required height.
            Size = new Vector2(Size.X, GetCombinedMinimumSize().Y);
            PositionToggle();
        }).CallDeferred();
    }
}
