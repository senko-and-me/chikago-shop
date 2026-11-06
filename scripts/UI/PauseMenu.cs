using Godot;

public partial class PauseMenu : CanvasLayer
{
	private Control _overlay = null!;
	private VBoxContainer _menu = null!;
	private VBoxContainer _settings = null!;
	private Button _resume = null!;
	private Button _openButton = null!;
	private CheckButton _fullscreen = null!;
	private bool _ownsPause;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		Layer = 100;
		var root = new Control { Name = "Root", MouseFilter = Control.MouseFilterEnum.Ignore };
		AddChild(root);
		root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_openButton = new Button { Name = "PauseButton", Text = "Пауза  [Esc]" };
		root.AddChild(_openButton);
		_openButton.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight);
		_openButton.OffsetLeft = -204;
		_openButton.OffsetRight = -24;
		_openButton.OffsetTop = 176;
		_openButton.OffsetBottom = 220;
		_openButton.Pressed += Open;

		_overlay = new Control { Name = "Overlay", Visible = false };
		root.AddChild(_overlay);
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
		_fullscreen.Toggled += enabled => DisplayServer.WindowSetMode(enabled
			? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed);
		AddButton(_settings, "BackButton", "Назад", ShowMenu);
	}

	private static void AddTitle(VBoxContainer parent, string text)
	{
		var title = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center };
		title.AddThemeFontSizeOverride("font_size", 32);
		title.AddThemeColorOverride("font_color", new Color(0.23f, 0.19f, 0.17f));
		parent.AddChild(title);
	}

	private static Button AddButton(VBoxContainer parent, string name, string text, System.Action action)
	{
		var button = new Button { Name = name, Text = text, CustomMinimumSize = new Vector2(0, 48) };
		parent.AddChild(button);
		button.Pressed += action;
		return button;
	}

	public void Open()
	{
		GetParent<TradeScene>().PersistProgress();
		_ownsPause = true;
		GetTree().Paused = true;
		_openButton.Hide();
		_overlay.Show();
		ShowMenu();
	}

	public void Close()
	{
		_ownsPause = false;
		GetTree().Paused = false;
		_overlay.Hide();
		_openButton.Show();
		_openButton.GrabFocus();
	}

	private void ShowSettings()
	{
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
		_resume.GrabFocus();
	}

	public override void _Input(InputEvent @event)
	{
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
