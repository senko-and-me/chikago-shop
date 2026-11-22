using Godot;

public partial class PauseMenu : CanvasLayer
{
	private Control _overlay = null!;
	private VBoxContainer _menu = null!;
	private VBoxContainer _settings = null!;
	private Button _resume = null!;
	private Button _lastHoveredMenuButton;
	private FullscreenToggle _fullscreen = null!;
	private Button _russian = null!, _english = null!;
	private bool _ownsPause;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		Layer = 100;
		var root = new Control { Name = "Root", MouseFilter = Control.MouseFilterEnum.Ignore };
		AddChild(root);
		root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

		_overlay = new Control { Name = "Overlay", Visible = false };
		root.AddChild(_overlay);
		_overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		var shade = new ColorRect
		{
			Color = new Color(0.031765f, 0.031765f, 0.031765f, 0.78f),
			MouseFilter = Control.MouseFilterEnum.Stop
		};
		_overlay.AddChild(shade);
		shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		var center = new CenterContainer { Name = "Center" };
		_overlay.AddChild(center);
		center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		// Reuse the main menu's dark hand-drawn settings backdrop.
		var panel = new PanelContainer { Name = "Panel", CustomMinimumSize = new Vector2(630, 0) };
		var style = new StyleBoxTexture
		{
			Texture = GD.Load<Texture2D>("res://art/ui/settings_panel.png")
		};
		style.ContentMarginLeft = 42;
		style.ContentMarginRight = 42;
		style.ContentMarginTop = 32;
		style.ContentMarginBottom = 32;
		panel.AddThemeStyleboxOverride("panel", style);
		center.AddChild(panel);
		var content = new VBoxContainer { Name = "Content" };
		panel.AddChild(content);
		_menu = new VBoxContainer { Name = "Menu" };
		_menu.AddThemeConstantOverride("separation", 14);
		content.AddChild(_menu);
		AddTitle(_menu, "ПАУЗА");
		_resume = AddButton(_menu, "ResumeButton", "Продолжить", Close);
		AddButton(_menu, "SettingsButton", "Настройки", ShowSettings);
		AddButton(_menu, "CheckpointButton", "Начать с чекпоинта", () =>
		{
			GetParent<TradeScene>().ReturnToCheckpoint();
			Close();
		});
		AddButton(_menu, "NewGameButton", "Начать заново", () =>
		{
			GetParent<TradeScene>().NewGame();
			Close();
		});
		AddButton(_menu, "TutorialButton", "Повторить обучение", ReplayTutorial);
		AddButton(_menu, "MainMenuButton", "В главное меню", () =>
		{
			var tree = GetTree();
			_ownsPause = false;
			tree.Paused = false;
			var error = tree.ChangeSceneToFile("res://scenes/main_menu.tscn");
			if (error != Error.Ok)
			{
				Open();
				GD.PushError($"Не удалось открыть главное меню: {error}");
			}
		});

