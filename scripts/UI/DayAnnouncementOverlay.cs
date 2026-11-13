using Godot;

public partial class DayAnnouncementOverlay : CanvasLayer
{
    private const double FadeIn = 0.45, Hold = 0.8, FadeOut = 3.75;
    private Control _root = null!;
    private Label _title = null!;
    private Label _details = null!;
    private int _day, _totalDays;
    private bool _openingPending = true, _midpointShown, _closedShown;
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
        _details = new Label
        {
            Name = "Details", HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center, MouseFilter = Control.MouseFilterEnum.Ignore,
            AutowrapMode = TextServer.AutowrapMode.WordSmart, AutoTranslateMode = AutoTranslateModeEnum.Disabled,
            Visible = false
        };
        _details.AddThemeFontOverride("font", GD.Load<FontFile>("res://art/fonts/game.ttf"));
        _details.AddThemeColorOverride("font_color", Colors.White);
        _details.AddThemeColorOverride("font_outline_color", Colors.Black);
        _details.AddThemeConstantOverride("outline_size", 4);
        _root.AddChild(_details);
        _details.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _details.OffsetLeft = 32;
        _details.OffsetRight = -32;
        _root.Resized += UpdateFont;
        UpdateFont();
        SetProcess(false);
    }

    private void UpdateFont()
    {
        float width = GetViewport().GetVisibleRect().Size.X;
        int titleSize = (int)Mathf.Clamp(width * 0.065f, 28, 96);
        int detailSize = (int)Mathf.Clamp(width * 0.026f, 18, 36);
        _title.AddThemeFontSizeOverride("font_size", titleSize);
        _details.AddThemeFontSizeOverride("font_size", detailSize);
        float titleHeight = titleSize * 1.4f;
        float detailsHeight = _details.Visible ? detailSize * 2.8f + 12 : 0;
        float height = titleHeight + detailsHeight;
        _title.AnchorTop = _title.AnchorBottom = 0.6f;
        _title.OffsetTop = -height / 2;
        _title.OffsetBottom = -height / 2 + titleHeight;
        _details.AnchorTop = _details.AnchorBottom = 0.6f;
        _details.OffsetTop = -height / 2 + titleHeight + 12;
        _details.OffsetBottom = height / 2;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged && _details != null && _day > 0) RefreshDetails();
    }

    private void RefreshDetails()
    {
        int remaining = _totalDays - _day + 1;
        string key = remaining % 10 == 1 && remaining % 100 != 11 ? "Остался {0} день, включая сегодня"
            : remaining % 10 is >= 2 and <= 4 && remaining % 100 is not (>= 12 and <= 14)
                ? "Осталось {0} дня, включая сегодня" : "Осталось {0} дней, включая сегодня";
        _details.Text = GameLocalization.Format("День {0} из {1}", _day, _totalDays) + "\n" +
            GameLocalization.Format(key, remaining);
        UpdateFont();
    }

    public void BeginDay()
    {
        Dismiss();
        _openingPending = true;
        _midpointShown = false;
        _closedShown = false;
    }

    public void Restore(double remaining, double duration)
    {
        Dismiss();
        _openingPending = false;
        _midpointShown = remaining <= duration / 2;
        _closedShown = remaining <= 0;
    }

    public void Observe(double remaining, double duration, bool closed, int currentDay, int totalDays)
    {
        if (closed)
        {
            if (!_closedShown)
            {
                _closedShown = true;
                _openingPending = false;
                _midpointShown = true;
                ShowMessage("Магазин закрыт");
            }
            return;
        }
        if (_closedShown) Dismiss();
        _closedShown = false;
        if (_openingPending)
        {
            _openingPending = false;
            _totalDays = System.Math.Max(1, totalDays);
            _day = System.Math.Clamp(currentDay, 1, _totalDays);
            RefreshDetails();
            ShowMessage("Магазин открыт", true);
        }
        if (!_midpointShown && remaining <= duration / 2)
        {
            _midpointShown = true;
            ShowMessage("Середина дня");
        }
    }

    private void ShowMessage(string message, bool showDetails = false)
    {
        _title.Text = message; // Godot translates on display and on locale changes.
        _details.Visible = showDetails;
        UpdateFont();
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
