using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using ChikagoShop.Domain.Days;

// Subsystem checks share the ProjectTest runner and fixture helpers.
public partial class ProjectTest
{
    private async Task WaitForSceneFade()
    {
        // Allow deferred scene handoffs to create their transition before checking it.
        await ToSignal(this, SignalName.ProcessFrame);
        ulong began = Time.GetTicksMsec();
        while (SceneTransition.IsActive(this))
        {
            Check(Time.GetTicksMsec() - began < 5000, "Scene fade must finish and release its input/pause lock");
            await ToSignal(this, SignalName.ProcessFrame);
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }

    private async Task TestSceneTransitions()
    {
        Check(OS.GetUserDataDir().Contains("ChikagoShopStartupSmoke"), "Transition tests require isolated userdata");
        Vector2I originalSize = Root.Size;
        var outgoing = new Node2D();
        var layer = new CanvasLayer();
        outgoing.AddChild(layer);
        var background = new ColorRect { Color = Colors.White };
        layer.AddChild(background);
        background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var button = new Button { Position = new Vector2(30, 30), Size = new Vector2(160, 70), Text = "Click" };
        layer.AddChild(button);
        int clicks = 0, ticks = 0, changes = 0;
        button.Pressed += () => clicks++;
        var timer = new Timer { WaitTime = 0.01, Autostart = true };
        timer.Timeout += () => ticks++;
        outgoing.AddChild(timer);
        Root.AddChild(outgoing);
        CurrentScene = outgoing;
        TradeScene incoming = null;
        try
        {
            using (await CapturePresentation()) { }
            int initialTicks = ticks;
            var run = SceneTransition.Run(outgoing, async () =>
            {
                changes++;
                var fade = Root.GetNode<SceneTransition>("SceneTransition");
                Check(fade.Opacity == 1 && Paused && ticks == initialTicks,
                    "The scene must only change behind fully opaque black while outgoing simulation is frozen");
                foreach (Vector2I size in new[] { new Vector2I(1280, 720), new Vector2I(960, 540) })
                {
                    Root.Size = size;
                    using var image = await CapturePresentation();
                    foreach (Vector2I pixel in new[] { new Vector2I(0, 0), new Vector2I(image.GetWidth() - 1, 0),
                        new Vector2I(0, image.GetHeight() - 1), new Vector2I(image.GetWidth() - 1, image.GetHeight() - 1),
                        new Vector2I(image.GetWidth() / 2, image.GetHeight() / 2) })
                    {
                        Color color = image.GetPixelv(pixel);
                        Check(color.R < 0.01 && color.G < 0.01 && color.B < 0.01,
                            "Opaque transition must cover the entire rendered viewport, including resized corners");
                    }
                }
                incoming = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
                incoming.SavePath = "";
                Check(ChangeSceneToNode(incoming) == Error.Ok, "Transition fixture must replace the outgoing scene");
                while (CurrentScene != incoming) await ToSignal(this, SignalName.ProcessFrame);
                Check(SceneTransition.IsActive(this) && Paused, "Root fade must survive the scene replacement");
                Root.PushInput(new InputEventKey { Pressed = true, Keycode = Key.Escape }, true);
                Check(!incoming.GetNode<PauseMenu>("PauseMenu").IsOpen,
                    "Incoming pause menu must ignore Escape until the transition ends");
                return true;
            });
            float alpha = 0;
            for (int frame = 0; frame < 30 && alpha == 0; frame++)
            {
                await ToSignal(this, SignalName.ProcessFrame);
                alpha = Root.GetNode<SceneTransition>("SceneTransition").Opacity;
            }
            Check(alpha > 0 && alpha < 1, "Fade-out must show intermediate opacity instead of a hard cut");
            Vector2 point = button.GetGlobalRect().GetCenter();
            Root.PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point,
                ButtonIndex = MouseButton.Left, Pressed = true }, true);
            Root.PushInput(new InputEventMouseButton { Position = point, GlobalPosition = point,
                ButtonIndex = MouseButton.Left, Pressed = false }, true);
            Check(!await SceneTransition.Run(outgoing, () => { changes++; return Task.FromResult(true); }),
                "A second transition must not run during the first");
            Check(await run && changes == 1 && clicks == 0 && !Paused && !SceneTransition.IsActive(this),
                "Transition must finish once, reject input and release its overlay and pause");
            await PressEscapeForTest();
            Check(incoming.GetNode<PauseMenu>("PauseMenu").IsOpen, "Input must work again after fade-in");
            await ClickControl(incoming, "PauseMenu/Root/Overlay/Center/Panel/Content/Menu/MainMenuButton");
            await WaitForSceneFade();
            Check(CurrentScene is MainMenu && !Paused && !GodotObject.IsInstanceValid(incoming),
                "Pause-to-menu must finish through the same fade and release the old game");

            var menu = (MainMenu)CurrentScene;
            menu.OnPlayPressed();
            var rejected = SceneTransition.Run(menu, () => Task.FromResult(false));
            menu._Input(new InputEventKey { Pressed = true, Keycode = Key.Escape });
            Check(menu.GetNode<Control>("MainMenuContainer/Menu/NewGameButton").Visible,
                "Menu input must stay blocked during the fade instead of navigating behind the cover");
            Check(!await rejected && !Paused && !SceneTransition.IsActive(this),
                "A rejected scene change must fade back and release input/pause");
            Paused = true;
            Check(!await SceneTransition.Run(CurrentScene, () => Task.FromResult(false)) && Paused,
                "A failed transition must preserve an existing pause");
            Paused = false;
            Check(await SceneTransition.Run(CurrentScene, () =>
            {
                Callable.From(() => Paused = true).CallDeferred();
                return Task.FromResult(true);
            }) && Paused, "Destination modal initialization must retain its own pause after fade-in");
            Paused = false;
            var cancelledOwner = new Node();
            Root.AddChild(cancelledOwner);
            bool called = false;
            var cancelled = SceneTransition.Run(cancelledOwner, () => { called = true; return Task.FromResult(true); });
            cancelledOwner.QueueFree();
            Check(!await cancelled && !called && !Paused && !SceneTransition.IsActive(this),
                "Removing the outgoing owner during fade-out must cancel without a late change or stuck pause");
        }
        finally
        {
            Paused = false;
            CurrentScene?.QueueFree();
            CurrentScene = null;
            Root.Size = originalSize;
            await ToSignal(this, SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
        }
    }