		_settings = new VBoxContainer { Name = "Settings", Visible = false };
		_settings.AddThemeConstantOverride("separation", 14);
		content.AddChild(_settings);
		AddTitle(_settings, "НАСТРОЙКИ");
		_fullscreen = GD.Load<PackedScene>("res://scenes/UI/FullscreenToggle.tscn").Instantiate<FullscreenToggle>();
		_settings.AddChild(_fullscreen);
		var volume = GD.Load<PackedScene>("res://scenes/UI/MusicVolumeControl.tscn").Instantiate<MusicVolumeControl>();
		volume.PlainStyle = true;
		_settings.AddChild(volume);
		var soundVolume = GD.Load<PackedScene>("res://scenes/UI/MusicVolumeControl.tscn").Instantiate<MusicVolumeControl>();
		soundVolume.Name = "SoundVolume";
		soundVolume.ControlsSoundEffects = true;
		soundVolume.PlainStyle = true;
		_settings.AddChild(soundVolume);
		var languages = new HBoxContainer { Name = "LanguageRow" };
		languages.AddThemeConstantOverride("separation", 12);
		_settings.AddChild(languages);
		var languageLabel = new Label
		{
			Name = "LanguageLabel", Text = "Язык",
			CustomMinimumSize = new Vector2(128, 0), ClipText = true,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			VerticalAlignment = VerticalAlignment.Center
		};
		languageLabel.AddThemeColorOverride("font_color", Colors.White);
		languages.AddChild(languageLabel);
		var languageGroup = new ButtonGroup();
		_english = AddLanguageButton(languages, languageGroup, "EnglishButton", "English", "en");
		_russian = AddLanguageButton(languages, languageGroup, "RussianButton", "Русский", "ru");
		RefreshLanguageSelection();
		AddButton(_settings, "BackButton", "Назад", ShowMenu);
		// Match the fixed-size captions used by the main menu's settings.
		languageLabel.AddThemeFontSizeOverride("font_size", 24);
		_english.AddThemeFontSizeOverride("font_size", 24);
		_russian.AddThemeFontSizeOverride("font_size", 24);
		_settings.GetNode<Button>("BackButton").AddThemeFontSizeOverride("font_size", 24);
	}

	private void ReplayTutorial()
	{
		Close();
		GetParent<TradeScene>().GetNode<FirstTradeTutorial>("FirstTradeTutorial").Restart();
	}

	private static Button AddLanguageButton(HBoxContainer row, ButtonGroup group, string name, string text, string language)
	{
		var button = new Button
		{
			Name = name, Text = text, ToggleMode = true, ButtonGroup = group,
			CustomMinimumSize = new Vector2(142, 52),
			AutoTranslateMode = Control.AutoTranslateModeEnum.Disabled
		};
		ApplyLanguageStyle(button);
		row.AddChild(button);
		button.Pressed += () =>
		{
			Error error = GameLocalization.SelectLanguage(language);
			if (error != Error.Ok) GD.PushWarning($"Language preference could not be saved: {error}");
		};
		return button;
	}

	private static StyleBoxTexture PaperStyle(Color tint)
	{
		var style = new StyleBoxTexture
		{
			Texture = GD.Load<Texture2D>("res://art/ui/panel_small.png"),
			ModulateColor = tint
		};
		style.ContentMarginLeft = 14;
		style.ContentMarginRight = 14;
		style.ContentMarginTop = 8;
		style.ContentMarginBottom = 8;
		return style;
	}

	private static void ApplyPaperButtonStyle(Button button)
	{
		button.AddThemeStyleboxOverride("normal", PaperStyle(Colors.White));
		button.AddThemeStyleboxOverride("hover", PaperStyle(new Color(0.88f, 0.88f, 0.88f)));
		var pressed = PaperStyle(new Color(0.67f, 0.67f, 0.67f));
		button.AddThemeStyleboxOverride("pressed", pressed);
		button.AddThemeStyleboxOverride("hover_pressed", pressed);
		button.AddThemeStyleboxOverride("disabled", PaperStyle(new Color(0.48f, 0.48f, 0.48f)));
		button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
		button.AddThemeColorOverride("font_color", new Color(0.10f, 0.10f, 0.10f));
		button.AddThemeColorOverride("font_hover_color", new Color(0.10f, 0.10f, 0.10f));
		button.AddThemeColorOverride("font_pressed_color", new Color(0.10f, 0.10f, 0.10f));
		button.AddThemeColorOverride("font_hover_pressed_color", new Color(0.10f, 0.10f, 0.10f));
		button.AddThemeColorOverride("font_focus_color", new Color(0.10f, 0.10f, 0.10f));
		button.AddThemeColorOverride("font_disabled_color", new Color(0.30f, 0.30f, 0.30f));
		button.AddThemeFontSizeOverride("font_size", 24);
		button.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
	}

	private static void ApplyLanguageStyle(Button button)
	{
		button.AddThemeStyleboxOverride("normal", PaperStyle(Colors.White));
		button.AddThemeStyleboxOverride("hover", PaperStyle(new Color(0.85f, 0.85f, 0.85f)));
		button.AddThemeStyleboxOverride("pressed", PaperStyle(new Color(0.47f, 0.47f, 0.47f)));
		button.AddThemeStyleboxOverride("hover_pressed", button.GetThemeStylebox("pressed"));
		button.AddThemeStyleboxOverride("disabled", PaperStyle(new Color(0.48f, 0.48f, 0.48f)));
		button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
		button.AddThemeColorOverride("font_color", new Color(0.08f, 0.08f, 0.08f));
		button.AddThemeColorOverride("font_hover_color", new Color(0.08f, 0.08f, 0.08f));
		button.AddThemeColorOverride("font_pressed_color", Colors.White);
		button.AddThemeColorOverride("font_hover_pressed_color", Colors.White);
		button.AddThemeColorOverride("font_disabled_color", new Color(0.30f, 0.30f, 0.30f));
		button.MouseDefaultCursorShape = Control.CursorShape.PointingHand;
	}

	private void RefreshLanguageSelection()
	{
		if (_russian == null || _english == null) return;
		bool english = GameLocalization.Language == "en";
		_russian.SetPressedNoSignal(!english);
		_english.SetPressedNoSignal(english);
		var ink = new Color(0.08f, 0.08f, 0.08f);
		_russian.AddThemeColorOverride("font_focus_color", english ? ink : Colors.White);
		_english.AddThemeColorOverride("font_focus_color", english ? Colors.White : ink);
	}

	public override void _Notification(int what)
	{
		if (what == NotificationTranslationChanged) RefreshLanguageSelection();
	}

	private static void AddTitle(VBoxContainer parent, string text)
	{
		var title = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center };
		title.AddThemeFontSizeOverride("font_size", 32);
		title.AddThemeColorOverride("font_color", Colors.White);
		parent.AddChild(title);
	}

	private Button AddButton(VBoxContainer parent, string name, string text, System.Action action)
	{
		var button = new Button { Name = name, Text = text, CustomMinimumSize = new Vector2(0, 52) };
		ApplyPaperButtonStyle(button);
		parent.AddChild(button);
		button.Pressed += action;
		if (parent == _menu)
			button.MouseEntered += () =>
			{
				if (button.Disabled || !button.IsVisibleInTree()) return;
				_lastHoveredMenuButton = button;
			};
		return button;
	}

	public void Open()
	{
		if (GetParent<TradeScene>().CraftMiniGameOpen || GetParent<TradeScene>().DayTransitionOpen) return;
		GetParent<TradeScene>().PersistProgress();
		_ownsPause = true;
		GetTree().Paused = true;
		_overlay.Show();
		ShowMenu();
	}

	public void Close()
	{
		_ownsPause = false;
		GetTree().Paused = false;
		_overlay.Hide();
	}

	private void ShowSettings()
	{
		RefreshLanguageSelection();
		_fullscreen.RefreshState();
		_menu.Hide();
		_settings.Show();
		_fullscreen.GrabFocus();
	}

	private void ShowMenu()
	{
		_settings.Hide();
		_menu.Show();
		var selected = _lastHoveredMenuButton;
		if (selected == null || !GodotObject.IsInstanceValid(selected) || selected.Disabled) selected = _resume;
		selected.GrabFocus();
	}

	public override void _Input(InputEvent @event)
	{
		if (GetParent<TradeScene>().CraftMiniGameOpen || GetParent<TradeScene>().DayTransitionOpen) return;
		if (@event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape }) return;
		if (!_overlay.Visible) Open();
		else if (_settings.Visible) ShowMenu();
		else Close();
		GetViewport().SetInputAsHandled();
	}

	public override void _ExitTree()
	{
		if (_ownsPause) GetTree().Paused = false;
	}
}
