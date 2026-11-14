using Godot;

public partial class PauseMenu : CanvasLayer
{
	private Control _overlay = null!;
	private VBoxContainer _menu = null!;
	private VBoxContainer _settings = null!;
	private Button _resume = null!;
	private Button _lastHoveredMenuButton;
	private CheckButton _fullscreen = null!;
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
		MonochromePresentation.CoverExceptions(_overlay);
		_overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		var shade = new ColorRect
		{
			Color = new Color(0.04f, 0.03f, 0.025f, 0.78f),
			MouseFilter = Control.MouseFilterEnum.Stop
		};
		_overlay.AddChild(shade);
		shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		var center = new CenterContainer { Name = "Center" };
		_overlay.AddChild(center);
		center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		var panel = new PanelContainer { Name = "Panel", CustomMinimumSize = new Vector2(440, 0) };
		var style = new StyleBoxTexture { Texture = GD.Load<Texture2D>("res://art/ui/panel_large.png") };
		style.SetContentMarginAll(32);
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
		_fullscreen = new CheckButton { Name = "FullscreenButton", Text = "Полный экран" };
		_fullscreen.AddThemeColorOverride("font_color", new Color(0.23f, 0.19f, 0.17f));
		_fullscreen.AddThemeColorOverride("font_hover_color", new Color(0.23f, 0.19f, 0.17f));
		_fullscreen.AddThemeColorOverride("font_pressed_color", new Color(0.23f, 0.19f, 0.17f));
		_fullscreen.AddThemeColorOverride("font_hover_pressed_color", new Color(0.23f, 0.19f, 0.17f));
		_fullscreen.AddThemeColorOverride("font_focus_color", new Color(0.23f, 0.19f, 0.17f));
		_fullscreen.AddThemeColorOverride("font_disabled_color", new Color(0.23f, 0.19f, 0.17f));
		var fullscreenStyle = new StyleBoxFlat { BgColor = new Color(0.94f, 0.91f, 0.85f),
			ContentMarginLeft = 14, ContentMarginRight = 14, ContentMarginTop = 10, ContentMarginBottom = 10 };
		foreach (string state in new[] { "normal", "hover", "pressed", "hover_pressed", "disabled" })
			_fullscreen.AddThemeStyleboxOverride(state, fullscreenStyle);
		_settings.AddChild(_fullscreen);
		var volume = GD.Load<PackedScene>("res://scenes/UI/MusicVolumeControl.tscn").Instantiate<MusicVolumeControl>();
		volume.PlainStyle = true;
		_settings.AddChild(volume);
		_fullscreen.Toggled += enabled => DisplayServer.WindowSetMode(enabled
			? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed);
		var languages = new HBoxContainer { Name = "LanguageRow" };
		languages.AddThemeConstantOverride("separation", 12);
		_settings.AddChild(languages);
		var languageLabel = new Label
		{
			Name = "LanguageLabel", Text = "Язык",
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			VerticalAlignment = VerticalAlignment.Center
		};
		languageLabel.AddThemeColorOverride("font_color", new Color(0.23f, 0.19f, 0.17f));
		languages.AddChild(languageLabel);
		var languageGroup = new ButtonGroup();
		_english = AddLanguageButton(languages, languageGroup, "EnglishButton", "English", "en");
		_russian = AddLanguageButton(languages, languageGroup, "RussianButton", "Русский", "ru");
		RefreshLanguageSelection();
		AddButton(_settings, "BackButton", "Назад", ShowMenu);
	}

	private static Button AddLanguageButton(HBoxContainer row, ButtonGroup group, string name, string text, string language)
	{
		var button = new Button
		{
			Name = name, Text = text, ToggleMode = true, ButtonGroup = group,
			CustomMinimumSize = new Vector2(120, 44),
			AutoTranslateMode = Control.AutoTranslateModeEnum.Disabled
		};
		row.AddChild(button);
		button.Pressed += () =>
		{
			Error error = GameLocalization.SelectLanguage(language);
			if (error != Error.Ok) GD.PushWarning($"Language preference could not be saved: {error}");
		};
		return button;
	}

	private void RefreshLanguageSelection()
	{
		if (_russian == null || _english == null) return;
		_russian.SetPressedNoSignal(GameLocalization.Language == "ru");
		_english.SetPressedNoSignal(GameLocalization.Language == "en");
	}

	public override void _Notification(int what)
	{
		if (what == NotificationTranslationChanged) RefreshLanguageSelection();
	}

	private static void AddTitle(VBoxContainer parent, string text)
	{
		var title = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center };
		title.AddThemeFontSizeOverride("font_size", 32);
		title.AddThemeColorOverride("font_color", new Color(0.23f, 0.19f, 0.17f));
		parent.AddChild(title);
	}

	private Button AddButton(VBoxContainer parent, string name, string text, System.Action action)
	{
		var button = new Button { Name = name, Text = text, CustomMinimumSize = new Vector2(0, 48) };
		parent.AddChild(button);
		button.Pressed += action;
		if (parent == _menu)
			button.MouseEntered += () =>
			{
				if (button.Disabled || !button.IsVisibleInTree()) return;
				_lastHoveredMenuButton = button;
				button.GrabFocus();
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
		_fullscreen.SetPressedNoSignal(DisplayServer.WindowGetMode() is
			DisplayServer.WindowMode.Fullscreen or DisplayServer.WindowMode.ExclusiveFullscreen);
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
