
using Godot;

public partial class MainMenu : Control
{
	private const string ProgressSavePath =
		"user://trade_progress.json";

	[Export]
	public PackedScene GameScene { get; set; } =
		GD.Load<PackedScene>("res://scenes/trade_scene.tscn");

	private CenterContainer _mainMenu = null!;
	private CenterContainer _settingsMenu = null!;
	private VBoxContainer _menuButtons = null!;

	private TextureRect _settingsFrame = null!;
	private CanvasItem _title = null!;

	private Button _russian = null!;
	private Button _english = null!;
	private Label _languageLabel = null!;

	private TextureButton _playButton = null!;
	private TextureButton _settingsButton = null!;
	private TextureButton _quitButton = null!;
	private TextureButton _backButton = null!;
	private TextureButton _fullscreenButton = null!;

	private TextureButton _newGameButton = null!;
	private TextureButton _continueButton = null!;
	private TextureButton _choiceBackButton = null!;
	private TextureButton _skipButton = null!;

	private CanvasLayer _skipLayer = null!;

	private Texture2D _playRu = null!;
	private Texture2D _settingsRu = null!;
	private Texture2D _quitRu = null!;
	private Texture2D _backRu = null!;

	private bool _startingGame;
	private bool _showingGameChoice;
	private bool _skipMouseWasDown;

	public DialoguePlayer ActiveDialogue { get; private set; } = null!;

	public override void _Ready()
	{
		_mainMenu = GetNode<CenterContainer>(
			"MainMenuContainer");

		_settingsMenu = GetNode<CenterContainer>(
			"SettingsMenuContainer");

		_menuButtons = GetNode<VBoxContainer>(
			"MainMenuContainer/Menu");

		_settingsFrame = GetNode<TextureRect>(
			"SettingsFrame");

		_title = GetNode<CanvasItem>("Title");

		_settingsFrame.MouseFilter =
			Control.MouseFilterEnum.Ignore;

		_settingsFrame.Hide();
		_settingsMenu.Hide();
		_title.Show();

		_playButton = GetNode<TextureButton>(
			"MainMenuContainer/Menu/PlayButton");

		_settingsButton = GetNode<TextureButton>(
			"MainMenuContainer/Menu/SettingsButton");

		_quitButton = GetNode<TextureButton>(
			"MainMenuContainer/Menu/QuitButton");

		_backButton = GetNode<TextureButton>(
			"SettingsMenuContainer/Settings/BackButton");

		_playRu = _playButton.TextureNormal;
		_settingsRu = _settingsButton.TextureNormal;
		_quitRu = _quitButton.TextureNormal;
		_backRu = _backButton.TextureNormal;

		ConfigureBackButton();

		_playButton.Pressed += OnPlayPressed;
		_settingsButton.Pressed += OnSettingsPressed;
		_quitButton.Pressed += OnQuitPressed;
		_backButton.Pressed += OnBackPressed;

		_fullscreenButton = GetNode<TextureButton>(
			"SettingsMenuContainer/Settings/FullscreenButton");

		ConfigureTextureButton(
			_fullscreenButton,
			new Vector2(500, 75));

		_fullscreenButton.ToggleMode = true;

		_fullscreenButton.SetPressedNoSignal(
			IsFullscreen());

		_fullscreenButton.Toggled += OnFullscreenToggled;

		BuildLanguageMenu();
		BuildGameChoiceMenu();
		BuildSkipButton();

		UpdateAllTextures();
		ShowMainMenu();

		_skipMouseWasDown =
			Input.IsMouseButtonPressed(MouseButton.Left);
	}

	private void ConfigureTextureButton(
		TextureButton button,
		Vector2 size)
	{
		button.IgnoreTextureSize = true;

		button.StretchMode =
			TextureButton.StretchModeEnum.Scale;

		button.CustomMinimumSize = size;

		button.SizeFlagsHorizontal =
			Control.SizeFlags.ShrinkCenter;

		button.MouseFilter =
			Control.MouseFilterEnum.Stop;
	}

	private void ConfigureBackButton()
	{
		float width = 480f;
		float height = 144f;

		if (_backRu != null)
		{
			Vector2 original = _backRu.GetSize();

			if (original.X > 0)
			{
				height = width * original.Y / original.X;
			}
		}

		_backButton.IgnoreTextureSize = true;

		_backButton.StretchMode =
			TextureButton.StretchModeEnum.KeepAspectCentered;

		_backButton.CustomMinimumSize =
			new Vector2(width, Mathf.Round(height));

		_backButton.SizeFlagsHorizontal =
			Control.SizeFlags.ShrinkCenter;

		_backButton.SizeFlagsVertical =
			Control.SizeFlags.ShrinkCenter;
	}

