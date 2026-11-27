using System;
using System.Threading.Tasks;
using Godot;
using ChikagoShop.Domain.Dialogue;

// First-quota conversation and both possible endings.
// Uses the same backdrop, portraits and speech bubble as the opening cutscene.
public partial class MafiaQuotaNotice : CanvasLayer
{
    public event Action Dismissed;
    public event Action RestartRequested;
    public event Action MainMenuRequested;

    private Control _root = null!;
    private ColorRect _shade = null!;
    private Label _heading = null!;
    private Label _speaker = null!;
    private Label _message = null!;
    private TextureButton _skipDialogue = null!;
    private Button _restart = null!;
    private Button _mainMenu = null!;
    private StoryPortraitController _portraits = null!;
    private StorySpeakerFocus _speakerFocus = null!;
    private double _speechTime;
    private Tween _fade = null!;
    private bool _ownsPause;
    private bool _terminal;
    private bool _storyFinished;
    private bool _transitioning;
    private int _transitionRevision;
    private DialogueSession _conversation;
    private string _pageText = "";
    private int _pageStart;
    private int _pageEnd;
    private string _currentSpeaker = "";
    public int ConversationIndex => _conversation?.Index ?? 0;
    public bool StoryFinished => _storyFinished;

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
            GD.Load<Texture2D>("res://art/dialogue/castello.png"), 0.325f);
        StoryDialogueLayout.AddPortrait(characters, "Frankie",
            GD.Load<Texture2D>("res://art/dialogue/frankie.png"), 0.6475f, top: 0.245f, bottom: 0.785f);

        var castello = characters.GetNode<TextureRect>("Castello");
        var frankie = characters.GetNode<TextureRect>("Frankie");
        _portraits = new StoryPortraitController(castello, StoryPortraitController.Castello,
            frankie, StoryPortraitController.Frankie);
        _speakerFocus = new StorySpeakerFocus(this, ("Кастелло", castello), ("Френки", frankie));
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
        _skipDialogue = new TextureButton { Name = "SkipDialogue" };
        _root.AddChild(_skipDialogue);
        StoryDialogueLayout.StyleSkipButton(_skipDialogue);
        _skipDialogue.Pressed += CloseWithTransition;
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

    public async void OpenWithTransition(int paidQuota, int remaining, bool succeeded, bool final)
    {
        if (IsOpen || _transitioning || SceneTransition.IsActive(GetTree())) return;
        _transitioning = true;
        int revision = ++_transitionRevision;
        bool pausedBefore = GetTree().Paused;
        try
        {
            await SceneTransition.Run(this, () =>
            {
                if (revision != _transitionRevision)
                {
                    GetTree().Paused = pausedBefore;
                    return Task.FromResult(false);
                }
                Open(paidQuota, remaining, succeeded, final);
                _ownsPause = !pausedBefore;
                return Task.FromResult(true);
            }, keepChangedPause: true);
        }
        finally { _transitioning = false; }
    }

    public void Open(int paidQuota, int remaining, bool succeeded, bool final)
    {
        if (IsOpen) return;
        bool english = GameLocalization.Language == "en";
        _terminal = !succeeded || final;
        _storyFinished = false;
        _heading.Text = succeeded
            ? final ? (english ? "VICTORY" : "ПОБЕДА")
                : (english ? "WEEK TWO" : "НЕДЕЛЯ ВТОРАЯ")
            : (english ? "DEFEAT" : "ПОРАЖЕНИЕ");
        string notice = !succeeded ? "defeat" : final ? "victory" : "firstQuota";
        _conversation = DialogueSession.FromJson(FileAccess.GetFileAsString(
            $"res://dialogue/{(notice == "firstQuota" ? "first_quota" : notice)}.ru.json"));
        DialogueLine line = _conversation.Current;
        ShowLine(line);
        StoryDialogueLayout.StyleSkipButton(_skipDialogue);
        _restart.Text = english ? "New game" : "Начать заново";
        _mainMenu.Text = english ? "Main menu" : "Главное меню";
        _skipDialogue.Show();
        _restart.Hide();
        _mainMenu.Hide();
        _ownsPause = !GetTree().Paused;
        GetTree().Paused = true;
        _root.Show();
        _shade.Color = StoryDialogueLayout.Shade with { A = 0 };
        if (_fade?.IsValid() == true) _fade.Kill();
        _fade = CreateTween().SetPauseMode(Tween.TweenPauseMode.Process);
        _fade.TweenProperty(_shade, "color:a", StoryDialogueLayout.Shade.A, 0.28f);
        GetViewport().GuiReleaseFocus();
    }

    private void ShowLine(DialogueLine line)
    {
        _currentSpeaker = line.Speaker;
        _speaker.Text = GameLocalization.Text(line.Speaker);
        _speaker.Visible = !string.IsNullOrWhiteSpace(line.Speaker);
        _pageText = StoryDialogueLayout.LineText(line.Text);
        _pageStart = 0;
        _portraits.SetLine(line);
        ShowPage();
    }

    private void ShowPage()
    {
        // Bound each page to three rows at the current width, including narrow windows.
        float width = Mathf.Max(160, _message.Size.X > 1 ? _message.Size.X
            : GetViewport().GetVisibleRect().Size.X * 0.78f - 200);
        var font = _message.GetThemeFont("font");
        int lastSpace = -1, rowStart = _pageStart, rows = 1;
        _pageEnd = _pageText.Length;
        for (int i = _pageStart; i < _pageText.Length; i++)
        {
            if (char.IsWhiteSpace(_pageText[i])) lastSpace = i;
            if (font.GetStringSize(_pageText[rowStart..(i + 1)], fontSize: StoryDialogueLayout.BodySize).X <= width) continue;
            int wrap = lastSpace >= rowStart ? lastSpace + 1 : i;
            if (++rows > 3) { _pageEnd = Mathf.Max(_pageStart + 1, wrap); break; }
            rowStart = wrap;
        }
        _message.Text = _pageText[_pageStart.._pageEnd].Trim();
        _speechTime = 0;
        _message.VisibleCharacters = 0;
        _portraits.ShowSpeech(0);
        _speakerFocus.SetSpeaker(_currentSpeaker);
        SetProcess(true);
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
            _speakerFocus.SetSpeaker(null);
            SetProcess(false);
        }
    }

    public void Close() => CloseCore(true);
    public void CloseSilently() => CloseCore(false);

    private async void CloseWithTransition()
    {
        if (!IsOpen || _transitioning || SceneTransition.IsActive(GetTree())) return;
        if (_terminal) { FinishStory(); return; }
        _transitioning = true;
        try
        {
            await SceneTransition.Run(this, () =>
            {
                CloseCore(true);
                return Task.FromResult(true);
            }, keepChangedPause: true);
        }
        finally { _transitioning = false; }
    }

    private void CloseCore(bool notify)
    {
        if (!IsOpen && !_transitioning) return;
        ++_transitionRevision;
        if (_fade?.IsValid() == true) _fade.Kill();
        SetProcess(false);
        _portraits.ShowSpeech(null);
        _speakerFocus.SetSpeaker(null);
        _root.Hide();
        if (_ownsPause) GetTree().Paused = false;
        _ownsPause = false;
        if (notify) Dismissed?.Invoke();
    }

    private void FinishStory()
    {
        if (_storyFinished) return;
        _storyFinished = true;
        SetProcess(false);
        _message.VisibleCharacters = -1;
        _portraits.ShowSpeech(null, lineComplete: true);
        _speakerFocus.SetSpeaker(null);
        _skipDialogue.Hide();
        _restart.Show();
        _mainMenu.Show();
        _restart.GrabFocus();
    }

    public override void _ExitTree()
    {
        if (_fade?.IsValid() == true) _fade.Kill();
        if (_ownsPause && GetTree() != null) GetTree().Paused = false;
        _ownsPause = false;
    }

    public override void _Input(InputEvent input)
    {
        if (!IsOpen || _transitioning || SceneTransition.IsActive(GetTree())) return;
        if (!_storyFinished && (input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } ||
            input is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Space }))
        {
            if (input is InputEventMouseButton && GetViewport().GuiGetHoveredControl() is BaseButton) return;
            GetViewport().SetInputAsHandled();
            if (_message.VisibleCharacters >= 0)
            {
                _speechTime = _message.Text.Length / 48.0 + 1;
                _Process(0);
            }
            else if (_pageEnd < _pageText.Length)
            {
                _pageStart = _pageEnd;
                ShowPage();
            }
            else if (_conversation != null && !_conversation.Advance()) ShowLine(_conversation.Current);
            else CloseWithTransition();
            return;
        }
        if (!IsOpen || input is not InputEventKey { Pressed: true, Echo: false } key) return;
        if (key.Keycode != Key.Escape) return;
        GetViewport().SetInputAsHandled();
        if (!_storyFinished) CloseWithTransition();
        else _mainMenu.GrabFocus();
    }
}