    private async Task TestIntroLatency()
    {
        Check(OS.GetUserDataDir().Contains("ChikagoShopStartupSmoke"), "Latency checks require isolated player saves");
        var scene = GD.Load<PackedScene>("res://scenes/main_menu.tscn");
        foreach (bool skip in new[] { true, false })
        foreach (double readingSeconds in new[] { 0.0, 0.8 })
        {
            var menu = scene.Instantiate<MainMenu>();
            menu.GameScene = null;
            Root.AddChild(menu);
            CurrentScene = menu;
            menu.OnPlayPressed();
            menu.GetNode<TextureButton>("MainMenuContainer/Menu/NewGameButton").EmitSignal(BaseButton.SignalName.Pressed);
            await SkipStartupTraining(menu);
            await ToSignal(this, SignalName.ProcessFrame);
            if (readingSeconds > 0)
                await ToSignal(CreateTimer(readingSeconds), SceneTreeTimer.SignalName.Timeout);
            if (readingSeconds > 0)
                Check(menu.IsGameScenePrepared, "Reading the intro must prepare the game before Skip");
            ulong began = Time.GetTicksMsec();
            if (skip) await ClickControl(menu, "IntroSkipLayer/SkipButton");
            else
            {
                var dialogue = menu.ActiveDialogue;
                int advances = 0;
                while (!dialogue.Session.IsComplete && advances++ < 512) dialogue.Advance();
                Check(dialogue.Session.IsComplete, "Natural prologue completion must reach its finish event");
            }
            // Repeated input during loading/fade must not create another run.
            if (GodotObject.IsInstanceValid(menu) && menu.IsInsideTree()) menu.OnPlayPressed();
            while (CurrentScene is not TradeScene)
            {
                Check(Time.GetTicksMsec() - began < 10000, "Skip must reach gameplay without a stuck transition");
                await ToSignal(this, SignalName.ProcessFrame);
            }
            await WaitForSceneFade();
            Check(CurrentScene is TradeScene && !Paused, "Measured skip must enter the actual game");
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            double elapsed = (Time.GetTicksMsec() - began) / 1000.0;
            GD.Print($"INTRO_SKIP_LATENCY skip={skip} reading={readingSeconds:F1}s transition-to-playable-frame={elapsed:F3}s");
            Check(elapsed < (readingSeconds > 0 ? 1.5 : 3.5),
                "Both finish and skip must use the shared short transition without the old1.5-second black hold");
            var game = (TradeScene)CurrentScene;
            Check(game.CurrentDay == 1 && !game.DayStarted && game.Score == game.StartingMoney,
                "A fast transition still creates exactly one fresh, unstarted day");
            game.QueueFree();
            CurrentScene = null;
            await ToSignal(this, SignalName.ProcessFrame);
        }
        var cancelled = scene.Instantiate<MainMenu>();
        cancelled.GameScene = null;
        Root.AddChild(cancelled);
        CurrentScene = cancelled;
        cancelled.OnPlayPressed();
        cancelled.GetNode<TextureButton>("MainMenuContainer/Menu/NewGameButton").EmitSignal(BaseButton.SignalName.Pressed);
        await SkipStartupTraining(cancelled);
        cancelled.GetNode<TextureButton>("IntroSkipLayer/SkipButton").EmitSignal(BaseButton.SignalName.Pressed);
        cancelled.QueueFree();
        CurrentScene = null;
        await ToSignal(CreateTimer(0.5), SceneTreeTimer.SignalName.Timeout);
        Check(CurrentScene == null && !Paused,
            $"Leaving during warmup/fade must not enter a game later (scene={CurrentScene?.Name}, paused={Paused})");
    }

