using System;
using Godot;
using ChikagoShop.Domain.Dialogue;

public partial class DialoguePlayer : Control
{
    public event Action Finished;
    public event Action Cancelled;
    public event Action<string> MusicCueRequested;
    public DialogueSession Session { get; private set; }
    [Export(PropertyHint.File, "*.json")]
    public string DialoguePath { get; set; } = "res://dialogue/intro.ru.json";
    private RichTextLabel _text;
    private Label _speaker;
    private Label _heading;
    private string _localizedText = "";
    private int _pageStart;
    private int _pageEnd;
    private double _characters;
    private bool _closed;
    private Tween _portraitTransition;
    public bool IsRevealing => _text.VisibleCharacters >= 0;

    public void StopInteraction()
    {
        if (_portraitTransition?.IsValid() == true) _portraitTransition.Kill();
        _closed = true;
        SetProcess(false);
    }

    public override void _Ready()
    {
        Name = "DialoguePlayer";
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AutoTranslateMode = AutoTranslateModeEnum.Disabled;
        Theme = GD.Load<Theme>("res://themes/game_theme.tres");
        Session = DialogueSession.FromJson(FileAccess.GetFileAsString(DialoguePath));
        _heading = GetNode<Label>("Heading");
        _speaker = GetNode<Label>("Panel/Content/Speaker");
        _text = GetNode<RichTextLabel>("Panel/Content/Text");
        var panel = GetNode<PanelContainer>("Panel");
        void LayoutPanel()
        {
            panel.OffsetTop = -Math.Clamp(Size.Y * 0.34f, 260, 280) - 16;
            panel.OffsetBottom = -16;
        }
        Resized += LayoutPanel;
        LayoutPanel();
        foreach (string font in new[] { "normal_font", "bold_font", "italics_font", "bold_italics_font", "mono_font" })
            _text.AddThemeFontOverride(font, Theme.DefaultFont);
        _text.CustomMinimumSize = new Vector2(0,
            Mathf.Ceil(_text.GetThemeFont("normal_font").GetHeight(20) * 3 +
                _text.GetThemeConstant("line_separation") * 3 +
                _text.GetThemeStylebox("normal").GetMinimumSize().Y));
        var scrollbar = _text.GetVScrollBar();
        scrollbar.CustomMinimumSize = new Vector2(8, 0);
        scrollbar.AddThemeStyleboxOverride("scroll", ScrollStyle("30291f", 4));
        scrollbar.AddThemeStyleboxOverride("grabber", ScrollStyle("aa8b55", 4));
        scrollbar.AddThemeStyleboxOverride("grabber_highlight", ScrollStyle("d7b67b", 4));
        scrollbar.AddThemeStyleboxOverride("grabber_pressed", ScrollStyle("f2d49c", 4));
        _text.Resized += () => Callable.From(() =>
        {
            if (!_closed) DisplayPage();
        }).CallDeferred();
        DisplayLine();
    }

    private static StyleBoxFlat ScrollStyle(string color, int radius) => new()
    {
        BgColor = new Color(color),
        CornerRadiusTopLeft = radius, CornerRadiusTopRight = radius,
        CornerRadiusBottomLeft = radius, CornerRadiusBottomRight = radius,
        ContentMarginLeft = 4, ContentMarginRight = 4,
        ContentMarginTop = 8, ContentMarginBottom = 8
    };

    private void DisplayLine()
    {
        RefreshLocalizedLine();
        if (!string.IsNullOrWhiteSpace(Session.Current.MusicCue))
            MusicCueRequested?.Invoke(Session.Current.MusicCue);
    }

    private void RefreshLocalizedLine()
    {
        _heading.Text = GameLocalization.Text("П Р О Л О Г\nДом моды Френки");
        if (Session.Current == null) return;
        _speaker.Text = GameLocalization.Text(Session.Current.Speaker);
        _speaker.Visible = !string.IsNullOrWhiteSpace(Session.Current.Speaker);
        UpdatePortraits();
        _localizedText = GameLocalization.Text(Session.Current.Text);
        _pageStart = 0;
        DisplayPage();
    }

    private void UpdatePortraits()
    {
        if (_portraitTransition?.IsValid() == true) _portraitTransition.Kill();
        _portraitTransition = CreateTween().SetParallel();
        foreach (var entry in new[] { ("Frankie", "Френки"), ("Castello", "Кастелло"), ("Customer", "Покупательница") })
        {
            var portrait = GetNode<TextureRect>("Characters/" + entry.Item1);
            float tone = Session.Current.Speaker == entry.Item2 ? 0 : 0.32f;
            _portraitTransition.TweenProperty(portrait.Material, "shader_parameter/lighten", tone, 0.2)
                .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationTranslationChanged && _text != null && _heading != null && Session != null)
            RefreshLocalizedLine();
    }

    private void DisplayPage()
    {
        if (_text.Size.X <= 0 || _text.Size.Y <= 0) return;
        _text.VisibleCharacters = -1;
        _text.AddThemeFontSizeOverride("normal_font_size", 20);
        _text.ScrollActive = false;
        string source = _localizedText;
        // Measure with the actual shaped label, including its font, wrapping and
        // line spacing. The whole page is laid out before typewriter reveal.
        _pageEnd = SentencePages.End(source, _pageStart, candidate =>
        {
            _text.Text = candidate;
            return _text.GetContentHeight() <= _text.Size.Y;
        });
        _text.Text = source.Substring(_pageStart, _pageEnd - _pageStart);
        _text.ScrollActive = _text.GetContentHeight() > _text.Size.Y;
        _text.ScrollToLine(0);
        _text.VisibleCharacters = 0;
        _characters = 0;
    }

    public override void _Process(double delta)
    {
        if (_closed || !IsRevealing) return;
        _characters += delta * 48;
        _text.VisibleCharacters = (int)_characters;
        if (_text.VisibleCharacters >= _text.GetTotalCharacterCount()) Reveal();
    }

    private void Reveal()
    {
        _text.VisibleCharacters = -1;
    }

    public void Advance()
    {
        if (_closed) return;
        if (IsRevealing) { Reveal(); return; }
        if (_pageEnd < _localizedText.Length)
        {
            _pageStart = _pageEnd;
            DisplayPage();
            return;
        }
        if (Session.Advance())
        {
            _closed = true;
            Finished?.Invoke();
        }
        else DisplayLine();
    }

    public override void _Input(InputEvent @event)
    {
        if (_closed) return;
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouse)
        {
            // Let the menu's Skip button receive the click instead of advancing text.
            if (GetViewport().GuiGetHoveredControl() is BaseButton) return;
            var scrollbar = _text.GetVScrollBar();
            if (scrollbar.IsVisibleInTree() && scrollbar.GetGlobalRect().HasPoint(mouse.Position)) return;
            GetViewport().SetInputAsHandled();
            Advance();
            return;
        }
        if (@event is not InputEventKey { Pressed: true, Echo: false } key) return;
        if (key.Keycode == Key.Escape)
        {
            GetViewport().SetInputAsHandled();
            _closed = true;
            Cancelled?.Invoke();
        }
        else if (key.Keycode == Key.Space)
        {
            GetViewport().SetInputAsHandled();
            Advance();
        }
    }
}
