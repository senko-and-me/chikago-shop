using Godot;

public partial class PauseMenu : CanvasLayer
{
	private Control _overlay = null!;
	private VBoxContainer _menu = null!;
	private VBoxContainer _settings = null!;
	private Button _resume = null!;
	private Button _lastHoveredMenuButton;
	private FullscreenToggle _fullscreen = null!;
    private PanelContainer _panel = null!;
    private StyleBoxTexture _pauseBackdrop = null!, _settingsBackdrop = null!;
	private Button _russian = null!, _english = null!;
	private bool _ownsPause;
    private Control _previousFocus;
    public bool IsOpen => _overlay?.Visible == true;

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
		_panel = new PanelContainer { Name = "Panel", CustomMinimumSize = new Vector2(630, 0) };
		var style = new StyleBoxTexture
		{
			Texture = GD.Load<Texture2D>("res://art/ui/settings_panel.png")
		};
		style.ContentMarginLeft = 42;
		style.ContentMarginRight = 42;
		style.ContentMarginTop = 32;
		style.ContentMarginBottom = 32;
		_pauseBackdrop = style;
        _settingsBackdrop = (StyleBoxTexture)style.Duplicate();
        _settingsBackdrop.SetExpandMargin(Side.Left, 32);
        _settingsBackdrop.SetExpandMargin(Side.Right, 32);
        _settingsBackdrop.SetExpandMargin(Side.Bottom, 54);
		_panel.AddThemeStyleboxOverride("panel", _pauseBackdrop);
		center.AddChild(_panel);
		var content = new VBoxContainer { Name = "Content" };
		_panel.AddChild(content);
		_menu = new VBoxContainer { Name = "Menu" };
		_menu.AddThemeConstantOverride("separation", 14);
		content.AddChild(_menu);
		AddTitle(_menu, "ПАУЗА");
		_resume = AddButton(_menu, "ResumeButton", "Продолжить", Close);
		AddButton(_menu, "SettingsButton", "Настройки", ShowSettings);
		AddButton(_menu, "CheckpointButton", "Начать с чекпоинта", () =>
		{
            DismissCraft();
			GetParent<TradeScene>().ReturnToCheckpoint();
			Close();
		});
		AddButton(_menu, "NewGameButton", "Начать заново", () =>
		{
            DismissCraft();
			GetParent<TradeScene>().NewGame();
			Close();
		});
		AddButton(_menu, "MainMenuButton", "В главное меню", ReturnToMainMenu);

		_settings = new VBoxContainer { Name = "Settings", Visible = false };
		_settings.AddThemeConstantOverride("separation", 14);
		content.AddChild(_settings);
		AddTitle(_settings, "НАСТРОЙКИ");
		_fullscreen = GD.Load<PackedScene>("res://scenes/UI/FullscreenToggle.tscn").Instantiate<FullscreenToggle>();
		_settings.AddChild(_fullscreen);
        var fullscreenArtwork = new ShaderMaterial
        {
            Shader = GD.Load<Shader>("res://shaders/pause_fullscreen_toggle.gdshader")
        };
        _fullscreen.Material = fullscreenArtwork;
        void UpdateFullscreenArtwork()
        {
            Vector2 image = _fullscreen.TextureNormal.GetSize();
            Vector2 button = _fullscreen.Size;
            fullscreenArtwork.SetShaderParameter("artwork_width_ratio", (image.X / image.Y) * button.Y / Mathf.Max(1, button.X));
        }
        _fullscreen.Resized += UpdateFullscreenArtwork;
        UpdateFullscreenArtwork();
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

	private async void ReturnToMainMenu()
	{
		var tree = GetTree();
		if (SceneTransition.IsActive(tree)) return;
		_ownsPause = false;
		tree.Paused = false;
		Error error = Error.Ok;
		bool changed = await SceneTransition.Run(this, () =>
		{
			error = tree.ChangeSceneToFile("res://scenes/main_menu.tscn");
			return System.Threading.Tasks.Task.FromResult(error == Error.Ok);
		});
		if (!changed && GodotObject.IsInstanceValid(this) && IsInsideTree())
		{
			_overlay.Hide();
			Open();
			if (error != Error.Ok) GD.PushError($"Не удалось открыть главное меню: {error}");
		}
	}

	public void Open()
	{
        var game = GetParent<TradeScene>();
        if (IsOpen || game.DayTransitionOpen ||
            game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").IsSkipConfirmationOpen) return;
        Layer = game.CraftMiniGameOpen ? 115 : 100;
        _previousFocus = GetViewport().GuiGetFocusOwner();
        game.GetNode<EmbroideryGame>("PolishingGame").SuspendPolishingInput();
        game.GetNode<EmbroideryGame>("EmbroideryGame").RestoreNeedleCursor();
        game.PersistProgress();
		_ownsPause = true;
		GetTree().Paused = true;
		_overlay.Show();
		ShowMenu();
	}

	public void Close()
	{
		_ownsPause = false;
		GetTree().Paused = GetParent<TradeScene>().CraftMiniGameOpen;
		_overlay.Hide();
        if (GodotObject.IsInstanceValid(_previousFocus) && _previousFocus.IsVisibleInTree())
            _previousFocus.GrabFocus();
        _previousFocus = null;
	}

    private void DismissCraft()
    {
        GetParent<TradeScene>().Crafting.Catalog.CloseForReset();
        GetParent<TradeScene>().Crafting.CancelPreparation();
        GetParent().GetNode<EmbroideryGame>("EmbroideryGame").Dismiss();
        GetParent().GetNode<EmbroideryGame>("PolishingGame").Dismiss();
    }

	private void ShowSettings()
	{
		RefreshLanguageSelection();
		_fullscreen.RefreshState();
		_menu.Hide();
		_settings.Show();
        _panel.AddThemeStyleboxOverride("panel", _settingsBackdrop);
		_fullscreen.GrabFocus();
	}

	private void ShowMenu()
	{
		_settings.Hide();
        _panel.AddThemeStyleboxOverride("panel", _pauseBackdrop);
		_menu.Show();
		var selected = _lastHoveredMenuButton;
		if (selected == null || !GodotObject.IsInstanceValid(selected) || selected.Disabled) selected = _resume;
		selected.GrabFocus();
	}

	public override void _Input(InputEvent @event)
	{
		if (SceneTransition.IsActive(GetTree()))
		{
			GetViewport().SetInputAsHandled();
			return;
		}
		if (@event is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape }) return;
        var game = GetParent<TradeScene>();
        if (game.DayTransitionOpen || game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").IsSkipConfirmationOpen) return;
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