    private async Task TestIntroTransition()
    {
        Check(OS.GetUserDataDir().Contains("ChikagoShopStartupSmoke"),
            "Startup smoke requires isolated userdata via a temporary override.cfg; refusing to touch player saves");
        GD.Print("Startup smoke user directory: " + OS.GetUserDataDir());
        var startupScene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
        try
        {
            Check(startupScene.SavePath == "user://trade_progress.json" && startupScene.StartingMoney == 220 &&
                startupScene.FirstQuota == 750 && startupScene.SecondQuota == 1500,
                "The packed gameplay scene must retain valid startup money, quotas and save path before Ready; editor null overrides break New Game");
            Check(startupScene.GetNode<FirstTradeTutorial>("FirstTradeTutorial").CameraTravelSeconds > 0 &&
                startupScene.GetNode<PulloutNotebook>("HUD/Root/PulloutNotebook").SlideSeconds > 0 &&
                !string.IsNullOrEmpty(startupScene.GetNode<TradingRoomPosterHotspot>("World/TradingRoom/ShowPoster").ThumbnailPath),
                "Startup tutorial and room UI must keep their authored defaults rather than null/zero exports");
        }
        finally { startupScene.Free(); }
        Check(FileAccess.FileExists("res://config/economy.v1.json") && FileAccess.FileExists("res://dialogue/intro.ru.json"),
            "The executable must contain both economy and dialogue JSON files");
        var menuScene = GD.Load<PackedScene>("res://scenes/main_menu.tscn");
        foreach (bool nullOverride in new[] { false, true })
        foreach (bool skip in new[] { false, true })
        {
            var menu = menuScene.Instantiate<MainMenu>();
            Check(menu.GameScene == null || menu.GameScene.ResourcePath == "res://scenes/trade_scene.tscn",
                "The serialized menu must use the actual game scene or the supported empty-override fallback");
            menu.GameScene = nullOverride ? null : GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
            var oldTutorial = new ConfigFile();
            oldTutorial.SetValue("tutorial", "finished", true);
            oldTutorial.SetValue("tutorial", "camera_pan_done", true);
            oldTutorial.SetValue("tutorial", "camera_zoom_done", true);
            oldTutorial.Save("user://trade_progress.json.tutorial.cfg");
            Root.AddChild(menu);
            CurrentScene = menu;
            menu.OnPlayPressed();
            menu.GetNode<TextureButton>("MainMenuContainer/Menu/NewGameButton").EmitSignal(BaseButton.SignalName.Pressed);
            await ToSignal(this, SignalName.ProcessFrame);
            await ToSignal(this, SignalName.ProcessFrame);
            var training = await WaitForStartupTraining(menu);
            var trainingTutorial = training.GetNode<FirstTradeTutorial>("FirstTradeTutorial");
            Check(menu.ActiveDialogue == null && trainingTutorial.IsActive && !trainingTutorial.CameraPanDone &&
                  !trainingTutorial.CameraZoomDone && !training.LoadedSavedGame,
                "New game must begin fresh camera practice before any prologue, ignoring the previous tutorial cfg");
            menu.GetNode<TextureButton>("MainMenuContainer/Menu/NewGameButton").EmitSignal(BaseButton.SignalName.Pressed);
            Check(menu.ActiveTrainingGame == training && menu.ActiveDialogue == null,
                "Repeated NewGame during training must not create another scene or start the prologue");
            training.Score = 77;
            training.EnergyRemaining = 0;
            training.PersistProgress();
            if (!nullOverride) await SkipStartupTraining(menu);
            else
            {
                // Resume a persisted final tutorial page, then acknowledge it through the real UI.
                // The complete tutorial suite covers reaching this page via the two actual sales.
                menu.QueueFree();
                CurrentScene = null;
                await ToSignal(this, SignalName.ProcessFrame);
                var snapshot = System.Text.Json.Nodes.JsonNode.Parse(FileAccess.GetFileAsString("user://trade_progress.json"));
                snapshot["accepted"] = 2;
                using (var file = FileAccess.Open("user://trade_progress.json", FileAccess.ModeFlags.Write))
                    file.StoreString(snapshot.ToJsonString());
                var finalPage = new ConfigFile();
                foreach (string key in new[] { "prologue_pending", "two_sales", "camera_pan_done", "camera_zoom_done",
                    "camera_handoff_done", "room_focus_done", "notebook_lesson_done" }) finalPage.SetValue("tutorial", key, true);
                finalPage.SetValue("tutorial", "accepted_at_start", 0);
                finalPage.SetValue("tutorial", "summary_page", 1);
                finalPage.Save("user://trade_progress.json.tutorial.cfg");
                menu = menuScene.Instantiate<MainMenu>();
                menu.GameScene = null;
                Root.AddChild(menu);
                CurrentScene = menu;
                menu.OnPlayPressed();
                menu.GetNode<TextureButton>("MainMenuContainer/Menu/ContinueButton").EmitSignal(BaseButton.SignalName.Pressed);
                training = await WaitForStartupTraining(menu);
                trainingTutorial = training.GetNode<FirstTradeTutorial>("FirstTradeTutorial");
                Check(training.LoadedSavedGame && training.Score == 77 && trainingTutorial.CurrentStep == FirstTradeTutorial.Step.Summary &&
                      menu.ActiveDialogue == null, "Continue must restore pending training before its prologue");
                await ClickControl(training, "FirstTradeTutorial/Root/Panel/Content/Actions/Done");
                await WaitForSceneFade();
            }
            Check(menu.ActiveDialogue != null && menu.ActiveTrainingGame == null && !GodotObject.IsInstanceValid(training) && !Paused,
                "Both completion and confirmed skip must remove practice and start exactly one unpaused prologue");
            if (!nullOverride && !skip)
                using (await CapturePresentation("startup-after-training")) { }
            // Cancelling the cutscene must preserve the pending prologue for Continue.
            menu.ActiveDialogue._Input(new InputEventKey { Pressed = true, Keycode = Key.Escape });
            await WaitForSceneFade();
            Check(menu.ActiveDialogue == null, "Escape must return from prologue to the menu");
            menu.GetNode<TextureButton>("MainMenuContainer/Menu/ContinueButton").EmitSignal(BaseButton.SignalName.Pressed);
            await WaitForSceneFade();
            Check(menu.ActiveDialogue != null && menu.ActiveTrainingGame == null,
                "Continue after cancelling a pending prologue must resume it without replaying training");
            await ToSignal(this, SignalName.ProcessFrame);
            await ToSignal(this, SignalName.ProcessFrame);
            if (skip) await ClickControl(menu, "IntroSkipLayer/SkipButton");
            else
            {
                int steps = 0;
                while (menu.ActiveDialogue != null && !menu.ActiveDialogue.Session.IsComplete && steps++ < 512)
                    menu.ActiveDialogue.Advance();
                Check(steps < 512, "The full intro must make progress and reach its finish event");
            }
            await WaitForIntroMusicExit(menu);
            await ToSignal(this, SignalName.ProcessFrame);
            await ToSignal(this, SignalName.ProcessFrame);
            Check(CurrentScene is TradeScene && !Paused,
                $"Intro must enter gameplay instead of returning to NewGame (null={nullOverride}, skip={skip})");
            var game = (TradeScene)CurrentScene;
            var tutorial = game.GetNode<FirstTradeTutorial>("FirstTradeTutorial");
            Check(!game.LoadedSavedGame && !tutorial.IsActive && !tutorial.ProloguePending,
                "After prologue every menu NewGame path must enter ordinary gameplay without repeating training");
            game.SetProcess(false);
            Check(game.CurrentDay == 1 && game.Score == game.StartingMoney &&
                  Math.Abs(game.DayRemainingSeconds - WorkshopItemCatalog.DayDurationSeconds) < 0.01 &&
                  !game.DayStarted && game.EnergyRemaining == game.EnergyCapacity && game.Accepted == 0 &&
                  game.StoredItems.Length == 0 && game.ShowcaseItems.All(item => item < 0) &&
                  !game.MusicWorkshopUnlocked && game.GetNode<Button>("HUD/DayClock/Root/DayTimer/StartButton").Visible,
                "Ordinary day1 must start clean with no training spending, sales, unlocks or inventory carried over");
            game.Score = 321;
            game.PersistProgress();
            game.QueueFree();
            CurrentScene = null;
            await ToSignal(this, SignalName.ProcessFrame);
        }
        var completedTutorial = new ConfigFile();
        completedTutorial.SetValue("tutorial", "finished", true);
        completedTutorial.Save("user://trade_progress.json.tutorial.cfg");
        var continueMenu = menuScene.Instantiate<MainMenu>();
        continueMenu.GameScene = null;
        Root.AddChild(continueMenu);
        CurrentScene = continueMenu;
        continueMenu.OnPlayPressed();
        continueMenu.GetNode<TextureButton>("MainMenuContainer/Menu/ContinueButton").EmitSignal(BaseButton.SignalName.Pressed);
        await WaitForIntroMusicExit(continueMenu);
        await ToSignal(this, SignalName.ProcessFrame);
        await ToSignal(this, SignalName.ProcessFrame);
        Check(CurrentScene is TradeScene continued && continued.LoadedSavedGame &&
            !continued.GetNode<FirstTradeTutorial>("FirstTradeTutorial").IsActive,
            "Actual menu Continue must preserve the current run's completed or skipped tutorial");
        Check(CurrentScene is TradeScene && ((TradeScene)CurrentScene).Score == 321,
            "Continue must also resolve a missing scene without resetting saved progress");
        CurrentScene.QueueFree();
        CurrentScene = null;
        await ToSignal(this, SignalName.ProcessFrame);
    }