	private void BuildLanguageMenu()
	{
		var settings = GetNode<VBoxContainer>(
			"SettingsMenuContainer/Settings");

		settings.AddThemeConstantOverride(
			"separation", 14);

		var row = new HBoxContainer
		{
			Name = "LanguageRow",
			Alignment = BoxContainer.AlignmentMode.Center,
			SizeFlagsHorizontal =
				Control.SizeFlags.ShrinkCenter
		};

		row.AddThemeConstantOverride(
			"separation", 12);

		settings.AddChild(row);

		settings.MoveChild(
			row,
			settings.GetNode("BackButton").GetIndex());

		_languageLabel = new Label
		{
			Name = "LanguageLabel",
			Text = "Язык",
			VerticalAlignment = VerticalAlignment.Center,
			AutoTranslateMode =
				AutoTranslateModeEnum.Disabled
		};

		_languageLabel.AddThemeColorOverride(
			"font_color", Colors.White);

		_languageLabel.AddThemeFontSizeOverride(
			"font_size", 22);

		row.AddChild(_languageLabel);

		var group = new ButtonGroup();

		_russian = AddLanguageButton(
			row,
			"RussianButton",
			"Русский",
			"ru",
			group);

		_english = AddLanguageButton(
			row,
			"EnglishButton",
			"English",
			"en",
			group);

		var backSpacer = new Control
		{
			Name = "BackSpacer",
			CustomMinimumSize = new Vector2(0, 20),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};

		settings.AddChild(backSpacer);

		settings.MoveChild(
			backSpacer,
			settings.GetNode("BackButton").GetIndex());
	}

	private Button AddLanguageButton(
		HBoxContainer row,
		string name,
		string text,
		string locale,
		ButtonGroup group)
	{
		var button = new Button
		{
			Name = name,
			Text = text,
			ToggleMode = true,
			ButtonGroup = group,
			CustomMinimumSize = new Vector2(144, 48),
			AutoTranslateMode =
				AutoTranslateModeEnum.Disabled
		};

		row.AddChild(button);

		StyleLanguageButton(button);

		button.Pressed += () =>
		{
			Error error =
				GameLocalization.SelectLanguage(locale);

			if (error != Error.Ok)
			{
				GD.PushWarning(
					$"Language preference could not be saved: {error}");
			}

			UpdateAllTextures();
		};

		return button;
	}

	private void StyleLanguageButton(Button button)
	{
		Color dark =
			new Color(0.07f, 0.07f, 0.07f);

		Color paper =
			new Color(0.96f, 0.95f, 0.93f);

		Color hover =
			new Color(0.83f, 0.82f, 0.79f);

		button.AddThemeStyleboxOverride(
			"normal",
			CreateLanguageStyle(dark, paper));

		button.AddThemeStyleboxOverride(
			"hover",
			CreateLanguageStyle(hover, paper));

		button.AddThemeStyleboxOverride(
			"pressed",
			CreateLanguageStyle(paper, paper));

		button.AddThemeStyleboxOverride(
			"hover_pressed",
			CreateLanguageStyle(paper, paper));

		button.AddThemeColorOverride(
			"font_color", Colors.White);

		button.AddThemeColorOverride(
			"font_hover_color", Colors.Black);

		button.AddThemeColorOverride(
			"font_pressed_color", Colors.Black);

		button.AddThemeColorOverride(
			"font_hover_pressed_color", Colors.Black);

		button.AddThemeFontSizeOverride(
			"font_size", 22);
	}

	private StyleBoxFlat CreateLanguageStyle(
		Color background,
		Color border)
	{
		return new StyleBoxFlat
		{
			BgColor = background,
			BorderColor = border,
			BorderWidthLeft = 2,
			BorderWidthRight = 2,
			BorderWidthTop = 2,
			BorderWidthBottom = 2,
			CornerRadiusTopLeft = 2,
			CornerRadiusTopRight = 2,
			CornerRadiusBottomLeft = 2,
			CornerRadiusBottomRight = 2,
			ContentMarginLeft = 12,
			ContentMarginRight = 12,
			ContentMarginTop = 6,
			ContentMarginBottom = 6
		};
	}

