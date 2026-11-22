using System;
using Godot;

// One-line story placeholders for the first quota and both possible endings.
// Uses the same backdrop, portraits and speech bubble as the opening cutscene.
public partial class MafiaQuotaNotice : CanvasLayer
{
    public event Action Dismissed;
    public event Action RestartRequested;
    public event Action CheckpointRequested;
    public event Action MainMenuRequested;

    private Control _root = null!;
    private ColorRect _shade = null!;
    private Label _heading = null!;
    private Label _speaker = null!;
    private Label _message = null!;
    private Button _continue = null!;
    private Button _restart = null!;
    private Button _checkpoint = null!;
    private Button _mainMenu = null!;
    private Tween _fade = null!;
    private bool _ownsPause;
    private bool _terminal;

    public bool IsOpen => _root != null && _root.Visible;

    public override void _Ready()
    {
        Name = "MafiaQuotaNotice";
        Layer = 150;
        ProcessMode = ProcessModeEnum.Always;

        Theme theme = GD.Load<Theme>("res://themes/game_theme.tres");
        _root = new Control
        {
            Name = "StoryRoot", Visible = false, Theme = theme,
            MouseFilter = Control.MouseFilterEnum.Stop,
            ProcessMode = ProcessModeEnum.Always
        };
        AddChild(_root);
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var background = new TextureRect
        {
            Name = "Background",
            Texture = GD.Load<Texture2D>("res://art/rooms/showroom.jpg"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _root.AddChild(background);
        background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        _shade = new ColorRect
        {
            Name = "Shade", Color = new Color(0, 0, 0, 0),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _root.AddChild(_shade);
        _shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var characters = new Control
        {
            Name = "Characters", MouseFilter = Control.MouseFilterEnum.Ignore,
            AnchorLeft = 0.075f, AnchorTop = 0.12f,
            AnchorRight = 0.925f, AnchorBottom = 0.82f
        };
        _root.AddChild(characters);
        AddPortrait(characters, "Castello", "res://art/dialogue/castello.png", 0.00f, 0.30f);
        AddPortrait(characters, "Frankie", "res://art/dialogue/frankie.png", 0.67f, 1.00f);

        _heading = new Label
        {
            Name = "Heading", AnchorRight = 1.0f, OffsetTop = 24,
            HorizontalAlignment = HorizontalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _heading.AddThemeFontSizeOverride("font_size", 26);
        _heading.AddThemeColorOverride("font_color", Colors.White);
        _root.AddChild(_heading);

        var speech = new PanelContainer
        {
            Name = "Speech", AnchorLeft = 0.11f, AnchorTop = 1.0f,
            AnchorRight = 0.89f, AnchorBottom = 1.0f,
            OffsetTop = -285, OffsetBottom = -16,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        var paper = new StyleBoxTexture
        {
            Texture = GD.Load<Texture2D>("res://art/ui/dialog_panel.png"),
            ContentMarginLeft = 105, ContentMarginRight = 105,
            ContentMarginTop = 48, ContentMarginBottom = 42
        };
        speech.AddThemeStyleboxOverride("panel", paper);
        _root.AddChild(speech);

        var content = new VBoxContainer { Name = "Content" };
        content.AddThemeConstantOverride("separation", 9);
        speech.AddChild(content);

        _speaker = new Label { Name = "Speaker", MouseFilter = Control.MouseFilterEnum.Ignore };
        _speaker.AddThemeColorOverride("font_color", new Color(0.73f, 0.73f, 0.73f));
        _speaker.AddThemeFontSizeOverride("font_size", 21);
        content.AddChild(_speaker);

        _message = new Label
        {
            Name = "Message", SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _message.AddThemeColorOverride("font_color", new Color(0.92f, 0.92f, 0.92f));
        _message.AddThemeFontSizeOverride("font_size", 22);
        content.AddChild(_message);

        var actions = new HBoxContainer
        {
            Name = "Actions", Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        actions.AddThemeConstantOverride("separation", 12);
        content.AddChild(actions);
        _continue = MakeAction(actions, "Continue", () => CloseCore(true));
        _checkpoint = MakeAction(actions, "Checkpoint", () =>
        {
            CloseCore(false);
            CheckpointRequested?.Invoke();
        });
        _restart = MakeAction(actions, "Restart", () =>
        {
            CloseCore(false);
            RestartRequested?.Invoke();
        });
        _mainMenu = MakeAction(actions, "MainMenu", () =>
        {
            CloseCore(false);
            MainMenuRequested?.Invoke();
        });
    }

    private static void AddPortrait(Control parent, string name, string path, float left, float right)
    {
        var portrait = new TextureRect
        {
            Name = name, AnchorLeft = left, AnchorTop = 0,
            AnchorRight = right, AnchorBottom = 1,
            Texture = GD.Load<Texture2D>(path),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        parent.AddChild(portrait);
    }

    private static Button MakeAction(HBoxContainer parent, string name, Action click)
    {
        var button = new Button
        {
            Name = name, CustomMinimumSize = new Vector2(188, 46),
            MouseDefaultCursorShape = Control.CursorShape.PointingHand
        };
        var normal = new StyleBoxTexture
        {
            Texture = GD.Load<Texture2D>("res://art/ui/panel_small.png"),
            ContentMarginLeft = 10, ContentMarginRight = 10,
            ContentMarginTop = 6, ContentMarginBottom = 6
        };
        var hover = (StyleBoxTexture)normal.Duplicate(true);
        hover.ModulateColor = new Color(0.86f, 0.86f, 0.86f);
        var pressed = (StyleBoxTexture)normal.Duplicate(true);
        pressed.ModulateColor = new Color(0.65f, 0.65f, 0.65f);
        button.AddThemeStyleboxOverride("normal", normal);
        button.AddThemeStyleboxOverride("hover", hover);
        button.AddThemeStyleboxOverride("pressed", pressed);
        button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
        foreach (string state in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color" })
            button.AddThemeColorOverride(state, Colors.Black);
        button.AddThemeFontSizeOverride("font_size", 19);
        button.Pressed += click;
        parent.AddChild(button);
        return button;
    }

    public void Open(int paidQuota, int remaining, bool succeeded, bool final)
    {
        if (IsOpen) return;
        bool english = GameLocalization.Language == "en";
        _terminal = !succeeded || final;
        _heading.Text = succeeded
            ? final ? (english ? "VICTORY" : "ПОБЕДА")
                : (english ? "FIRST QUOTA PAID" : "ПЕРВАЯ КВОТА ВЫПЛАЧЕНА")
            : (english ? "DEFEAT" : "ПОРАЖЕНИЕ");
        _speaker.Text = english ? "Castello" : "Кастелло";
        _message.Text = !succeeded
            ? (english ? "You did not keep your promise, Frankie. Our wager ends here."
                : "Ты не сдержал слово, Френки. Наше пари окончено.")
            : final
                ? (english ? "You won, Frankie. I did not expect that. I will keep my word."
                    : "Ты победил, Френки. Не ожидал. Я сдержу своё слово.")
                : (english ? "You survived the first week, Frankie. But our wager isn't over."
                    : "Первую неделю ты выдержал, Френки. Но наше пари ещё не окончено.");
        _continue.Text = english ? "Continue" : "Продолжить";
        _checkpoint.Text = english ? "From checkpoint" : "С чекпоинта";
        _restart.Text = english ? "New game" : "Начать заново";
        _mainMenu.Text = english ? "Main menu" : "Главное меню";
        _continue.Visible = !_terminal;
        _checkpoint.Visible = !succeeded;
        _restart.Visible = _terminal;
        _mainMenu.Visible = _terminal;
        _ownsPause = !GetTree().Paused;
        GetTree().Paused = true;
        _root.Show();
        _shade.Color = new Color(0, 0, 0, 0);
        if (_fade?.IsValid() == true) _fade.Kill();
        _fade = CreateTween().SetPauseMode(Tween.TweenPauseMode.Process);
        _fade.TweenProperty(_shade, "color:a", 0.45f, 0.28f);
        (_terminal ? _restart : _continue).GrabFocus();
    }

    public void Close() => CloseCore(true);
    public void CloseSilently() => CloseCore(false);

    private void CloseCore(bool notify)
    {
        if (!IsOpen) return;
        if (_fade?.IsValid() == true) _fade.Kill();
        _root.Hide();
        if (_ownsPause) GetTree().Paused = false;
        _ownsPause = false;
        if (notify) Dismissed?.Invoke();
    }

    public override void _ExitTree()
    {
        if (_fade?.IsValid() == true) _fade.Kill();
        if (_ownsPause && GetTree() != null) GetTree().Paused = false;
        _ownsPause = false;
    }

    public override void _Input(InputEvent input)
    {
        if (!IsOpen || input is not InputEventKey { Pressed: true, Echo: false } key) return;
        if (key.Keycode != Key.Escape) return;
        GetViewport().SetInputAsHandled();
        if (!_terminal) CloseCore(true);
        else _mainMenu.GrabFocus();
    }
}
