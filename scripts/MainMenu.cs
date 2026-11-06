using Godot;

public partial class MainMenu : Control
{
	[Export] public PackedScene GameScene { get; set; } = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");

	private CenterContainer _mainMenu = null!;
	private CenterContainer _settingsMenu = null!;
	private Button _russian = null!;
	private Button _english = null!;
	public DialoguePlayer ActiveDialogue { get; private set; }
	private bool _startingGame;

	public override void _Ready()
	{
		_mainMenu = GetNode<CenterContainer>("MainMenuContainer");
		_settingsMenu = GetNode<CenterContainer>("SettingsMenuContainer");
		GetNode<TextureButton>("MainMenuContainer/Menu/PlayButton").Pressed += OnPlayPressed;
		GetNode<TextureButton>("MainMenuContainer/Menu/SettingsButton").Pressed += OnSettingsPressed;
		GetNode<TextureButton>("MainMenuContainer/Menu/QuitButton").Pressed += OnQuitPressed;
		GetNode<TextureButton>("SettingsMenuContainer/Settings/BackButton").Pressed += OnBackPressed;

		var fullscreenButton = GetNode<CheckButton>("SettingsMenuContainer/Settings/FullscreenButton");
		fullscreenButton.ButtonPressed = DisplayServer.WindowGetMode() is
			DisplayServer.WindowMode.Fullscreen or DisplayServer.WindowMode.ExclusiveFullscreen;
		fullscreenButton.Toggled += OnFullscreenToggled;

		var settings = GetNode<VBoxContainer>("SettingsMenuContainer/Settings");
		var row = new HBoxContainer { Name = "LanguageRow", Alignment = BoxContainer.AlignmentMode.Center };
		row.AddThemeConstantOverride("separation", 12);
		settings.AddChild(row);
		settings.MoveChild(row, settings.GetNode("BackButton").GetIndex());
		var languageLabel = new Label { Name = "LanguageLabel", Text = "Язык", VerticalAlignment = VerticalAlignment.Center };
		languageLabel.AddThemeColorOverride("font_color", new Color(0.16f, 0.12f, 0.1f));
		row.AddChild(languageLabel);
		var group = new ButtonGroup();
		_russian = AddLanguageButton(row, "RussianButton", "Русский", "ru", group);
		_english = AddLanguageButton(row, "EnglishButton", "English", "en", group);
		UpdateLanguageButtons();
	}

	private Button AddLanguageButton(HBoxContainer row, string name, string text, string locale, ButtonGroup group)
	{
		var button = new Button
		{
			Name = name, Text = text, ToggleMode = true, ButtonGroup = group,
			CustomMinimumSize = new Vector2(144, 48), AutoTranslateMode = AutoTranslateModeEnum.Disabled
		};
		row.AddChild(button);
		button.Pressed += () =>
		{
			Error error = GameLocalization.SelectLanguage(locale);
			if (error != Error.Ok) GD.PushWarning($"Language preference could not be saved: {error}");
			UpdateLanguageButtons();
		};
		return button;
	}

	private void UpdateLanguageButtons()
	{
		if (_russian == null || _english == null) return;
		_russian.SetPressedNoSignal(GameLocalization.Language == "ru");
		_english.SetPressedNoSignal(GameLocalization.Language == "en");
	}

	public override void _Notification(int what)
	{
		if (what == NotificationTranslationChanged) UpdateLanguageButtons();
	}

	public override void _Input(InputEvent @event)
	{
		if (ActiveDialogue != null) return;
		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
			GetTree().Quit();
	}

	public void OnPlayPressed()
	{
		if (ActiveDialogue != null || _startingGame) return;
		_mainMenu.Hide();
		_settingsMenu.Hide();
		ActiveDialogue = new DialoguePlayer();
		ActiveDialogue.Finished += () =>
		{
			if (_startingGame) return;
			_startingGame = true;
			var error = GetTree().ChangeSceneToPacked(GameScene);
			if (error != Error.Ok)
			{
				_startingGame = false;
				CloseDialogue();
				GD.PushError($"Could not open game: {error}");
			}
		};
		ActiveDialogue.Cancelled += CloseDialogue;
		AddChild(ActiveDialogue);
	}

	private void CloseDialogue()
	{
		ActiveDialogue.QueueFree();
		ActiveDialogue = null;
		_mainMenu.Show();
	}

	public void OnSettingsPressed()
	{
		_mainMenu.Hide();
		_settingsMenu.Show();
	}

	public void OnQuitPressed() => GetTree().Quit();

	public void OnBackPressed()
	{
		_settingsMenu.Hide();
		_mainMenu.Show();
	}

	public void OnFullscreenToggled(bool enabled) => DisplayServer.WindowSetMode(
		enabled ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed);
}