    private async Task<TradeScene> WaitForStartupTraining(MainMenu menu)
    {
        ulong began = Time.GetTicksMsec();
        while (menu.ActiveTrainingGame == null ||
            !menu.ActiveTrainingGame.GetNode<FirstTradeTutorial>("FirstTradeTutorial").IsActive)
        {
            Check(Time.GetTicksMsec() - began < 10000, "NewGame/Continue must reach active training within a bounded time");
            await ToSignal(this, SignalName.ProcessFrame);
        }
        await WaitForSceneFade();
        return menu.ActiveTrainingGame;
    }

    private async Task SkipStartupTraining(MainMenu menu)
    {
        var training = await WaitForStartupTraining(menu);
        Check(menu.ActiveDialogue == null, "The prologue must wait for tutorial completion or confirmed skip");
        await ClickControl(training, "FirstTradeTutorial/Root/Skip");
        Check(training.GetNode<FirstTradeTutorial>("FirstTradeTutorial").IsSkipConfirmationOpen && menu.ActiveDialogue == null,
            "Opening skip confirmation must not start the prologue before consent");
        await ClickControl(training, "FirstTradeTutorial/Root/SkipConfirmation/Center/Panel/Content/Actions/Confirm");
        await WaitForSceneFade();
        Check(menu.ActiveDialogue != null && menu.ActiveTrainingGame == null && !Paused,
            "Confirming tutorial skip must start the prologue");
    }