	private void BuildGameChoiceMenu()
	{
		_newGameButton = AddChoiceButton(
			"NewGameButton");

		_continueButton = AddChoiceButton(
			"ContinueButton");

		_choiceBackButton = AddChoiceButton(
			"ChoiceBackButton");

		_newGameButton.Pressed += OnNewGamePressed;
		_continueButton.Pressed += OnContinuePressed;
		_choiceBackButton.Pressed += OnChoiceBackPressed;

		SetChoiceButtonsVisible(false);
	}

	private TextureButton AddChoiceButton(string name)
	{
		var button = new TextureButton
		{
			Name = name,
			FocusMode = Control.FocusModeEnum.All
		};

		ConfigureTextureButton(
			button,
			new Vector2(350, 100));

		_menuButtons.AddChild(button);

		return button;
	}

	private void MatchChoiceButtonSizes()
	{
		Vector2 size = _playButton.Size;

		if (size.X < 50 || size.Y < 20)
		{
			size = _playButton.CustomMinimumSize;
		}

		if (size.X < 50 || size.Y < 20)
		{
			size = new Vector2(350, 100);
		}

		TextureButton[] buttons =
		{
			_newGameButton,
			_continueButton,
			_choiceBackButton
		};

		foreach (TextureButton button in buttons)
		{
			button.IgnoreTextureSize = true;

			button.StretchMode =
				TextureButton.StretchModeEnum.Scale;

			button.CustomMinimumSize = size;

			button.SizeFlagsHorizontal =
				_playButton.SizeFlagsHorizontal;

			button.SizeFlagsVertical =
				_playButton.SizeFlagsVertical;
		}
	}

	private void BuildSkipButton()
	{
		_skipLayer = new CanvasLayer
		{
			Name = "IntroSkipLayer",
			Layer = 1000
		};

		AddChild(_skipLayer);

		_skipButton = new TextureButton
		{
			Name = "SkipButton",
			FocusMode = Control.FocusModeEnum.All,
			MouseFilter = Control.MouseFilterEnum.Stop,
			IgnoreTextureSize = true,
			StretchMode =
				TextureButton.StretchModeEnum.Scale
		};

		_skipLayer.AddChild(_skipButton);

		_skipButton.CustomMinimumSize =
			new Vector2(180, 54);

		_skipButton.AnchorLeft = 1;
		_skipButton.AnchorRight = 1;
		_skipButton.AnchorTop = 0;
		_skipButton.AnchorBottom = 0;

		_skipButton.OffsetLeft = -204;
		_skipButton.OffsetRight = -24;
		_skipButton.OffsetTop = 24;
		_skipButton.OffsetBottom = 78;

		_skipButton.Pressed += OnSkipIntroPressed;

		_skipLayer.Hide();
	}

	private void UpdateAllTextures()
	{
		UpdateLanguageButtons();
		UpdateLanguageLabel();
		UpdateButtonTextures();
		UpdateFullscreenTexture();
		UpdateChoiceTextures();
		UpdateSkipTexture();
	}

	private void UpdateLanguageButtons()
	{
		if (_russian == null || _english == null)
			return;

		_russian.SetPressedNoSignal(
			GameLocalization.Language == "ru");

		_english.SetPressedNoSignal(
			GameLocalization.Language == "en");
	}

	private void UpdateLanguageLabel()
	{
		if (_languageLabel == null)
			return;

		_languageLabel.Text =
			GameLocalization.Language == "en"
				? "Language"
				: "Язык";
	}

	private void UpdateButtonTextures()
	{
		if (_playButton == null)
			return;

		bool english =
			GameLocalization.Language == "en";

		SetButtonTexture(
			_playButton,
			_playRu,
			"Play.png",
			english);

		SetButtonTexture(
			_settingsButton,
			_settingsRu,
			"Settings.png",
			english);

		SetButtonTexture(
			_quitButton,
			_quitRu,
			"Exit.png",
			english);

		SetButtonTexture(
			_backButton,
			_backRu,
			"Back.png",
			english);
	}

	private void SetButtonTexture(
		TextureButton button,
		Texture2D russianTexture,
		string englishFile,
		bool english)
	{
		if (!english)
		{
			button.TextureNormal = russianTexture;
			return;
		}

		string path =
			"res://art/ui/en/" + englishFile;

		if (ResourceLoader.Exists(path))
		{
			button.TextureNormal =
				GD.Load<Texture2D>(path);
		}
		else
		{
			GD.PushWarning(
				$"Button texture not found: {path}");

			button.TextureNormal = russianTexture;
		}
	}

