using System;
using Godot;
using ChikagoShop.Domain.Dialogue;

public partial class DialoguePlayer : Control
{
	public event Action Finished;
	public event Action Cancelled;
	public event Action PageProgressed;
	public event Action<string> MusicCueRequested;
	public DialogueSession Session { get; private set; }
	[Export(PropertyHint.File, "*.json")]
	public string DialoguePath { get; set; } = "res://dialogue/intro.ru.json";
	[Export] public bool IsPrologue { get; set; } = true;
	[Export] public bool UseWorldScene { get; set; }
	public Texture2D StoryBackground { get; set; }
	public Texture2D StoryPortrait { get; set; }
	public int SavedPageStart => _pageStart;
	public bool SavedPageRevealed => !IsRevealing;
	private RichTextLabel _text;
	private Label _speaker;
	private Label _heading;
	private string _localizedText = "";
	private int _pageStart;
	private int _pageEnd;
	private const double CharactersPerSecond = 48;
	private double _characters;
	private StoryPortraitController _portraits;
	private StorySpeakerFocus _speakerFocus;
	private bool _closed;
	private Tween _portraitTransition;
	private DialogueTypingAudio _typingAudio;
	public bool IsRevealing => _text.VisibleCharacters >= 0;

	public void StopInteraction()
	{
		_typingAudio?.Stop();
		if (_portraitTransition?.IsValid() == true) _portraitTransition.Kill();
		if (IsPrologue) GetNode<PrologueCharacterMotion>("Characters").Stop();
		_portraits?.ShowSpeech(null, lineComplete: true);
		_speakerFocus?.SetSpeaker(null);
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
		StoryDialogueLayout.StyleHeading(_heading);
		GetNode<ColorRect>("Shade").Color = StoryDialogueLayout.Shade;
		var background = GetNode<TextureRect>("Background");
		var left = GetNode<TextureRect>("Characters/Castello");
		var right = GetNode<TextureRect>("Characters/Frankie");
		if (IsPrologue)
		{
			StoryDialogueLayout.FrameShowroom(background);
			_portraits = new StoryPortraitController(left, StoryPortraitController.Castello,
				right, StoryPortraitController.Frankie);
			_speakerFocus = new StorySpeakerFocus(this, ("Кастелло", left), ("Френки", right));
		}
		else if (UseWorldScene)
		{
			background.Hide();
			GetNode<Control>("Shade").Hide();
			GetNode<Control>("Characters").Hide();
			_heading.Hide();
			var skip = GetNode<TextureButton>("SkipDialogue");
			StoryDialogueLayout.StyleSkipButton(skip);
			skip.Pressed += SkipCurrentStory;
		}
		else
		{
			if (StoryBackground != null) background.Texture = StoryBackground;
			StoryDialogueLayout.FrameShowroom(background);
			var characters = GetNode<Control>("Characters");
			foreach (var portrait in characters.GetChildren())
				((CanvasItem)portrait).Hide();
			var son = characters.GetNodeOrNull<TextureRect>("Son");
			if (son != null)
			{
				son.Show();
				right.Show();
				_portraits = new StoryPortraitController(son, "Сын", right, StoryPortraitController.Frankie);
				_speakerFocus = new StorySpeakerFocus(this, ("Сын", son), ("Френки", right));
			}
			else if (StoryPortrait != null)
			{
				StoryDialogueLayout.AddPortrait(characters, "Son", StoryPortrait, 0.68f);
				_speakerFocus = new StorySpeakerFocus(this, ("Сын", characters.GetNode<Control>("Son")));
			}
			_heading.Hide();
			if (GetNodeOrNull<Button>("SkipDialogue") is { } skip)
				skip.Pressed += SkipCurrentStory;
		}
		_speaker = GetNode<Label>("Panel/Content/Speaker");
		_text = GetNode<RichTextLabel>("Panel/Content/Text");
		_typingAudio = DialogueTypingAudio.Attach(this);
		StoryDialogueLayout.StylePanel(GetNode<PanelContainer>("Panel"), _speaker);
		_text.AddThemeColorOverride("default_color", StoryDialogueLayout.TextColor);
		_text.AddThemeConstantOverride("line_separation", 5);
		_text.AddThemeFontSizeOverride("normal_font_size", StoryDialogueLayout.BodySize);
		foreach (string font in new[] { "normal_font", "bold_font", "italics_font", "bold_italics_font", "mono_font" })
			_text.AddThemeFontOverride(font, Theme.DefaultFont);
		_text.CustomMinimumSize = new Vector2(0,
			Mathf.Ceil(_text.GetThemeFont("normal_font").GetHeight(StoryDialogueLayout.BodySize) * 3 +
				_text.GetThemeConstant("line_separation") * 3 +
				_text.GetThemeStylebox("normal").GetMinimumSize().Y));
		var scrollbar = _text.GetVScrollBar();
		scrollbar.CustomMinimumSize = new Vector2(24, 0);
		// Let the global handwritten scroll-bar theme style this menu too.
		_text.Resized += () => Callable.From(() =>
		{
			if (_closed) return;
			int visibleUntil = _text.VisibleCharacters < 0 ? _pageEnd : _pageStart + _text.VisibleCharacters;
			DisplayPage(visibleUntil);
		}).CallDeferred();
		DisplayLine();
	}

	private void DisplayLine()
	{
		RefreshLocalizedLine();
		if (!string.IsNullOrWhiteSpace(Session.Current.MusicCue))
			MusicCueRequested?.Invoke(Session.Current.MusicCue);
	}

