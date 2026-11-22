using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;
using ChikagoShop.Domain.Dialogue;

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
    private Dictionary<string, DialogueLine> _noticeLines = null!;
    private StoryPortraitController _portraits = null!;
    private double _speechTime;
    private Tween _fade = null!;
    private bool _ownsPause;
    private bool _terminal;

    public bool IsOpen => _root != null && _root.Visible;

    public override void _Ready()
    {
        Name = "MafiaQuotaNotice";
        Layer = 150;
        ProcessMode = ProcessModeEnum.Always;
        _noticeLines = JsonSerializer.Deserialize<Dictionary<string, DialogueLine>>(
            FileAccess.GetFileAsString("res://dialogue/quota_notices.ru.json"),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

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
        StoryDialogueLayout.FrameShowroom(background);

        _shade = new ColorRect
        {
            Name = "Shade", Color = new Color(0, 0, 0, 0),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _root.AddChild(_shade);
        _shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var characters = new Control
        {
            Name = "Characters", MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _root.AddChild(characters);
        characters.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        StoryDialogueLayout.AddPortrait(characters, "Castello",
            GD.Load<Texture2D>("res://art/dialogue/castello.png"), 0.225f);
        StoryDialogueLayout.AddPortrait(characters, "Frankie",
            GD.Load<Texture2D>("res://art/dialogue/frankie.png"), 0.7475f, top: 0.245f, bottom: 0.785f);

        _portraits = new StoryPortraitController(characters.GetNode<TextureRect>("Castello"), StoryPortraitController.Castello,
            characters.GetNode<TextureRect>("Frankie"), StoryPortraitController.Frankie);
        SetProcess(false);

        _heading = new Label { Name = "Heading" };
        _root.AddChild(_heading);
        StoryDialogueLayout.StyleHeading(_heading);

        var speech = new PanelContainer { Name = "Speech" };
        _root.AddChild(speech);
        var content = new VBoxContainer { Name = "Content" };
        speech.AddChild(content);

        _speaker = new Label { Name = "Speaker" };
        content.AddChild(_speaker);
        StoryDialogueLayout.StylePanel(speech, _speaker);

        _message = new Label
        {
            Name = "Message", SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _message.AddThemeColorOverride("font_color", StoryDialogueLayout.TextColor);
        _message.AddThemeFontSizeOverride("font_size", StoryDialogueLayout.BodySize);
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
        string notice = !succeeded ? "defeat" : final ? "victory" : "firstQuota";
        DialogueLine line = _noticeLines[notice];
        _speaker.Text = GameLocalization.Text(line.Speaker);
        _message.Text = GameLocalization.Text(line.Text);
        _portraits.SetLine(line);
        _speechTime = 0;
        _message.VisibleCharacters = 0;
        _portraits.ShowSpeech(0);
        SetProcess(true);
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
        _shade.Color = StoryDialogueLayout.Shade with { A = 0 };
        if (_fade?.IsValid() == true) _fade.Kill();
        _fade = CreateTween().SetPauseMode(Tween.TweenPauseMode.Process);
        _fade.TweenProperty(_shade, "color:a", StoryDialogueLayout.Shade.A, 0.28f);
        (_terminal ? _restart : _continue).GrabFocus();
    }

    public override void _Process(double delta)
    {
        _speechTime += delta;
        _message.VisibleCharacters = (int)(_speechTime * 48);
        if (_message.VisibleCharacters < _message.Text.Length)
            _portraits.ShowSpeech(_speechTime);
        else
        {
            _message.VisibleCharacters = -1;
            _portraits.ShowSpeech(null, lineComplete: true);
            SetProcess(false);
        }
    }

    public void Close() => CloseCore(true);
    public void CloseSilently() => CloseCore(false);

    private void CloseCore(bool notify)
    {
        if (!IsOpen) return;
        if (_fade?.IsValid() == true) _fade.Kill();
        SetProcess(false);
        _portraits.ShowSpeech(null);
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