	private void SetLocalizedTexture(
		TextureButton button,
		string russianPath,
		string englishPath)
	{
		string path =
			GameLocalization.Language == "en"
				? englishPath
				: russianPath;

		if (!ResourceLoader.Exists(path))
		{
			GD.PushWarning(
				$"Button texture not found: {path}");

			return;
		}

		button.TextureNormal =
			GD.Load<Texture2D>(path);
	}

	private void UpdateChoiceTextures()
	{
		if (_newGameButton == null)
			return;

		SetLocalizedTexture(
			_newGameButton,
			"res://art/ui/ru/NewGameRu.png",
			"res://art/ui/en/NewGame.png");

		SetLocalizedTexture(
			_continueButton,
			"res://art/ui/ru/ContinueRu.png",
			"res://art/ui/en/Continue.png");

		SetButtonTexture(
			_choiceBackButton,
			_backRu,
			"Back.png",
			GameLocalization.Language == "en");
	}

	private void UpdateSkipTexture()
	{
		if (_skipButton == null)
			return;

		SetLocalizedTexture(
			_skipButton,
			"res://art/ui/ru/SkipRu.png",
			"res://art/ui/en/Skip.png");
	}

	private bool IsFullscreen()
	{
		return DisplayServer.WindowGetMode() is
			DisplayServer.WindowMode.Fullscreen or
			DisplayServer.WindowMode.ExclusiveFullscreen;
	}

	private void UpdateFullscreenTexture()
	{
		if (_fullscreenButton == null)
			return;

		bool english =
			GameLocalization.Language == "en";

		bool enabled = IsFullscreen();

		_fullscreenButton.SetPressedNoSignal(enabled);

		string path;

		if (english)
		{
			path = enabled
				? "res://art/ui/en/Fullscreen.png"
				: "res://art/ui/en/Fullscreen2.png";
		}
		else
		{
			path = enabled
				? "res://art/ui/ru/FullScreenRu.png"
				: "res://art/ui/ru/FullScreenRu2.png";
		}

		if (ResourceLoader.Exists(path))
		{
			_fullscreenButton.TextureNormal =
				GD.Load<Texture2D>(path);
		}
		else
		{
			GD.PushWarning(
				$"Fullscreen texture not found: {path}");
		}
	}

	private bool HasSavedGame()
	{
		return Godot.FileAccess.FileExists(
			ProgressSavePath);
	}

	private void UpdateContinueButton()
	{
		bool hasSave = HasSavedGame();

		_continueButton.Disabled = !hasSave;

		_continueButton.Modulate = hasSave
			? Colors.White
			: new Color(1, 1, 1, 0.45f);
	}

	private void SetMainButtonsVisible(bool visible)
	{
		_playButton.Visible = visible;
		_settingsButton.Visible = visible;
		_quitButton.Visible = visible;
	}

	private void SetChoiceButtonsVisible(bool visible)
	{
		_newGameButton.Visible = visible;
		_continueButton.Visible = visible;
		_choiceBackButton.Visible = visible;
	}

	private void ShowMainMenu()
	{
		_showingGameChoice = false;

		_settingsMenu.Hide();
		_settingsFrame.Hide();

		SetChoiceButtonsVisible(false);
		SetMainButtonsVisible(true);

		_title.Show();
		_mainMenu.Show();
	}

	private void ShowGameChoice()
	{
		_showingGameChoice = true;

		_settingsMenu.Hide();
		_settingsFrame.Hide();

		MatchChoiceButtonSizes();

		SetMainButtonsVisible(false);
		SetChoiceButtonsVisible(true);

		_title.Show();
		_mainMenu.Show();

		UpdateChoiceTextures();
		UpdateContinueButton();
	}

	public void OnPlayPressed()
	{
		if (_startingGame || ActiveDialogue != null)
			return;

		ShowGameChoice();
	}

	private void OnChoiceBackPressed()
	{
		if (_startingGame)
			return;

		ShowMainMenu();
	}

	private void OnNewGamePressed()
	{
		if (_startingGame || ActiveDialogue != null)
			return;

		_mainMenu.Hide();
		_settingsMenu.Hide();
		_settingsFrame.Hide();
		_title.Hide();

		StartIntroDialogue();
	}

	private void StartIntroDialogue()
	{
		ActiveDialogue = new DialoguePlayer();

		ActiveDialogue.Finished += OnIntroFinished;
		ActiveDialogue.Cancelled += CloseDialogue;

		AddChild(ActiveDialogue);

		MoveChild(
			_skipLayer,
			GetChildCount() - 1);

		_skipLayer.Show();

		_skipButton.MouseFilter =
			Control.MouseFilterEnum.Stop;

		_skipMouseWasDown =
			Input.IsMouseButtonPressed(MouseButton.Left);
	}