	private void RefreshLocalizedLine()
	{
		if (UseWorldScene) StoryDialogueLayout.StyleSkipButton(GetNode<TextureButton>("SkipDialogue"));
		_heading.Text = GameLocalization.Text("П Р О Л О Г\nДом моды Френки");
		if (Session.Current == null) return;
		_speaker.Text = GameLocalization.Text(Session.Current.Speaker);
		_speaker.Visible = !string.IsNullOrWhiteSpace(Session.Current.Speaker);
		if (IsPrologue)
		{
			GetNode<PrologueCharacterMotion>("Characters").SyncLine();
		}
		if (_portraits != null) UpdatePortraits();
		_portraits?.SetLine(Session.Current);
		_localizedText = StoryDialogueLayout.LineText(Session.Current.Text);
		_pageStart = 0;
		DisplayPage();
	}

	private void UpdatePortraits()
	{
		if (_portraitTransition?.IsValid() == true) _portraitTransition.Kill();
		_portraitTransition = CreateTween().SetParallel();
		foreach (var entry in IsPrologue
			? new[] { ("Frankie", "Френки"), ("Castello", "Кастелло") }
			: new[] { ("Frankie", "Френки"), ("Son", "Сын") })
		{
			var portrait = GetNode<TextureRect>("Characters/" + entry.Item1);
			float tone = Session.Current.Speaker == entry.Item2 ? 0 : StoryDialogueLayout.InactivePortraitTone;
			_portraitTransition.TweenProperty(portrait.Material, "shader_parameter/lighten", tone, 0.2)
				.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
		}
	}

	public override void _Notification(int what)
	{
		if (what == NotificationTranslationChanged && !_closed && _text != null && _heading != null && Session != null)
			RefreshLocalizedLine();
	}

	private void DisplayPage(int visibleUntil = 0)
	{
		if (_text.Size.X <= 0 || _text.Size.Y <= 0) return;
		_text.VisibleCharacters = -1;
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
		_characters = Math.Max(0, visibleUntil - _pageStart);
		bool revealed = visibleUntil >= _pageEnd;
		_text.VisibleCharacters = revealed ? -1 : (int)_characters;
		_typingAudio.BeginPage(_text.GetParsedText(), !string.IsNullOrWhiteSpace(Session.Current?.Speaker),
			revealed ? _text.GetTotalCharacterCount() : (int)_characters);
		UpdateMouths();
	}

	public override void _Process(double delta)
	{
		if (_closed || !IsRevealing) return;
		_characters += delta * CharactersPerSecond;
		_text.VisibleCharacters = (int)_characters;
		if (_text.VisibleCharacters >= _text.GetTotalCharacterCount()) Reveal();
		else
		{
			_typingAudio.Advance(_text.VisibleCharacters, delta);
			UpdateMouths();
		}
	}

	private void UpdateMouths()
	{
		string speaker = Session.Current?.Speaker;
		bool continues = _pageEnd < _localizedText.Length || !string.IsNullOrEmpty(Session.Next?.Speaker);
		_speakerFocus?.SetSpeaker(IsRevealing || continues ? speaker : null);
		_portraits?.ShowSpeech(IsRevealing ? _characters / CharactersPerSecond : null,
			!IsRevealing && _pageEnd >= _localizedText.Length);
	}

	private void Reveal()
	{
		_typingAudio.Stop();
		_text.VisibleCharacters = -1;
		UpdateMouths();
		PageProgressed?.Invoke();
	}

	public void RestorePage(int start, bool revealed)
	{
		if (_closed) return;
		_pageStart = Mathf.Clamp(start, 0, Math.Max(0, _localizedText.Length - 1));
		DisplayPage();
		if (revealed) Reveal();
	}

	public void Advance()
	{
		if (_closed) return;
		if (IsRevealing) { Reveal(); return; }
		if (_pageEnd < _localizedText.Length)
		{
			_pageStart = _pageEnd;
			DisplayPage();
			PageProgressed?.Invoke();
			return;
		}
		if (Session.Advance())
		{
			StopInteraction();
			Finished?.Invoke();
		}
		else DisplayLine();
	}

	private void AdvanceFromInput()
	{
		Advance();
	}

	private void SkipCurrentStory()
	{
		if (_closed || SceneTransition.IsActive(GetTree())) return;
		StopInteraction();
		Finished?.Invoke();
	}

	public override void _Input(InputEvent @event)
	{
		if (_closed || SceneTransition.IsActive(GetTree())) return;
		if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouse)
		{
			// Let the menu's Skip button receive the click instead of advancing text.
			if (GetViewport().GuiGetHoveredControl() is BaseButton) return;
			var scrollbar = _text.GetVScrollBar();
			if (scrollbar.IsVisibleInTree() && scrollbar.GetGlobalRect().HasPoint(mouse.Position)) return;
			GetViewport().SetInputAsHandled();
			AdvanceFromInput();
			return;
		}
		if (@event is not InputEventKey { Pressed: true, Echo: false } key) return;
		if (key.Keycode == Key.Escape)
		{
			GetViewport().SetInputAsHandled();
			if (!IsPrologue && HasNode("SkipDialogue")) return;
			StopInteraction();
			Cancelled?.Invoke();
		}
		else if (key.Keycode == Key.Space)
		{
			GetViewport().SetInputAsHandled();
			AdvanceFromInput();
		}
	}
}