    private async Task TestMainMenu()
    {
        var scene = GD.Load<PackedScene>("res://scenes/main_menu.tscn")
            ?? throw new Exception("Main menu must load");
        var menu = scene.Instantiate<MainMenu>();
        Root.AddChild(menu);
        CurrentScene = menu;
        await ToSignal(this, SignalName.ProcessFrame);
        Check(ProjectSettings.GetSetting("gui/theme/custom").AsString() == "res://themes/game_theme.tres",
            "The art team's font must be configured as the global UI theme");
        var gameFont = GD.Load<Theme>("res://themes/game_theme.tres").DefaultFont;
        foreach (char letter in "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyzАБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя")
            Check(gameFont.HasChar(letter), $"The game font must include English and Russian letters: {letter}");
        Check(menu.GetNode<CheckButton>("SettingsMenuContainer/Settings/FullscreenButton").GetThemeFont("font") == gameFont,
            "Explicit menu fonts must use the same completed font as the global UI theme");
        Check(menu.GameScene == null || menu.GameScene.ResourcePath == "res://scenes/trade_scene.tscn",
            "The game scene may prepare asynchronously while the menu remains usable");
        foreach (string path in new[]
        {
            "MainMenuContainer/Menu/PlayButton", "MainMenuContainer/Menu/SettingsButton",
            "MainMenuContainer/Menu/QuitButton", "SettingsMenuContainer/Settings/BackButton"
        })
            Check(menu.GetNode<TextureButton>(path).TextureNormal != null, $"Menu button art must load: {path}");
        var mainView = menu.GetNode<CenterContainer>("MainMenuContainer");
        var settingsView = menu.GetNode<CenterContainer>("SettingsMenuContainer");
        Check(settingsView.GetNode<TextureRect>("SettingsFrame").Texture != null,
            "Settings must use the art team's finished panel");
        menu.OnSettingsPressed();
        Check(!mainView.Visible && settingsView.Visible, "Settings must open");
        CheckFullscreenText(menu.GetNode<CheckButton>("SettingsMenuContainer/Settings/FullscreenButton"));
        menu.OnBackPressed();
        Check(mainView.Visible && !settingsView.Visible, "Back must return to the main menu");
        menu.OnPlayPressed();
        Check(menu.ActiveDialogue != null && !mainView.Visible && CurrentScene == menu,
            "Play must start the prologue without creating a running game");
        var dialogue = menu.ActiveDialogue;
        menu.OnPlayPressed();
        Check(menu.ActiveDialogue == dialogue, "Repeated Play must not create duplicate dialogues");
        var dialogueStyle = dialogue.GetNode<PanelContainer>("Panel").GetThemeStylebox("panel") as StyleBoxTexture;
        Check(dialogueStyle?.Texture?.ResourcePath == "res://art/ui/dialog_panel.png",
            "Dialogue panel must use the supplied sprite as its background");
        Check(dialogue.Theme.DefaultFont == gameFont &&
              dialogue.GetNode<Label>("Panel/Content/Speaker").GetThemeFont("font") == gameFont &&
              dialogue.GetNode<RichTextLabel>("Panel/Content/Text").GetThemeFont("normal_font") == gameFont,
            "Dialogue labels and speech must use the project's shared font");
        Check(!dialogue.HasNode("Panel/Content/Actions") && !dialogue.HasNode("Panel/Content/Hint") &&
              dialogue.GetNode<Control>("Panel/Content").GetChildCount() == 2,
            "Only speaker and speech must remain in the cloud, with no buttons, counter or hints");
        Check(!dialogue.HasNode("Panel/Content/Rule") &&
              dialogue.GetNode<PanelContainer>("Panel").Material is ShaderMaterial,
            "The divider must be removed and the cloud edges must fade smoothly");
        Vector2I dialogueViewport = Root.Size;
        foreach (var size in new[] { new Vector2I(1280, 720), new Vector2I(960, 540) })
        {
            Root.Size = size;
            await ToSignal(this, SignalName.ProcessFrame);
            await ToSignal(this, SignalName.ProcessFrame);
            var panel = dialogue.GetNode<Control>("Panel");
            var text = dialogue.GetNode<RichTextLabel>("Panel/Content/Text");
            Check(panel.Size.Y >= 260 && panel.Size.Y <= 280 &&
                  panel.Size.X <= Root.GetVisibleRect().Size.X * 0.79f &&
                  text.GlobalPosition.X >= panel.GlobalPosition.X + 112 &&
                  text.GetGlobalRect().End.X <= panel.GetGlobalRect().End.X - 112 &&
                  text.GetGlobalRect().End.Y <= panel.GetGlobalRect().End.Y - 64,
                "The cloud must retain horizontal and bottom text padding at both viewport sizes");
            Check(new Rect2(Vector2.Zero, Root.GetVisibleRect().Size).Encloses(panel.GetGlobalRect()) &&
                  panel.GetGlobalRect().Encloses(text.GetGlobalRect()) && text.Size.Y > 40 &&
                  (text.ScrollActive || text.GetContentHeight() <= text.Size.Y) &&
                  text.GetThemeFontSize("normal_font_size") == 20,
                "Speech must retain its font size, using scrolling when needed inside the cloud");
        }
        Root.Size = dialogueViewport;
        await ToSignal(this, SignalName.ProcessFrame);
        Check(dialogue.Session.Count == 24 && dialogue.Session.Current.Speaker == "Покупательница",
            "The prologue must begin directly with the customer, without the opening storyboard");
        await ClickControl(dialogue, "Panel/Content/Text");
        Check(!dialogue.IsRevealing && dialogue.Session.Index == 0,
            "First click must reveal text without skipping the storyboard");
        string firstPage = dialogue.GetNode<RichTextLabel>("Panel/Content/Text").Text;
        dialogue._Input(new InputEventKey { Pressed = true, Keycode = Key.Space });
        Check(dialogue.IsRevealing && (dialogue.Session.Index > 0 ||
              dialogue.GetNode<RichTextLabel>("Panel/Content/Text").Text != firstPage),
            "Space must advance the fully revealed page, including continuation of a long speech");
        int heldIndex = dialogue.Session.Index;
        string heldText = dialogue.GetNode<RichTextLabel>("Panel/Content/Text").Text;
        dialogue._Input(new InputEventKey { Pressed = true, Echo = true, Keycode = Key.Space });
        Check(dialogue.Session.Index == heldIndex && dialogue.GetNode<RichTextLabel>("Panel/Content/Text").Text == heldText,
            "Held keys must not skip dialogue");
        dialogue._Input(new InputEventKey { Pressed = true, Keycode = Key.Escape });
        await ToSignal(this, SignalName.ProcessFrame);
        Check(CurrentScene == menu && menu.ActiveDialogue == null && mainView.Visible,
            "Escape must return to the menu without quitting or starting gameplay");
        menu.OnPlayPressed();
        Check(menu.ActiveDialogue.Session.Index == 0, "A cancelled prologue must restart from its first frame");
        Root.Size = new Vector2I(960, 540);
        await CompletePrologue(menu);
        Root.Size = dialogueViewport;
        await ToSignal(this, SignalName.ProcessFrame);
        Check(CurrentScene is TradeScene, "Play must enter the game");
        var game = (TradeScene)CurrentScene;
        int score = game.Score;
        int[] stored = game.StoredItems;
        int[] showcases = game.ShowcaseItems;
        bool active = game.TradeActive;
        int activeItem = game.ItemIndex;
        game.SetProcess(false);
        await PressEscapeForTest();
        Check(Paused, "Returning to the menu must begin from pause");
        await ClickControl(game, "PauseMenu/Root/Overlay/Center/Panel/Content/Menu/MainMenuButton");
        Check(CurrentScene is MainMenu && !Paused && !GodotObject.IsInstanceValid(game),
            "The main menu button must change scenes, free the game and release pause");
        menu = (MainMenu)CurrentScene;
        await ClickControl(menu, "MainMenuContainer/Menu/SettingsButton");
        Check(menu.GetNode<Control>("SettingsMenuContainer").Visible,
            "The returned main menu must respond to input");
        menu.OnBackPressed();
        menu.OnPlayPressed();
        await CompletePrologue(menu);
        await ToSignal(this, SignalName.ProcessFrame);
        game = (TradeScene)CurrentScene;
        game.SetProcess(false);
        Check(!Paused && game.Score == score && game.StoredItems.SequenceEqual(stored) &&
              game.ShowcaseItems.SequenceEqual(showcases) && game.TradeActive == active && game.ItemIndex == activeItem,
            "Reentering from the menu must retain saved goods and the active trade");
        GD.Print("Main menu checks passed");
    }

