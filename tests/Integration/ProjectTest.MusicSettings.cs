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
                Check(control.IsVisibleInTree() && Root.GetVisibleRect().Encloses(control.GetGlobalRect()),
                    "Music slider must fit the visible settings panel");
                Check(title.Tr(title.Text) == (language == "en" ? "Music volume" : "Громкость музыки") &&
                      !title.GetGlobalRect().Intersects(percentage.GetGlobalRect()), "Localized label and percentage must not overlap");
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
            menu = GD.Load<PackedScene>("res://scenes/main_menu.tscn").Instantiate<MainMenu>();
            Root.AddChild(menu);
            menu.OnSettingsPressed();
            await ToSignal(this, SignalName.ProcessFrame);
            var slider = menu.GetNode<HSlider>(menuPath + "/Content/Slider");
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
            Check(Paused && pauseSlider.Value == 64, "Pause settings must share the saved menu volume");
            await ClickControl(game, pausePath + "/Content/Slider");
            Check(Paused && Math.Abs(audio.Volume - pauseSlider.Value / 100) < 0.001,
                "Real mouse input must adjust music while gameplay remains paused");
            Check(game.GetNode<GameplayMusicDirector>("GameplayMusicDirector").GetChildren()
                .OfType<AudioStreamPlayer>().All(player => player.Bus == MusicSettings.BusName),
                "Day and upgrade tracks must use the same volume bus");
            await CheckLayout(game, pausePath, "pause-volume");
            pause.Close();
            game.QueueFree(); game = null;
            await ToSignal(this, SignalName.ProcessFrame);
            audio.SetVolume(0.23);
            menu = GD.Load<PackedScene>("res://scenes/main_menu.tscn").Instantiate<MainMenu>();
            Root.AddChild(menu);
            Check(menu.GetNode<HSlider>(menuPath + "/Content/Slider").Value == 23,
                "A recreated menu must show the stored preference");
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
            TranslationServer.SetLocale(oldLanguage);
            Root.Size = oldSize;
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }
}
