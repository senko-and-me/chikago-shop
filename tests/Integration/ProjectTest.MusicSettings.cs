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
                var back = control.GetParent().GetNode<Control>("BackButton");
                Check(back.Size.Y > 0 && Root.GetVisibleRect().Encloses(back.GetGlobalRect()),
                    "Adding the sound plaque must keep Back visible and clickable");
                if (host is TradeScene)
                {
                    Check(control.GetThemeStylebox("panel") is StyleBoxFlat plain &&
                          plain.BgColor == ((StyleBoxFlat)fullscreen.GetThemeStylebox("normal")).BgColor &&
                          !control.GetNode<HSlider>("Content/Slider").HasThemeIconOverride("grabber"),
                        "Pause volume must use the fullscreen background and a plain native slider handle");
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
