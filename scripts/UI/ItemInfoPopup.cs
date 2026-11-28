using Godot;

// Owns item tooltip presentation and lifetime; the display owns its stock.
public partial class ItemInfoPopup : PanelContainer
{
    private RichTextLabel _label = null!;
    private Control _slot;
    private System.Func<string> _describe;
    internal FirstTradeTutorial Tutorial { get; set; }
    private CanvasLayer _layer;
    private int _normalLayer;
    public string ItemText => _label.GetParsedText();

    public override void _Ready()
    {
        _layer = GetParent<CanvasLayer>();
        _normalLayer = _layer.Layer;
        MouseFilter = MouseFilterEnum.Ignore;
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.10353f, 0.10353f, 0.10353f, 0.94f),
            ContentMarginLeft = 12, ContentMarginRight = 12,
            ContentMarginTop = 8, ContentMarginBottom = 8
        };
        AddThemeStyleboxOverride("panel", style);
        _label = new RichTextLabel { MouseFilter = MouseFilterEnum.Ignore, BbcodeEnabled = true, FitContent = true,
            ScrollActive = false, AutowrapMode = TextServer.AutowrapMode.Off };
        _label.AddThemeFontSizeOverride("normal_font_size", 18);
        _label.AddThemeColorOverride("default_color", Colors.White);
        AddChild(_label);
        HideInfo();
    }

    public void ShowFor(SlotUI slot) => ShowFor(slot, () => slot.ItemInfo);

    public void ShowFor(Control slot, System.Func<string> describe)
    {
        if (GetTree().Paused || GetViewport().GuiIsDragging() || slot.IsQueuedForDeletion()) return;
        _slot = slot;
        _describe = describe;
        Refresh();
        SetProcess(true);
    }

    public void HideFor(Control slot)
    {
        if (_slot == slot) HideInfo();
    }

    public void HideInfo()
    {
        _slot = null;
        _describe = null;
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
        _layer.Layer = Tutorial?.ContextTooltipLayer(_normalLayer, this) ?? _normalLayer;
        string text = _describe?.Invoke();
        if (text == null) { HideInfo(); return; }
        if (_label.Text != text)
        {
            _label.Text = text;
            var font = _label.GetThemeFont("normal_font");
            float width = 0;
            foreach (string line in _label.GetParsedText().Split('\n'))
                width = Mathf.Max(width, font.GetStringSize(line, fontSize: 18).X);
            _label.CustomMinimumSize = new Vector2(Mathf.Ceil(width) + 2, 0);
        }
        Vector2 minimum = GetCombinedMinimumSize();
        if (Size != minimum) Size = minimum;
        Rect2 anchor = _slot.GetGlobalTransformWithCanvas() * new Rect2(Vector2.Zero, _slot.Size);
        Vector2 position = new(anchor.GetCenter().X - Size.X / 2, anchor.Position.Y - Size.Y - 8);
        position.X = Mathf.Clamp(position.X, 8, Mathf.Max(8, GetViewportRect().Size.X - Size.X - 8));
        if (position.Y < 8) position.Y = Mathf.Min(anchor.End.Y + 8, GetViewportRect().End.Y - Size.Y - 8);
        if (Position != position) Position = position;
        if (!Visible) Show();
    }
}
