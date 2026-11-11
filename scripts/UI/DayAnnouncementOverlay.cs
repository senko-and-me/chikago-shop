using Godot;

public partial class DayAnnouncementOverlay : CanvasLayer
{
    private const double FadeIn = 0.45, Hold = 0.8, FadeOut = 3.75;
    private Control _root = null!;
    private Label _title = null!;
    private bool _openingPending = true, _midpointShown;
    private double _elapsed;
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

    private void UpdateFont() => _title.AddThemeFontSizeOverride("font_size",
        (int)Mathf.Clamp(GetViewport().GetVisibleRect().Size.X * 0.065f, 28, 96));

    public void BeginDay()
    {
        Dismiss();
        _openingPending = true;
        _midpointShown = false;
    }

    public void Restore(double remaining, double duration)
    {
        Dismiss();
        _openingPending = false;
        _midpointShown = remaining <= duration / 2;
    }

    public void Observe(double remaining, double duration, bool closed)
    {
        if (closed) { Dismiss(); return; }
        if (_openingPending)
        {
            _openingPending = false;
            ShowMessage("Магазин открыт");
        }
        if (!_midpointShown && remaining <= duration / 2)
        {
            _midpointShown = true;
            ShowMessage("Середина дня");
        }
    }

    private void ShowMessage(string message)
    {
        _title.Text = message; // Godot translates on display and on locale changes.
        _elapsed = 0;
        _root.Modulate = new Color(1, 1, 1, 0);
        _root.Show();
        SetProcess(true);
    }

    public void Dismiss()
    {
        _root.Hide();
        SetProcess(false);
    }

    public override void _Process(double delta)
    {
        _elapsed += delta;
        if (_elapsed >= FadeIn + Hold + FadeOut) { Dismiss(); return; }
        float alpha = _elapsed < FadeIn ? (float)(_elapsed / FadeIn)
            : _elapsed < FadeIn + Hold ? 1 : 1 - (float)((_elapsed - FadeIn - Hold) / FadeOut);
        alpha = Mathf.SmoothStep(0, 1, alpha);
        _root.Modulate = new Color(1, 1, 1, alpha);
    }
}