    private async Task CompletePrologue(MainMenu menu)
    {
        var dialogue = menu.ActiveDialogue;
        Check(dialogue != null, "Play must always open the prologue");
        await ToSignal(this, SignalName.ProcessFrame);
        await ToSignal(this, SignalName.ProcessFrame);
        int count = dialogue.Session.Count;
        bool checkedScrolling = false;
        for (int index = 0; index < count; index++)
        {
            Check(CurrentScene == menu && dialogue.Session.Index == index,
                "Gameplay must not begin before all dialogue lines have been acknowledged");
            string original = dialogue.Session.Current.Text;
            string reconstructed = "";
            int pages = 0;
            while (!dialogue.Session.IsComplete && dialogue.Session.Index == index)
            {
                Check(++pages < 50, "Pagination must always make progress");
                if (dialogue.IsRevealing) dialogue.Advance();
                var text = dialogue.GetNode<RichTextLabel>("Panel/Content/Text");
                var speaker = dialogue.GetNode<Label>("Panel/Content/Speaker");
                Check(speaker.Visible == !string.IsNullOrWhiteSpace(dialogue.Session.Current.Speaker) &&
                      speaker.Text != "Раскадровка", "Actions must not display a narration heading");
                Check(text.GetThemeFontSize("normal_font_size") == 20 &&
                      (text.ScrollActive || text.GetContentHeight() <= text.Size.Y),
                    "Every page must retain font size, allowing scrolling for long sentences");
                Check(text.GetVScrollBar().GetThemeStylebox("grabber") is StyleBoxFlat,
                    "Long dialogue must use a styled scrollbar");
                if (!checkedScrolling && text.ScrollActive && text.GetContentHeight() > text.Size.Y)
                {
                    await ToSignal(this, SignalName.ProcessFrame);
                    await ToSignal(this, SignalName.ProcessFrame);
                    var scrollbar = text.GetVScrollBar();
                    Check(scrollbar.IsVisibleInTree() && scrollbar.MaxValue > scrollbar.Page,
                        "Overflowing sentences must expose a usable scroll range");
                    string page = text.Text;
                    dialogue._Input(new InputEventMouseButton { Pressed = true, ButtonIndex = MouseButton.Left,
                        Position = scrollbar.GetGlobalRect().GetCenter() });
                    Check(dialogue.Session.Index == index && text.Text == page,
                        "Clicking the scrollbar must not advance the dialogue");
                    scrollbar.Value = scrollbar.MaxValue;
                    Check(scrollbar.Value > 0, "The end of the long sentence must be reachable by scrolling");
                    checkedScrolling = true;
                }
                reconstructed += text.Text;
                if (reconstructed.Length < original.Length)
                {
                    string ending = text.Text.TrimEnd().TrimEnd('»', '”', '"', '\'', ')', ']', '*');
                    Check(ending.Length > 0 && ".!?…".Contains(ending[^1]),
                        "A continuing speech must end on a complete sentence");
                }
                dialogue._Input(new InputEventMouseButton { Pressed = true, ButtonIndex = MouseButton.Left });
            }
            Check(reconstructed == original, "All pages must reproduce the entire original speech without loss or duplication");
        }
        dialogue.Advance(); // Completed sessions must not start the game twice.
        await WaitForIntroMusicExit(menu);
        await ToSignal(this, SignalName.ProcessFrame);
    }

