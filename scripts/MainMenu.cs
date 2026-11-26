
using System.Threading.Tasks;
using Godot;

public partial class MainMenu : Control
{
	private const string ProgressSavePath =
		"user://trade_progress.json";

	private const string DefaultGameScenePath = "res://scenes/trade_scene.tscn";
	[Export] public PackedScene GameScene { get; set; }
	private Task<PackedScene> _gameSceneWarmup;
	public bool IsGameScenePrepared => GameScene != null ||
		_gameSceneWarmup is { IsCompletedSuccessfully: true } && _gameSceneWarmup.Result != null;

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
	private bool _endingIntro;
	private IntroMusicDirector Music => GetNode<IntroMusicDirector>("MusicDirector");
	private bool _showingGameChoice;
	private bool _skipMouseWasDown;

	public DialoguePlayer ActiveDialogue { get; private set; } = null!;
	public TradeScene ActiveTrainingGame { get; private set; }

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

		foreach (var button in new[] { _playButton, _settingsButton, _quitButton })
			ButtonDropShadow.Attach(button, opacity: 0.65f);

		_playRu = _playButton.TextureNormal;
		_settingsRu = _settingsButton.TextureNormal;
		_quitRu = _quitButton.TextureNormal;
		_backRu = GD.Load<Texture2D>("res://art/ui/button_back.png");


		_playButton.Pressed += OnPlayPressed;
		_settingsButton.Pressed += OnSettingsPressed;
		_quitButton.Pressed += OnQuitPressed;
		_backButton.Pressed += OnBackPressed;

		_fullscreenButton = GetNode<TextureButton>(
			"SettingsMenuContainer/Settings/FullscreenButton");

		_fullscreenButton.ToggleMode = true;

		_fullscreenButton.SetPressedNoSignal(
			IsFullscreen());

		_fullscreenButton.Toggled += OnFullscreenToggled;

		BindLanguageMenu();
		BuildGameChoiceMenu();
		BuildSkipButton();

		UpdateAllTextures();
		ShowMainMenu();

		_skipMouseWasDown =
			Input.IsMouseButtonPressed(MouseButton.Left);
		if (GameScene == null)
			_gameSceneWarmup = SceneResourceWarmup.LoadAsync(GetTree(), DefaultGameScenePath);
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

	private void BindLanguageMenu()
	{
		var row = GetNode<HBoxContainer>("SettingsMenuContainer/Settings/LanguageRow");
		_languageLabel = row.GetNode<Label>("LanguageLabel");
		_english = row.GetNode<Button>("EnglishButton");
		_russian = row.GetNode<Button>("RussianButton");
		_english.Pressed += () => SelectLanguage("en");
		_russian.Pressed += () => SelectLanguage("ru");
	}

	private void SelectLanguage(string locale)
	{
		Error error = GameLocalization.SelectLanguage(locale);
		if (error != Error.Ok) GD.PushWarning($"Language preference could not be saved: {error}");
		UpdateAllTextures();
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
		ButtonDropShadow.Attach(button, opacity: 0.65f);

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
		ButtonDropShadow.Attach(_skipButton);

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
			"play.png",
			english);

		SetButtonTexture(
			_settingsButton,
			_settingsRu,
			"settings.png",
			english);

		SetButtonTexture(
			_quitButton,
			_quitRu,
			"exit.png",
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
			"res://art/ui/ru/new_game.png",
			"res://art/ui/en/new_game.png");

		SetLocalizedTexture(
			_continueButton,
			"res://art/ui/ru/continue.png",
			"res://art/ui/en/continue.png");

