using System;
using Godot;
using ChikagoShop.Domain.Dialogue;

public partial class DialoguePlayer : Control
{
    public event Action Finished;
    public event Action Cancelled;
    public DialogueSession Session { get; private set; }
    public string DialoguePath { get; set; } = "res://dialogue/intro.ru.json";
    private RichTextLabel _text;
    private Label _speaker;
    private int _pageStart;
    private int _pageEnd;
    private double _characters;
    private bool _closed;
    public bool IsRevealing => _text.VisibleCharacters >= 0;

    public override void _Ready()
    {
        Name = "DialoguePlayer";
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AutoTranslateMode = AutoTranslateModeEnum.Disabled;
        Theme = GD.Load<Theme>("res://themes/game_theme.tres");
        Session = DialogueSession.FromJson(FileAccess.GetFileAsString(DialoguePath));
        var background = new TextureRect
        {
            Texture = GD.Load<Texture2D>("res://art/rooms/showroom.jpg"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(background);
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var shade = new ColorRect { Color = new Color(0.06f, 0.045f, 0.035f, 0.78f), MouseFilter = MouseFilterEnum.Ignore };
        AddChild(shade);
        shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var heading = new Label { Text = "П Р О Л О Г\nДом моды Френки", HorizontalAlignment = HorizontalAlignment.Center };
        AddChild(heading);
        heading.SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide);
        heading.OffsetTop = 32;
        heading.AddThemeFontSizeOverride("font_size", 18);
        heading.AddThemeColorOverride("font_color", new Color("c7b591"));
        heading.AddThemeConstantOverride("line_spacing", 10);
        // The source cloud has nonzero alpha along its outer pixels. Fade those
        // pixels rather than showing the rectangular boundary of the PNG.
        var panel = new PanelContainer
        {
            Name = "Panel",
            Material = new ShaderMaterial
            {
                Shader = new Shader { Code = """
                    shader_type canvas_item;
                    void fragment() {
                        vec4 cloud = COLOR;
                        vec2 edge = min(UV, vec2(1.0) - UV);
                        cloud.a *= smoothstep(0.0, 0.055, edge.x)
                                 * smoothstep(0.0, 0.10, edge.y);
                        COLOR = cloud;
                    }
                    """ }
            }
        };
        AddChild(panel);
        panel.AnchorLeft = 0.11f;
        panel.AnchorRight = 0.89f;
        panel.AnchorTop = 1;
        panel.AnchorBottom = 1;
        void LayoutPanel()
        {
            panel.OffsetTop = -Math.Clamp(Size.Y * 0.34f, 220, 250) - 16;
            panel.OffsetBottom = -16;
        }
        Resized += LayoutPanel;
        LayoutPanel();
        var style = new StyleBoxTexture
        {
            Texture = GD.Load<Texture2D>("res://art/ui/dialog_panel.png"),
            ContentMarginLeft = 112, ContentMarginRight = 112, ContentMarginTop = 64, ContentMarginBottom = 64
        };
        panel.AddThemeStyleboxOverride("panel", style);
        var column = new VBoxContainer { Name = "Content" };
        column.AddThemeConstantOverride("separation", 10);
        panel.AddChild(column);
        _speaker = new Label { Name = "Speaker" };
        _speaker.AddThemeColorOverride("font_color", new Color("d7b67b"));
        _speaker.AddThemeFontSizeOverride("font_size", 20);
        column.AddChild(_speaker);
        _text = new RichTextLabel
        {
            Name = "Text", SizeFlagsVertical = SizeFlags.ExpandFill,
            BbcodeEnabled = false, ScrollActive = false, FocusMode = FocusModeEnum.None,
            MouseFilter = MouseFilterEnum.Pass
        };
        _text.AddThemeFontSizeOverride("normal_font_size", 20);
        foreach (string font in new[] { "normal_font", "bold_font", "italics_font", "bold_italics_font", "mono_font" })
            _text.AddThemeFontOverride(font, Theme.DefaultFont);
        _text.AddThemeConstantOverride("line_separation", 5);
        _text.AddThemeColorOverride("default_color", new Color("f2e6d1"));
        column.AddChild(_text);
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
        _speaker.Text = Session.Current.Speaker;
        _speaker.Visible = !string.IsNullOrWhiteSpace(Session.Current.Speaker);
        _pageStart = 0;
        DisplayPage();
    }

    private void DisplayPage()
    {
        if (_text.Size.X <= 0 || _text.Size.Y <= 0) return;
        _text.VisibleCharacters = -1;
        _text.AddThemeFontSizeOverride("normal_font_size", 20);
        _text.ScrollActive = false;
        string source = Session.Current.Text;
        // Measure with the actual shaped label, including its font, wrapping and
        // line spacing. The whole page is laid out before typewriter reveal.
        _pageEnd = SentencePages.End(source, _pageStart, candidate =>
        {
            _text.Text = candidate;
            return _text.GetContentHeight() <= _text.Size.Y;
        });
        _text.Text = source.Substring(_pageStart, _pageEnd - _pageStart);
        _text.ScrollActive = _text.GetLineCount() >= 3 || _text.GetContentHeight() > _text.Size.Y;
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
        if (_pageEnd < Session.Current.Text.Length)
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
