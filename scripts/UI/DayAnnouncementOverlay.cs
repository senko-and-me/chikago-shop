using Godot;

public partial class DayAnnouncementOverlay : CanvasLayer
{
    private const double FadeIn = 0.45, Hold = 0.8, FadeOut = 3.75;
    private Control _root = null!;
    private Label _title = null!;
    private double _elapsed;
    private System.Action _afterReward;
    public bool IsShowing => _root.Visible;
    public float Opacity => _root.Modulate.A;

    public override void _Ready()
    {
        Name = "DayAnnouncements";
        Layer = 90; // Below pause, crafting and day-choice windows.
        ProcessMode = ProcessModeEnum.Pausable;
        _root = new Control { Name = "Root", MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
        AddChild(_root);
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var shade = new ColorRect
        {
            Name = "Shade", Color = new Color(0, 0, 0, 0.28f),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _root.AddChild(shade);
        shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _title = new Label
        {
            Name = "Title", HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center, MouseFilter = Control.MouseFilterEnum.Ignore,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _title.AddThemeFontOverride("font", GD.Load<FontFile>("res://art/fonts/game.ttf"));
        _title.AddThemeColorOverride("font_color", Colors.White);
        _title.AddThemeColorOverride("font_outline_color", Colors.Black);
        _title.AddThemeConstantOverride("outline_size", 6);
        _root.AddChild(_title);
        _title.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _title.OffsetLeft = 32;
        _title.OffsetRight = -32;
        _root.Resized += UpdateFont;
        UpdateFont();
        SetProcess(false);
    }

    private void UpdateFont()
    {
        float width = GetViewport().GetVisibleRect().Size.X;
        int titleSize = (int)Mathf.Clamp(width * 0.065f, 28, 96);
        _title.AddThemeFontSizeOverride("font_size", titleSize);
        float titleHeight = titleSize * 1.4f;
        _title.AnchorTop = _title.AnchorBottom = 0.6f;
        _title.OffsetTop = -titleHeight / 2;
        _title.OffsetBottom = titleHeight / 2;
    }

    public void ShowReward(string message, System.Action after)
    {
        _afterReward = after;
        _title.Text = message;
        _elapsed = 0;
        _root.Modulate = new Color(1, 1, 1, 0);
        _root.Show();
        SetProcess(true);
    }

    public void Dismiss()
    {
        _afterReward = null;
        _root.Hide();
        SetProcess(false);
    }

    public override void _Process(double delta)
    {
        _elapsed += delta;
        if (_elapsed >= FadeIn + Hold + FadeOut)
        {
            var after = _afterReward;
            Dismiss();
            after?.Invoke();
            return;
        }
        float alpha = _elapsed < FadeIn ? (float)(_elapsed / FadeIn)
            : _elapsed < FadeIn + Hold ? 1 : 1 - (float)((_elapsed - FadeIn - Hold) / FadeOut);
        alpha = Mathf.SmoothStep(0, 1, alpha);
        _root.Modulate = new Color(1, 1, 1, alpha);
    }
}
