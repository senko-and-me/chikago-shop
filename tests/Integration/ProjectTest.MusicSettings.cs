using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class ProjectTest
{
    private async Task TestMusicSettings()
    {
        Check(OS.GetUserDataDir().Contains("ChikagoShopStartupSmokeMenuMusic"), "Music settings checks require isolated userdata");
        var audio = MusicSettings.Instance;
        var effects = SoundEffects.Instance;
        bool hadSettings = FileAccess.FileExists(GameLocalization.SettingsPath);
        byte[] backup = hadSettings ? FileAccess.GetFileAsBytes(GameLocalization.SettingsPath) : null;
        var oldSize = Root.Size;
        string oldLanguage = GameLocalization.Language;
        var oldWindowMode = DisplayServer.WindowGetMode();
        MainMenu menu = null;
        TradeScene game = null;
        const string pausePath = "PauseMenu/Root/Overlay/Center/Panel/Content/Settings/MusicVolume";
        const string menuPath = "SettingsMenuContainer/Settings/MusicVolume";

        async Task CheckLayout(Node host, string path, string name)
        {
            foreach (var size in new[] { new Vector2I(960, 540), new Vector2I(1280, 720) })
            foreach (string language in new[] { "ru", "en" })
            {
                Root.Size = size;
                GameLocalization.SelectLanguage(language);
                await ToSignal(this, SignalName.ProcessFrame);
                await ToSignal(this, SignalName.ProcessFrame);
                var control = host.GetNode<MusicVolumeControl>(path);
                var title = control.GetNode<Label>("Content/Header/Title");
                var percentage = control.GetNode<Label>("Content/Header/Percentage");
                var sound = host.GetNode<MusicVolumeControl>(path.Replace("/MusicVolume", "/SoundVolume"));
                var soundTitle = sound.GetNode<Label>("Content/Header/Title");
                Check(control.IsVisibleInTree() && Root.GetVisibleRect().Encloses(control.GetGlobalRect()),
                    "Music slider must fit the visible settings panel");
                var fullscreen = control.GetParent().GetNode<Control>("FullscreenButton");
                var settings = control.GetParent();
                var fullscreenCaption = fullscreen.GetNode<Label>("Caption");
                var backCaption = host is MainMenu ? settings.GetNode<Control>("BackButton/Caption") : settings.GetNode<Control>("BackButton");
                foreach (Control caption in new Control[] { title, percentage, fullscreenCaption, backCaption,
                    settings.GetNode<Control>("LanguageRow/LanguageLabel"), settings.GetNode<Control>("LanguageRow/EnglishButton"),
                    settings.GetNode<Control>("LanguageRow/RussianButton") })
                    Check(caption.GetThemeFontSize("font_size") == 24,
                        $"Every settings caption must use the same font size, including texture buttons: {caption.GetPath()}");
                Check(fullscreenCaption.Tr(fullscreenCaption.Text) == (language == "en" ? "Fullscreen" : "Полный экран") &&
                    fullscreenCaption.GetMinimumSize().X <= fullscreenCaption.Size.X,
                    "The fixed-size fullscreen caption must translate and fit beside the drawn toggle");
                Check(Math.Abs(control.GetGlobalRect().Position.X - fullscreen.GetGlobalRect().Position.X) < 1 &&
                      Math.Abs(control.Size.X - fullscreen.Size.X) < 1,
                    "Music volume must align with the fullscreen control and match its width in both menus");
                Check(sound.Size.IsEqualApprox(control.Size) &&
                      Math.Abs(sound.GetGlobalRect().Position.X - control.GetGlobalRect().Position.X) < 1 &&
                      !sound.GetGlobalRect().Intersects(control.GetGlobalRect()) &&
                      Root.GetVisibleRect().Encloses(sound.GetGlobalRect()),
                    "Sound and music plaques must have identical dimensions, alignment and spacing without overlap");
                Check(sound.GetNode<HSlider>("Content/Slider").GetThemeIcon("grabber") ==
                      control.GetNode<HSlider>("Content/Slider").GetThemeIcon("grabber"),
                    "Sound and music plaques must use the same slider artwork");
                var backControl = control.GetParent().GetNode<Control>("BackButton");
                Check(backControl.Size.Y > 0 && Root.GetVisibleRect().Encloses(backControl.GetGlobalRect()),
                    "Adding the sound plaque must keep Back visible and clickable");
                if (host is TradeScene)
                {
                    Check(control.GetThemeStylebox("panel") is StyleBoxFlat plain &&
                          plain.BgColor == new Color(0.94f, 0.91f, 0.85f) &&
                          !control.GetNode<HSlider>("Content/Slider").HasThemeIconOverride("grabber"),
                        "Pause volume must retain its compact style and plain slider handle");
                    var toggle = fullscreen as FullscreenToggle;
                    bool enabled = DisplayServer.WindowGetMode() is DisplayServer.WindowMode.Fullscreen or DisplayServer.WindowMode.ExclusiveFullscreen;
                    Check(toggle != null && toggle.TextureNormal.ResourcePath ==
                        $"res://art/ui/{language}/fullscreen_{(enabled ? "on" : "off")}.png" &&
                        toggle.ButtonPressed == enabled && Root.GetVisibleRect().Encloses(toggle.GetGlobalRect()),
                        "Pause fullscreen must use the main menu's localized artwork and actual on/off state");
                }
                else
                {
                    var back = host.GetNode<TextureButton>("SettingsMenuContainer/Settings/BackButton");
                    Check(back.GetNode<Label>("Caption").Tr(back.GetNode<Label>("Caption").Text) ==
                        (language == "en" ? "Back" : "Назад") && settings.GetNode<Label>("SettingsTitle").GetThemeFontSize("font_size") == 40,
                        "Back uses the common caption size while the settings heading keeps its original size");
                    var languages = control.GetParent().GetNode<HBoxContainer>("LanguageRow");
                    Check(back.IsVisibleInTree() && back.TextureNormal != null && back.Size.Y >= 60 &&
                        Root.GetVisibleRect().Encloses(back.GetGlobalRect()) &&
                        !back.GetGlobalRect().Intersects(languages.GetGlobalRect()),
                        "The textured Back button must fit below language selection at both window sizes");
                    Check(languages.GetNode<Button>("RussianButton").Text == "Русский" &&
                        languages.GetNode<Button>("EnglishButton").GetThemeStylebox("normal") is StyleBoxTexture,
                        "Scene-authored language controls must use paper textures and correct text");
                }
                Check(title.Tr(title.Text) == (language == "en" ? "Music volume" : "Громкость музыки") &&
                      !title.GetGlobalRect().Intersects(percentage.GetGlobalRect()), "Localized label and percentage must not overlap");
                Check(soundTitle.Tr(soundTitle.Text) == (language == "en" ? "Sound volume" : "Громкость звуков") &&
                      !soundTitle.GetGlobalRect().Intersects(sound.GetNode<Label>("Content/Header/Percentage").GetGlobalRect()),
                    "Sound volume label and percentage must localize and fit");
                if (DisplayServer.GetName() != "headless")
                {
                    using var image = await CapturePresentation();
                    image.SavePng($"res://{name}-{language}-{size.X}.png");
                }
                if (host is MainMenu main)
                {
                    await ClickControl(main, "SettingsMenuContainer/Settings/BackButton");
                    Check(!main.GetNode<Control>("SettingsMenuContainer").Visible &&
                        main.GetNode<Control>("MainMenuContainer").Visible,
                        "A real Back click must return from settings to the main menu");
                    main.OnSettingsPressed();
                    await ToSignal(this, SignalName.ProcessFrame);
                }
            }
        }

        try
        {
            audio.SetVolume(1);
            effects.SetVolume(1);
            menu = GD.Load<PackedScene>("res://scenes/main_menu.tscn").Instantiate<MainMenu>();
            Root.AddChild(menu);
            menu.OnSettingsPressed();
            await ToSignal(this, SignalName.ProcessFrame);
            var slider = menu.GetNode<HSlider>(menuPath + "/Content/Slider");
            var soundSlider = menu.GetNode<HSlider>("SettingsMenuContainer/Settings/SoundVolume/Content/Slider");
            int bus = AudioServer.GetBusIndex(MusicSettings.BusName);
            Check(slider.Value == 100 && menu.GetNode<AudioStreamPlayer>("MenuMusic").Bus == MusicSettings.BusName,
                "Menu music must use the user volume bus");
            Check(menu.GetNode<IntroMusicDirector>("MusicDirector").GetChildren().OfType<AudioStreamPlayer>()
                .All(player => player.Bus == MusicSettings.BusName), "Every prologue track must use the music volume bus");
            await ClickControl(menu, menuPath + "/Content/Slider");
            Check(slider.Value > 35 && slider.Value < 65, "A real mouse click on the track must change volume");
            slider.GrabFocus();
            double before = slider.Value;
            Root.PushInput(new InputEventKey { Pressed = true, Keycode = Key.Right }, true);
            Root.PushInput(new InputEventKey { Pressed = false, Keycode = Key.Right }, true);
            await ToSignal(this, SignalName.ProcessFrame);
            Check(slider.Value > before, "Keyboard arrows must adjust the focused slider");
            slider.Value = 37;
            Check(Math.Abs(Mathf.DbToLinear(AudioServer.GetBusVolumeDb(bus)) - 0.37) < 0.001,
                "User volume must immediately control the music bus gain");
            slider.Value = 0;
            Check(AudioServer.IsBusMute(bus) && menu.GetNode<AudioStreamPlayer>("MenuMusic").Playing,
                "Zero must mute music completely without stopping playback");
            slider.Value = 64;
            Check(!AudioServer.IsBusMute(bus), "Raising volume must unmute the bus");
            GameLocalization.SelectLanguage("en");
            var stored = new ConfigFile();
            Check(stored.Load(GameLocalization.SettingsPath) == Error.Ok &&
                  stored.GetValue("audio", "volume").AsDouble() == 0.64 &&
                  stored.GetValue("interface", "language").AsString() == "en", "Both settings must be written to disk");
            stored.SetValue("audio", "volume", 0.42);
            stored.Save(GameLocalization.SettingsPath);
            audio.LoadPreference();
            Check(audio.Volume == 0.42 && slider.Value == 42, "Reload must read persisted volume and synchronize the control");
            slider.Value = 64;
            audio.LoadPreference();
            Check(audio.Volume == 0.64 && GameLocalization.Language == "en", "Volume and language must persist independently");
            int effectsBus = AudioServer.GetBusIndex(SoundEffects.BusName);
            soundSlider.Value = 0;
            Check(AudioServer.IsBusMute(effectsBus) && !AudioServer.IsBusMute(bus) && audio.Volume == 0.64,
                "Zero sound volume must mute all effects independently of music");
            soundSlider.Value = 53;
            Check(!AudioServer.IsBusMute(effectsBus) &&
                  Math.Abs(Mathf.DbToLinear(AudioServer.GetBusVolumeDb(effectsBus)) - 0.53) < 0.001,
                "The sound slider must immediately change the SFX bus");
            audio.SetVolume(0);
            Check(!AudioServer.IsBusMute(effectsBus) && AudioServer.IsBusMute(bus),
                "Music mute must remain independent of the sound volume slider");
            audio.SetVolume(0.64);
            var savedAudio = new ConfigFile();
            Check(savedAudio.Load(GameLocalization.SettingsPath) == Error.Ok &&
                  savedAudio.GetValue("audio", "effects_volume").AsDouble() == 0.53 &&
                  savedAudio.GetValue("audio", "volume").AsDouble() == 0.64 &&
                  savedAudio.GetValue("interface", "language").AsString() == "en",
                "Both volumes and language must be saved without overwriting each other");
            savedAudio.SetValue("audio", "effects_volume", 0.42);
            savedAudio.Save(GameLocalization.SettingsPath);
            effects.LoadPreference();
            Check(effects.Volume == 0.42 && soundSlider.Value == 42, "Loading sound volume must synchronize the plaque");
            soundSlider.Value = 53;
            await CheckLayout(menu, menuPath, "menu-volume");
            menu.QueueFree(); menu = null;
            await ToSignal(this, SignalName.ProcessFrame);

            game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
            game.SavePath = "user://music_settings_test.json";
            Root.AddChild(game);
            game.SetProcess(false);
            await ToSignal(this, SignalName.ProcessFrame);
            var pause = game.GetNode<PauseMenu>("PauseMenu");
            pause.Open();
            await ClickControl(game, "PauseMenu/Root/Overlay/Center/Panel/Content/Menu/SettingsButton");
            var pauseSlider = game.GetNode<HSlider>(pausePath + "/Content/Slider");
            var pauseSound = game.GetNode<HSlider>(pausePath.Replace("/MusicVolume", "/SoundVolume") + "/Content/Slider");
            Check(Paused && pauseSlider.Value == 64, "Pause settings must share the saved menu volume");
            Check(pauseSound.Value == 53, "Pause sound settings must share the saved menu preference");
            await ClickControl(game, pausePath + "/Content/Slider");
            Check(Paused && Math.Abs(audio.Volume - pauseSlider.Value / 100) < 0.001,
                "Real mouse input must adjust music while gameplay remains paused");
            double musicBeforeSound = audio.Volume;
            await ClickControl(game, pausePath.Replace("/MusicVolume", "/SoundVolume") + "/Content/Slider");
            Check(Paused && effects.Volume == pauseSound.Value / 100 && audio.Volume == musicBeforeSound,
                "The separate sound slider must accept real input in pause without changing music");
            Check(game.GetNode<GameplayMusicDirector>("GameplayMusicDirector").GetChildren()
                .OfType<AudioStreamPlayer>().All(player => player.Bus == MusicSettings.BusName),
                "Day and upgrade tracks must use the same volume bus");
            await CheckLayout(game, pausePath, "pause-volume");
            const string fullscreenPath = "PauseMenu/Root/Overlay/Center/Panel/Content/Settings/FullscreenButton";
            var fullscreenToggle = game.GetNode<FullscreenToggle>(fullscreenPath);
            foreach (string language in new[] { "ru", "en" })
            {
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
                GameLocalization.SelectLanguage(language);
                await ToSignal(CreateTimer(0.15), SceneTreeTimer.SignalName.Timeout);
                await ClickControl(game, fullscreenPath);
                await ToSignal(CreateTimer(0.15), SceneTreeTimer.SignalName.Timeout);
                Check(Paused && DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen &&
                    fullscreenToggle.ButtonPressed && fullscreenToggle.TextureNormal.ResourcePath == $"res://art/ui/{language}/fullscreen_on.png",
                    "A real mouse click must enable fullscreen and show matching artwork without resuming gameplay");
                fullscreenToggle.GrabFocus();
                Root.PushInput(new InputEventKey { Pressed = true, Keycode = Key.Space }, true);
                Root.PushInput(new InputEventKey { Pressed = false, Keycode = Key.Space }, true);
                await ToSignal(CreateTimer(0.15), SceneTreeTimer.SignalName.Timeout);
                Check(Paused && DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Windowed &&
                    !fullscreenToggle.ButtonPressed && fullscreenToggle.TextureNormal.ResourcePath == $"res://art/ui/{language}/fullscreen_off.png",
                    "Keyboard activation must leave fullscreen and update artwork while paused");
            }
            pause.Close();
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
            pause.Open();
            await ClickControl(game, "PauseMenu/Root/Overlay/Center/Panel/Content/Menu/SettingsButton");
            Check(fullscreenToggle.ButtonPressed && fullscreenToggle.TextureNormal.ResourcePath.EndsWith("fullscreen_on.png"),
                "Reopening settings must reflect a window mode changed outside the toggle");
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
            await ToSignal(CreateTimer(0.15), SceneTreeTimer.SignalName.Timeout);
            Check(!fullscreenToggle.ButtonPressed, "Visible toggle must follow external window-mode changes without emitting another toggle");
            pause.Close();
            game.QueueFree(); game = null;
            await ToSignal(this, SignalName.ProcessFrame);
            audio.SetVolume(0.23);
            effects.SetVolume(0.41);
            menu = GD.Load<PackedScene>("res://scenes/main_menu.tscn").Instantiate<MainMenu>();
            Root.AddChild(menu);
            Check(menu.GetNode<HSlider>(menuPath + "/Content/Slider").Value == 23,
                "A recreated menu must show the stored preference");
            Check(menu.GetNode<HSlider>("SettingsMenuContainer/Settings/SoundVolume/Content/Slider").Value == 41,
                "A recreated menu must show the saved sound volume");
        }
        finally
        {
            DisplayServer.WindowSetMode(oldWindowMode);
            Paused = false;
            if (GodotObject.IsInstanceValid(menu)) menu.QueueFree();
            if (GodotObject.IsInstanceValid(game)) game.QueueFree();
            if (hadSettings)
            {
                using var file = FileAccess.Open(GameLocalization.SettingsPath, FileAccess.ModeFlags.Write);
                file.StoreBuffer(backup);
            }
            else DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(GameLocalization.SettingsPath));
            audio.LoadPreference();
            effects.LoadPreference();
            TranslationServer.SetLocale(oldLanguage);
            Root.Size = oldSize;
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }
}