	private void OnIntroFinished()
	{
		if (_startingGame)
			return;

		FinishIntroAndStartGame();
	}

	private void OnSkipIntroPressed()
	{
		if (_startingGame || ActiveDialogue == null)
			return;

		FinishIntroAndStartGame();
	}

	private void FinishIntroAndStartGame()
	{
		if (_startingGame)
			return;

		_skipLayer.Hide();

		if (ActiveDialogue != null)
		{
			var dialogue = ActiveDialogue;

			ActiveDialogue = null!;
			dialogue.QueueFree();
		}

		EnterGame(true);
	}

	private void OnContinuePressed()
	{
		if (_startingGame || !HasSavedGame())
			return;

		EnterGame(false);
	}

	private void EnterGame(bool newGame)
	{
		if (_startingGame)
			return;

		// An empty Inspector override must not send a completed intro back to the menu.
		GameScene ??= GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
		if (GameScene == null)
		{
			GD.PushError("Could not load res://scenes/trade_scene.tscn");
			ShowGameChoice();
			return;
		}

		if (newGame && HasSavedGame())
		{
			string path = ProjectSettings.GlobalizePath(
				ProgressSavePath);

			Error deleteError =
				DirAccess.RemoveAbsolute(path);

			if (deleteError != Error.Ok)
			{
				GD.PushError(
					$"Could not reset saved game: {deleteError}");

				ShowGameChoice();
				return;
			}
		}

		_startingGame = true;

		_skipLayer.Hide();
		_mainMenu.Hide();
		_settingsMenu.Hide();
		_settingsFrame.Hide();
		_title.Hide();

		Error error =
			GetTree().ChangeSceneToPacked(GameScene);

		if (error != Error.Ok)
		{
			_startingGame = false;

			GD.PushError(
				$"Could not open game: {error}");

			ShowGameChoice();
		}
	}

	private void CloseDialogue()
	{
		if (ActiveDialogue == null)
			return;

		var dialogue = ActiveDialogue;

		ActiveDialogue = null!;
		dialogue.QueueFree();

		_skipLayer.Hide();

		ShowGameChoice();
	}

	public void OnSettingsPressed()
	{
		_mainMenu.Hide();
		_title.Hide();

		_settingsFrame.Show();
		_settingsMenu.Show();

		UpdateFullscreenTexture();
	}

	public void OnBackPressed()
	{
		ShowMainMenu();
	}

	public void OnQuitPressed()
	{
		GetTree().Quit();
	}

	public void OnFullscreenToggled(bool enabled)
	{
		DisplayServer.WindowSetMode(
			enabled
				? DisplayServer.WindowMode.Fullscreen
				: DisplayServer.WindowMode.Windowed);

		UpdateFullscreenTexture();
	}

	public override void _Notification(int what)
	{
		if (what == NotificationTranslationChanged &&
			IsNodeReady())
		{
			UpdateAllTextures();
		}
	}

	public override void _Process(double delta)
	{
		bool mouseDown =
			Input.IsMouseButtonPressed(MouseButton.Left);

		if (ActiveDialogue != null &&
			_skipLayer.Visible &&
			mouseDown &&
			!_skipMouseWasDown)
		{
			Vector2 mousePosition =
				GetViewport().GetMousePosition();

			Rect2 skipRect =
				_skipButton.GetGlobalRect();

			if (skipRect.HasPoint(mousePosition))
			{
				OnSkipIntroPressed();
			}
		}

		_skipMouseWasDown = mouseDown;
	}

	public override void _Input(InputEvent @event)
	{
		if (ActiveDialogue != null)
		{
			if (_skipLayer.Visible &&
				@event is InputEventMouseButton mouse &&
				mouse.ButtonIndex == MouseButton.Left &&
				mouse.Pressed &&
				_skipButton.GetGlobalRect().HasPoint(
					mouse.Position))
			{
				GetViewport().SetInputAsHandled();

				OnSkipIntroPressed();
			}

			return;
		}

		if (@event is InputEventKey key &&
			key.Pressed &&
			!key.Echo &&
			key.Keycode == Key.Escape)
		{
			if (_showingGameChoice)
			{
				OnChoiceBackPressed();
				return;
			}

			if (_settingsMenu.Visible)
			{
				OnBackPressed();
				return;
			}

			GetTree().Quit();
		}
	}
}
