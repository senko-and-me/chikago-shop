using Godot;

// Owns item tooltip presentation and lifetime; inventory owns selection and transfers.
public partial class ItemInfoPopup : PanelContainer
{
    private Label _label = null!;
    private SlotUI _slot;
    public string ItemText => _label.Text;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.12f, 0.1f, 0.09f, 0.94f),
            ContentMarginLeft = 12, ContentMarginRight = 12,
            ContentMarginTop = 8, ContentMarginBottom = 8
        };
        AddThemeStyleboxOverride("panel", style);
        _label = new Label { MouseFilter = MouseFilterEnum.Ignore };
        _label.AddThemeFontSizeOverride("font_size", 18);
        _label.AddThemeColorOverride("font_color", Colors.White);
        AddChild(_label);
        HideInfo();
    }

    public void ShowFor(SlotUI slot)
    {
        if (GetTree().Paused || GetViewport().GuiIsDragging() || slot.IsQueuedForDeletion()) return;
        _slot = slot;
        Refresh();
        SetProcess(true);
    }

    public void HideFor(SlotUI slot)
    {
        if (_slot == slot) HideInfo();
    }

    public void HideInfo()
    {
        _slot = null;
        Hide();
        SetProcess(false);
    }

    public override void _Notification(int what)
    {
        if ((what == NotificationPaused || what == NotificationDragBegin) && _label != null)
            HideInfo();
    }

    public override void _Process(double delta)
    {
        if (_slot == null || !IsInstanceValid(_slot) || _slot.IsQueuedForDeletion() ||
            !_slot.IsVisibleInTree() || GetViewport().GuiGetHoveredControl() != _slot)
            HideInfo();
        else Refresh();
    }

    public void Refresh()
    {
        if (_slot == null) return;
        if (_label.Text != _slot.ItemInfo) _label.Text = _slot.ItemInfo;
        Vector2 minimum = GetCombinedMinimumSize();
        if (Size != minimum) Size = minimum;
        Rect2 anchor = _slot.GetGlobalTransformWithCanvas() * new Rect2(Vector2.Zero, _slot.Size);
        Vector2 position = new(anchor.GetCenter().X - Size.X / 2, anchor.Position.Y - Size.Y - 8);
        position.X = Mathf.Clamp(position.X, 8, Mathf.Max(8, GetViewportRect().Size.X - Size.X - 8));
        if (Position != position) Position = position;
        if (!Visible) Show();
    }
}