		SetButtonTexture(
			_choiceBackButton,
			_backRu,
			"back.png",
			GameLocalization.Language == "en");
	}

	private void UpdateSkipTexture()
	{
		if (_skipButton == null)
			return;

		SetLocalizedTexture(
			_skipButton,
			"res://art/ui/ru/skip.png",
			"res://art/ui/en/skip.png");
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
				? "res://art/ui/en/fullscreen_on.png"
				: "res://art/ui/en/fullscreen_off.png";
		}
		else
		{
			path = enabled
				? "res://art/ui/ru/fullscreen_on.png"
				: "res://art/ui/ru/fullscreen_off.png";
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

	private void ResumeMenuMusic()
	{
		Music.PlayMenu();
	}

	private void ShowMainMenu()
	{
		ResumeMenuMusic();
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
		ResumeMenuMusic();
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
		if (_startingGame || _endingIntro || ActiveDialogue != null || ActiveTrainingGame != null)
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
		if (_startingGame || _endingIntro || ActiveDialogue != null || ActiveTrainingGame != null)
			return;

		BeginTraining(newGame: true);
	}

	private void HideMenuForSequence()
	{
		_mainMenu.Hide();
		_settingsMenu.Hide();
		_settingsFrame.Hide();
		_title.Hide();
	}

	private async void BeginTraining(bool newGame)
	{
		_startingGame = true;
		if (!await PrepareGameScene()) return;
		await SceneTransition.Run(this, () =>
		{
			if (newGame && !ResetSavedGame()) return Task.FromResult(false);
			HideMenuForSequence();
			var game = GameScene.Instantiate<TradeScene>();
			var tutorial = game.GetNode<FirstTradeTutorial>("FirstTradeTutorial");
			tutorial.ProloguePending = true;
			tutorial.Finished += OnTrainingFinished;
			ActiveTrainingGame = game;
			GetNode<AudioStreamPlayer>("MenuMusic").Stop();
			AddChild(game);
			return Task.FromResult(true);
		});
		if (GodotObject.IsInstanceValid(this)) _startingGame = false;
	}

	private void OnTrainingFinished()
	{
		// Finish the tutorial's own reset before removing its scene and starting the prologue.
		Callable.From(FinishTrainingTransition).CallDeferred();
	}

	private async void FinishTrainingTransition()
	{
		if (!IsInsideTree() || IsQueuedForDeletion() || ActiveTrainingGame == null || _startingGame) return;
		_startingGame = true;
		await SceneTransition.Run(this, () =>
		{
			var game = ActiveTrainingGame;
			ActiveTrainingGame = null;
			game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").Finished -= OnTrainingFinished;
			RemoveChild(game);
			game.QueueFree();
			StartIntroDialogue();
			return Task.FromResult(true);
		});
		if (GodotObject.IsInstanceValid(this)) _startingGame = false;
	}

	private async void ShowPrologueWithTransition()
	{
		_startingGame = true;
		await SceneTransition.Run(this, () =>
		{
			StartIntroDialogue();
			return Task.FromResult(true);
		});
		if (GodotObject.IsInstanceValid(this)) _startingGame = false;
	}

	internal void StartIntroDialogue()
	{
		HideMenuForSequence();
		Music.BeginIntro();
		ActiveDialogue = GD.Load<PackedScene>("res://scenes/UI/DialoguePlayer.tscn").Instantiate<DialoguePlayer>();

		ActiveDialogue.Finished += OnIntroFinished;
		ActiveDialogue.Cancelled += CloseDialogue;
		ActiveDialogue.MusicCueRequested += Music.ApplyIntroCue;

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

		FinishIntroAndStartGame(skipped: true);
	}

	private async void FinishIntroAndStartGame(bool skipped = false)
	{
		if (_startingGame || _endingIntro)
			return;

		_endingIntro = true;
		_skipLayer.Hide();
		ActiveDialogue?.StopInteraction();
		await EnterGame(true, Music.FadeToSilence(skipped, SceneTransition.FadeOutSeconds));
		if (GodotObject.IsInstanceValid(this)) _endingIntro = false;
	}

	private void OnContinuePressed()
	{
		if (_startingGame || _endingIntro || ActiveDialogue != null || ActiveTrainingGame != null || !HasSavedGame())
			return;

		var progress = new ConfigFile();
		if (progress.Load(ProgressSavePath + ".tutorial.cfg") == Error.Ok &&
			progress.GetValue("tutorial", "prologue_pending", false).AsBool())
		{
			if (progress.GetValue("tutorial", "finished", false).AsBool()) ShowPrologueWithTransition();
			else BeginTraining(newGame: false);
			return;
		}
		_ = EnterGame(false);
	}

	private async Task<bool> PrepareGameScene()
	{
		if (!IsInsideTree() || IsQueuedForDeletion()) return false;
		if (GameScene == null)
		{
			_gameSceneWarmup ??= SceneResourceWarmup.LoadAsync(GetTree(), DefaultGameScenePath);
			var prepared = await _gameSceneWarmup;
			if (!GodotObject.IsInstanceValid(this) || !IsInsideTree() || IsQueuedForDeletion()) return false;
			GameScene ??= prepared;
		}
		if (GameScene == null)
		{
			_startingGame = false;
			GD.PushError("Could not load res://scenes/trade_scene.tscn");
			ShowGameChoice();
			return false;
		}
		return true;
	}

	private bool ResetSavedGame()
	{
		if (HasSavedGame())
		{
			string path = ProjectSettings.GlobalizePath(
				ProgressSavePath);

			Error deleteError =
				DirAccess.RemoveAbsolute(path);

			if (deleteError != Error.Ok)
			{
				_startingGame = false;
				GD.PushError(
					$"Could not reset saved game: {deleteError}");

				ShowGameChoice();
				return false;
			}
		}
		return true;
	}

	private async Task EnterGame(bool newGame, Task<bool> silence = null)
	{
		if (_startingGame) return;
		_startingGame = true;
		if (!await PrepareGameScene()) return;
		await SceneTransition.Run(this, async () =>
		{
			if (silence != null && !await silence) return false;
			if (!GodotObject.IsInstanceValid(this) || !IsInsideTree() || IsQueuedForDeletion()) return false;
			if (newGame && !ResetSavedGame()) return false;
			GetNode<AudioStreamPlayer>("MenuMusic").Stop();
			_skipLayer.Hide();
			HideMenuForSequence();
			var game = GameScene.Instantiate();
			if (newGame && game is TradeScene trade)
				trade.GetNode<FirstTradeTutorial>("FirstTradeTutorial").SkipOnInitialize = true;
			Error error = GetTree().ChangeSceneToNode(game);
			if (error == Error.Ok) return true;
			game.Free();
			GD.PushError($"Could not open game: {error}");
			ShowGameChoice();
			return false;
		});
		if (GodotObject.IsInstanceValid(this)) _startingGame = false;
	}

	private async void CloseDialogue()
	{
		if (_endingIntro || ActiveDialogue == null)
			return;

		_endingIntro = true;
		ActiveDialogue.StopInteraction();
		await SceneTransition.Run(this, () =>
		{
			var dialogue = ActiveDialogue;
			ActiveDialogue = null!;
			dialogue.QueueFree();
			_skipLayer.Hide();
			ShowGameChoice();
			return Task.FromResult(true);
		});
		if (GodotObject.IsInstanceValid(this)) _endingIntro = false;
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
		if (SceneTransition.IsActive(GetTree()))
		{
			_skipMouseWasDown = mouseDown;
			return;
		}

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
				_skipButton.EmitSignal(BaseButton.SignalName.Pressed);
			}
		}

		_skipMouseWasDown = mouseDown;
	}

	public override void _Input(InputEvent @event)
	{
		if (SceneTransition.IsActive(GetTree()))
		{
			GetViewport().SetInputAsHandled();
			return;
		}
		if (ActiveTrainingGame != null) return;
		if (_endingIntro || _startingGame)
		{
			GetViewport().SetInputAsHandled();
			return;
		}
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

				_skipButton.EmitSignal(BaseButton.SignalName.Pressed);
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