    private async Task WaitForIntroMusicExit(MainMenu menu)
    {
        if (GodotObject.IsInstanceValid(menu) && menu.IsInsideTree())
        {
            var music = menu.GetNode<IntroMusicDirector>("MusicDirector");
            if (music.CurrentCue == "silence") music._Process(music.ExitFadeSeconds);
        }
        ulong began = Time.GetTicksMsec();
        while (CurrentScene == menu || CurrentScene == null)
        {
            Check(Time.GetTicksMsec() - began < 5000, "Music exit and asynchronous scene preparation must finish");
            await ToSignal(this, SignalName.ProcessFrame);
        }
        await WaitForSceneFade();
        await ToSignal(this, SignalName.ProcessFrame);
    }

    private static void CheckFullscreenText(CheckButton button)
    {
        foreach (string state in new[] { "normal", "hover", "pressed", "hover_pressed", "disabled" })
        {
            var style = button.GetThemeStylebox(state) as StyleBoxFlat;
            Check(style != null && style.BgColor.R > 0.8f && style.BgColor.G > 0.8f && style.BgColor.B > 0.8f,
                "Fullscreen text must have a light opaque background in every state");
        }
        foreach (bool enabled in new[] { false, true })
        {
            button.SetPressedNoSignal(enabled);
            foreach (string state in new[] { "font_color", "font_hover_color", "font_pressed_color",
                         "font_hover_pressed_color", "font_focus_color", "font_disabled_color" })
            {
                Color color = button.GetThemeColor(state);
                Check(color.R < 0.3f && color.G < 0.3f && color.B < 0.3f && color.A == 1,
                    "Fullscreen text must stay dark and legible in all interaction and toggle states");
            }
        }
    }
}
