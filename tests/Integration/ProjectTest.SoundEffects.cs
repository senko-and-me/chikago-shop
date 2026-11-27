using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class ProjectTest
{
    private async Task TestSoundEffects()
    {
        Check(OS.GetUserDataDir().Contains("ChikagoShopStartupSmokeMenuMusic"), "Sound tests require isolated userdata");
        var effects = SoundEffects.Instance;
        var sounds = new List<SoundEffects.Cue>();
        void Observe(SoundEffects.Cue cue) => sounds.Add(cue);
        effects.Played += Observe;
        MainMenu menu = null;
        TradeScene game = null;
        CanvasLayer fixture = null;
        const string save = "user://sound_effects_test.json";
        bool hadSettings = FileAccess.FileExists(GameLocalization.SettingsPath);
        byte[] backup = hadSettings ? FileAccess.GetFileAsBytes(GameLocalization.SettingsPath) : null;
        string originalLanguage = GameLocalization.Language;

        async Task Settle()
        {
            await ToSignal(this, SignalName.ProcessFrame);
            await ToSignal(this, SignalName.ProcessFrame);
        }
        async Task Move(Vector2 point)
        {
            Root.PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point, Relative = Vector2.One }, true);
            await Settle();
        }
        async Task Press(Vector2 point)
        {
            Root.PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point,
                ButtonIndex = MouseButton.Left, Pressed = true, ButtonMask = MouseButtonMask.Left }, true);
            Root.PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point,
                ButtonIndex = MouseButton.Left, Pressed = false }, true);
            await Settle();
        }
        void OnlyActivation(SoundEffects.Cue cue, string message) => Check(
            sounds.Where(sound => sound != SoundEffects.Cue.Hover).SequenceEqual(new[] { cue }) ||
            cue == SoundEffects.Cue.Click && sounds.SequenceEqual(new[] { SoundEffects.Cue.Hover }),
            message + $" (heard: {string.Join(",", sounds)})");
        bool UiBusy() => new[] { "Hover", "Click", "Settings", "Sale" }.Any(name => effects.GetNode<AudioStreamPlayer>(name).Playing);
        async Task WaitUi()
        {
            ulong deadline = Time.GetTicksMsec() + 3000;
            while (UiBusy())
            {
                Check(Time.GetTicksMsec() < deadline, "UI one-shots must finish naturally");
                await ToSignal(this, SignalName.ProcessFrame);
            }
        }
        async Task CreateTrade()
        {
            game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
            game.SavePath = save;
            game.GetNode<FirstTradeTutorial>("FirstTradeTutorial").ProgressPath = "";
            Root.AddChild(game);
            game.SetProcess(false);
            game.GetNode<WorldCamera>("Camera2D").SetProcess(false);
            await Settle();
        }
        void CraftSoundFixture()
        {
            game.Crafting.OpenCatalog("atelier");
            Check(game.Crafting.Select("hat"), "The sound fixture must select an available item from the catalog");
            var embroidery = game.GetNode<EmbroideryGame>("EmbroideryGame");
            embroidery.Activate();
            embroidery.FinishForTesting(true);
            embroidery.Activate();
            if (game.StoredItems.Length > 0)
            {
                SelectInventorySlot(game, 0);
                game.PlaceItemInShowcase(0);
            }
            Check((game.ShowcaseItems.Any(item => item >= 0) || game.TradeActive) && !Paused,
                "Collect the fixture item without spending the waiting buyer's full patience");
        }

        try
        {
            effects.SetVolume(1);
            var authored = GD.Load<PackedScene>("res://scenes/Audio/SoundEffects.tscn").Instantiate<SoundEffects>();
            try
            {
                Check(authored.GetChildren().OfType<AudioStreamPlayer>().Count() == Enum.GetValues<SoundEffects.Cue>().Length,
                    "Every effect must be authored as an editable player before the scene enters the tree");
                foreach (var cue in Enum.GetValues<SoundEffects.Cue>())
                {
                    var source = authored.GetNode<AudioStreamPlayer>(cue.ToString());
                    var live = effects.GetNode<AudioStreamPlayer>(cue.ToString());
                    Check(live.VolumeDb == source.VolumeDb && live.Stream == source.Stream && live.Bus == SoundEffects.BusName,
                        "Runtime setup must preserve each editor-authored sound and its base volume");
                }
                float baseClickDb = effects.GetNode<AudioStreamPlayer>("Click").VolumeDb;
                GD.Print($"SFX_EDITOR_VOLUME_CHECK: Click={baseClickDb}dB from the saved scene");
                effects.SetVolume(0.25);
                Check(effects.GetNode<AudioStreamPlayer>("Click").VolumeDb == baseClickDb &&
                      Math.Abs(Mathf.DbToLinear(AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex(SoundEffects.BusName))) - 0.25) < 0.001,
                    "The user's volume must multiply the sound bus without overwriting the developer's base mix");
                effects.SetVolume(1);
            }
            finally { authored.Free(); }
            foreach (var (file, hash) in new[] {
                ("sale", "C551903A4D9C339C901CB46BF1FA65B2DCE3270AD91EF86DAAF179DC37A7F747"),
                ("hover", "025A7DCAB4B9812642C1015EDB153E1C2612F78EC9C013526AB6DF3229054A94"),
                ("customer_bell", "B0C1B0E9BEB015260B53BC62878646DD0BB6FB1B8E0EFDED393A3B6A86808EDB"),
                ("settings", "1C9E9B0A55D4D6FB94332D7DAFECE4E4DB437EC133CA2E9F7052076E1FAEE199"),
                ("click", "3F48CF0A142FB4F11D3487B107DB514DD4FC46034724D6379BFFFB27FE311FDD"),
                ("reroll_success", "A3781C4CF59B15C66CC3E3088188833C7E11E4A0C7F9376EF4C0FD77950FE40D"),
                ("reroll_failure", "485ABFFB20623403A204107812EAF06F9A764BB3CE01F25058CBDAC88D7B2163") })
            {
                string path = $"res://audio/sfx/{file}.wav";
                Check(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(FileAccess.GetFileAsBytes(path))) == hash,
                    "SFX must match the approved WAV files without modifying their audio");
                var stream = GD.Load<AudioStreamWav>(path);
                Check(stream.LoopMode == AudioStreamWav.LoopModeEnum.Disabled && stream.Data.Length > 0 && stream.GetLength() > 0,
                    "Every sound effect must be imported as a playable one-shot");
            }
            Check(effects.GetNode<AudioStreamPlayer>("Click").Stream.ResourcePath == "res://audio/sfx/click.wav" &&
                  effects.GetNode<AudioStreamPlayer>("Hover").Stream.ResourcePath == "res://audio/sfx/hover.wav",
                "Hover and click must retain separate roles without a timed merge window");
            fixture = new CanvasLayer { Layer = 500 };
            Root.AddChild(fixture);
            var button = new Button { Name = "SoundProbe", Text = "Sound", Position = new Vector2(80, 100), Size = new Vector2(240, 70) };
            fixture.AddChild(button);
            await Settle();
            sounds.Clear();
            button.GrabFocus();
            button.EmitSignal(Control.SignalName.MouseEntered);
            await Settle();
            Check(sounds.Count == 0, "Focus and synthetic hover signals must not create pointer sounds");
            Vector2 point = button.GetGlobalRect().GetCenter();
            int activations = 0;
            button.Pressed += () => activations++;
            Root.PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point, Relative = Vector2.One }, true);
            await Press(point);
            Check(sounds.SequenceEqual(new[] { SoundEffects.Cue.Click }) && activations == 1,
                "A same-frame entry/click must activate immediately with one click sound");
            await WaitUi();
            await Move(new Vector2(600, 100));
            sounds.Clear();
            await Move(point);
            Check(sounds.SequenceEqual(new[] { SoundEffects.Cue.Hover }), "Hover must start immediately on entry");
            await Press(point);
            Check(sounds.SequenceEqual(new[] { SoundEffects.Cue.Hover }) && activations == 2 &&
                  effects.GetNode<AudioStreamPlayer>("Hover").Playing,
                "Clicking during hover must execute the action but neither interrupt nor restart audio");
            var second = new Button { Name = "SecondProbe", Text = "Second", Position = new Vector2(380, 100), Size = new Vector2(160, 70) };
            fixture.AddChild(second);
            int secondActivations = 0;
            second.Pressed += () => secondActivations++;
            Vector2 secondPoint = second.GetGlobalRect().GetCenter();
            await Move(secondPoint); await Press(secondPoint);
            Check(sounds.SequenceEqual(new[] { SoundEffects.Cue.Hover }) && secondActivations == 1,
                "Another button must obey the shared UI voice limit while its action still works");
            await WaitUi();
            Check(sounds.SequenceEqual(new[] { SoundEffects.Cue.Hover }), "Skipped sounds must never start later from a queue");
            sounds.Clear();
            await Press(secondPoint);
            Check(sounds.SequenceEqual(new[] { SoundEffects.Cue.Click }), "After natural completion the next click must play normally");
            await Move(new Vector2(600, 100)); await Move(point); await Press(point);
            Check(sounds.SequenceEqual(new[] { SoundEffects.Cue.Click }) && activations == 3,
                "While Click plays, both a new hover and another click must be skipped without blocking actions");
            await WaitUi();
            sounds.Clear();
            button.GrabFocus();
            Root.PushInput(new InputEventKey { Keycode = Key.Space, Pressed = true }, true);
            Root.PushInput(new InputEventKey { Keycode = Key.Space, Pressed = false }, true);
            await Settle();
            Check(sounds.SequenceEqual(new[] { SoundEffects.Cue.Click }) && activations == 4,
                "Keyboard activation after audio completion must remain audible and functional");
            await WaitUi();
            await Move(new Vector2(600, 100));
            sounds.Clear();
            button.Name = "SettingsButton";
            await Move(point); await Press(point);
            Check(sounds.SequenceEqual(new[] { SoundEffects.Cue.Hover, SoundEffects.Cue.Settings }) &&
                  !effects.GetNode<AudioStreamPlayer>("Hover").Playing,
                "Settings must retain its own higher-priority sound without overlap with hover");
            sounds.Clear();
            button.Name = "SoundProbe";
            await Move(new Vector2(600, 100)); await Move(point); await Press(point);
            Check(sounds.Count == 0, "Ordinary sounds must wait until the special UI cue finishes too");
            await WaitUi();
            await Move(new Vector2(600, 100)); await Move(point);
            sounds.Clear();
            button.Hide();
            await WaitUi();
            Check(sounds.Count == 0, "Hiding a control must not enqueue new audio");
            button.Show();
            await ToSignal(CreateTimer(0.1), SceneTreeTimer.SignalName.Timeout);
            Check(sounds.Count == 0, "Showing a control under a stationary pointer must remain silent");
            await Move(new Vector2(600, 100));
            sounds.Clear();
            Root.PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point, Relative = Vector2.One }, true);
            Root.PushInput(new InputEventMouseMotion { Position = new Vector2(600, 100), GlobalPosition = new Vector2(600, 100), Relative = Vector2.One }, true);
            await Settle();
            Check(sounds.Count == 0, "Passing over a button entirely between frames must not queue a sound");
            button.Disabled = true;
            await Move(point); await Press(point);
            Check(sounds.Count == 0, "Disabled controls must remain silent");
            fixture.QueueFree(); fixture = null;
            await Settle();

            menu = GD.Load<PackedScene>("res://scenes/main_menu.tscn").Instantiate<MainMenu>();
            Root.AddChild(menu);
            await Settle(); sounds.Clear();
            await ClickControl(menu, "MainMenuContainer/Menu/SettingsButton");
            OnlyActivation(SoundEffects.Cue.Settings, "Main menu settings must use the gear sound instead of generic click");
            sounds.Clear();
            await Settle();
            Check(sounds.Count == 0, "A new menu appearing under a stationary pointer must not play hover");
            await WaitUi();
            sounds.Clear();
            await ClickControl(menu, "SettingsMenuContainer/Settings/LanguageRow/EnglishButton");
            OnlyActivation(SoundEffects.Cue.Click, "Ordinary main menu controls must use generic click");
            sounds.Clear();
            await WaitUi();
            await ClickControl(menu, "SettingsMenuContainer/Settings/BackButton");
            OnlyActivation(SoundEffects.Cue.Click, "Restored main settings Back must be clickable and sounded");
            menu.GetNode<IntroMusicDirector>("MusicDirector").SetProcess(false);
            menu.OnPlayPressed();
            menu.GetNode<TextureButton>("MainMenuContainer/Menu/NewGameButton").EmitSignal(BaseButton.SignalName.Pressed);
            await SkipStartupTraining(menu);
            await Settle();
            var skip = menu.GetNode<TextureButton>("IntroSkipLayer/SkipButton");
            await WaitUi();
            sounds.Clear();
            await Move(new Vector2(600, 100));
            Vector2 skipPoint = skip.GetGlobalTransformWithCanvas() * (skip.Size / 2);
            await Move(skipPoint);
            await WaitUi();
            Check(sounds.SequenceEqual(new[] { SoundEffects.Cue.Hover }),
                $"Prologue Skip must support immediate hover (heard: {string.Join(",", sounds)}, target: {Root.GuiGetHoveredControl()?.GetPath()}, rect: {skip.GetGlobalRect()})");
            sounds.Clear();
            await Press(skipPoint);
            OnlyActivation(SoundEffects.Cue.Click, "The manual Skip input path must play or merge one ordinary activation");
            Check(!menu.GetNode<CanvasLayer>("IntroSkipLayer").Visible &&
                  menu.GetNode<IntroMusicDirector>("MusicDirector").CurrentCue == "silence",
                "A sounded Skip must still start exactly one existing fade-out transition");
            menu.QueueFree(); menu = null;
            await Settle();
            await WaitForSceneFade();

            menu = GD.Load<PackedScene>("res://scenes/main_menu.tscn").Instantiate<MainMenu>();
            Root.AddChild(menu);
            menu.GetNode<IntroMusicDirector>("MusicDirector").SetProcess(false);
            menu.OnPlayPressed();
            menu.GetNode<TextureButton>("MainMenuContainer/Menu/NewGameButton").EmitSignal(BaseButton.SignalName.Pressed);
            await SkipStartupTraining(menu);
            await Settle();
            skip = menu.GetNode<TextureButton>("IntroSkipLayer/SkipButton");
            await WaitUi();
            skipPoint = skip.GetGlobalTransformWithCanvas() * (skip.Size / 2);
            sounds.Clear();
            Root.PushInput(new InputEventMouseMotion { Position = skipPoint, GlobalPosition = skipPoint, Relative = Vector2.One }, true);
            await Press(skipPoint);
            await WaitUi();
            Check(sounds.SequenceEqual(new[] { SoundEffects.Cue.Click }),
                "Fast Skip must emit exactly one click and cancel hover without a late sound");
            menu.QueueFree(); menu = null;
            await Settle();
            await WaitForSceneFade();

            if (FileAccess.FileExists(save)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(save));
            await CreateTrade();
            sounds.Clear();
            game.EndDay(); // Start and bring in the first buyer.
            Check(sounds.SequenceEqual(new[] { SoundEffects.Cue.CustomerBell }),
                "The first customer must ring once at Start");
            CraftSoundFixture();
            sounds.Clear();
            game._Process(0);
            game.OnShowcasePressed(0);
            Check(game.TradeActive && !sounds.Contains(SoundEffects.Cue.CustomerBell),
                "Serving the customer who is already waiting must not ring a second time");
            sounds.Clear();
            game.RerollsRemaining = 100;
            int initialOffer = game.OfferedPrice;
            game.RerollCustomer();
            void CheckRerollSound(int previousOffer)
            {
                int baseline = game.GetItemPrice(game.ItemIndex);
                if (game.OfferedPrice == previousOffer || game.OfferedPrice == baseline)
                    Check(sounds.Count == 0, "An unavailable reroll or neutral offer must not play a result");
                else
                    OnlyActivation(game.OfferedPrice > baseline ? SoundEffects.Cue.RerollSuccess : SoundEffects.Cue.RerollFailure,
                        "Refusal must sound the NEW offer's result without bell or duplicate click");
            }
            CheckRerollSound(initialOffer);
            bool heardSuccess = false, heardFailure = false;
            for (int attempt = 0; attempt < 80 && !(heardSuccess && heardFailure); attempt++)
            {
                sounds.Clear();
                int previousOffer = game.OfferedPrice, previousRerolls = game.RerollsRemaining;
                await ClickControl(game, "World/TradingRoom/Item/TradePanel/Content/Buttons/DeclineButton");
                Check(game.RerollsRemaining == previousRerolls - 1, "A real refusal click must reach the existing reroll action");
                CheckRerollSound(previousOffer);
                heardSuccess |= sounds.Contains(SoundEffects.Cue.RerollSuccess);
                heardFailure |= sounds.Contains(SoundEffects.Cue.RerollFailure);
            }
            Check(heardSuccess && heardFailure, "Real refusal clicks must exercise both profitable and unprofitable offers");
            int remaining = game.RerollsRemaining;
            game.RerollsRemaining = 0;
            sounds.Clear();
            game.RerollCustomer();
            Check(sounds.Count == 0, "A blocked reroll must not play a result");
            game.RerollsRemaining = remaining;
            var pause = game.GetNode<PauseMenu>("PauseMenu");
            pause.Open();
            sounds.Clear();
            await ClickControl(game, "PauseMenu/Root/Overlay/Center/Panel/Content/Menu/SettingsButton");
            OnlyActivation(SoundEffects.Cue.Settings, "Paused settings must use the same gear sound");
            Check(Paused && effects.GetNode<AudioStreamPlayer>("Settings").Playing,
                "Sound effects must actually play while gameplay is paused");
            MusicSettings.Instance.SetVolume(0);
            await WaitUi();
            sounds.Clear();
            await ClickControl(game, "PauseMenu/Root/Overlay/Center/Panel/Content/Settings/BackButton");
            OnlyActivation(SoundEffects.Cue.Click, "Pause Back must use generic click");
            Check(effects.GetNode<AudioStreamPlayer>("Click").Bus == SoundEffects.BusName &&
                  !AudioServer.IsBusMute(AudioServer.GetBusIndex(SoundEffects.BusName)),
                "Music mute must not mute the separate effects bus");
            pause.Close();
            sounds.Clear();
            int accepted = game.Accepted;
            await ClickControl(game, "World/TradingRoom/Item/TradePanel/Content/Buttons/AcceptButton");
            OnlyActivation(SoundEffects.Cue.Sale, "The green sell button must use sale sound without a duplicate click");
            Check(!game.TradeActive && game.Accepted == accepted + 1, "The real sell button must still complete one sale");
            sounds.Clear();
            game.GetNode<Button>("World/TradingRoom/Item/TradePanel/Content/Buttons/AcceptButton").EmitSignal(BaseButton.SignalName.Pressed);
            Check(sounds.Count == 0, "A hidden completed sale must not play another effect");

            CraftSoundFixture();
            sounds.Clear(); game._Process(0);
            game.OnShowcasePressed(0);
            Check(game.TradeActive && sounds.SequenceEqual(new[] { SoundEffects.Cue.CustomerBell }),
                "The next natural buyer must ring once after the previous sale");
            game.PersistProgress();
            game.QueueFree(); game = null; await Settle();
            sounds.Clear();
            await CreateTrade();
            Check(game.TradeActive && !sounds.Any(cue => cue is SoundEffects.Cue.CustomerBell or
                      SoundEffects.Cue.RerollSuccess or SoundEffects.Cue.RerollFailure),
                "Restoring an existing buyer must not ring or play a reroll result");
        }
        finally
        {
            effects.Played -= Observe;
            Paused = false;
            if (GodotObject.IsInstanceValid(fixture)) fixture.QueueFree();
            if (GodotObject.IsInstanceValid(menu)) menu.QueueFree();
            if (GodotObject.IsInstanceValid(game)) game.QueueFree();
            if (hadSettings)
            {
                using var file = FileAccess.Open(GameLocalization.SettingsPath, FileAccess.ModeFlags.Write);
                file.StoreBuffer(backup);
            }
            else if (FileAccess.FileExists(GameLocalization.SettingsPath))
                DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(GameLocalization.SettingsPath));
            MusicSettings.Instance.LoadPreference();
            effects.LoadPreference();
            TranslationServer.SetLocale(originalLanguage);
            if (FileAccess.FileExists(save)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(save));
        }
        await Settle();
        foreach (var player in effects.GetChildren().OfType<AudioStreamPlayer>()) player.Stop();
        await ToSignal(CreateTimer(0.2), SceneTreeTimer.SignalName.Timeout);
    }
}
