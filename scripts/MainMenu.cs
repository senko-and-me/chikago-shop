using Godot;

public partial class MainMenu : Control
{
	[Export] public PackedScene GameScene { get; set; } = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");

	private CenterContainer _mainMenu = null!;
	private CenterContainer _settingsMenu = null!;

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
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventKey { Pressed: true, Echo: false, Keycode: Key.Escape })
			GetTree().Quit();
	}

	public void OnPlayPressed() => GetTree().ChangeSceneToPacked(GameScene);

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
