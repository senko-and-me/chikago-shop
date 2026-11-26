using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using ChikagoShop.Domain.Days;

// Subsystem checks share the ProjectTest runner and fixture helpers.
public partial class ProjectTest
{
    private async Task TestIntroLatency()
    {
        Check(OS.GetUserDataDir().Contains("ChikagoShopStartupSmoke"), "Latency checks require isolated player saves");
        var scene = GD.Load<PackedScene>("res://scenes/main_menu.tscn");
        foreach (double readingSeconds in new[] { 0.0, 0.8 })
        {
            var menu = scene.Instantiate<MainMenu>();
            menu.GameScene = null;
            Root.AddChild(menu);
            CurrentScene = menu;
            menu.OnPlayPressed();
            menu.GetNode<TextureButton>("MainMenuContainer/Menu/NewGameButton").EmitSignal(BaseButton.SignalName.Pressed);
            await ToSignal(this, SignalName.ProcessFrame);
            if (readingSeconds > 0)
                await ToSignal(CreateTimer(readingSeconds), SceneTreeTimer.SignalName.Timeout);
            if (readingSeconds > 0)
                Check(menu.IsGameScenePrepared, "Reading the intro must prepare the game before Skip");
            ulong began = Time.GetTicksMsec();
            await ClickControl(menu, "IntroSkipLayer/SkipButton");
            // Repeated input during loading/fade must not create another run.
            if (GodotObject.IsInstanceValid(menu) && menu.IsInsideTree()) menu.OnPlayPressed();
            while (CurrentScene is not TradeScene)
            {
                Check(Time.GetTicksMsec() - began < 10000, "Skip must reach gameplay without a stuck transition");
                await ToSignal(this, SignalName.ProcessFrame);
            }
            Check(CurrentScene is TradeScene && !Paused, "Measured skip must enter the actual game");
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            double elapsed = (Time.GetTicksMsec() - began) / 1000.0;
            GD.Print($"INTRO_SKIP_LATENCY reading={readingSeconds:F1}s click-to-first-game-frame={elapsed:F3}s");
            Check(elapsed < (readingSeconds > 0 ? 1.0 : 3.0),
                "Prepared Skip must avoid the old mandatory 1.5-second wait; cold loading must also stay bounded");
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
        cancelled.GetNode<TextureButton>("IntroSkipLayer/SkipButton").EmitSignal(BaseButton.SignalName.Pressed);
        cancelled.QueueFree();
        CurrentScene = null;
        await ToSignal(CreateTimer(0.5), SceneTreeTimer.SignalName.Timeout);
        Check(CurrentScene == null && !Paused, "Leaving during warmup/fade must not enter a game later");
    }

    private async Task TestIntroTransition()
    {
        Check(OS.GetUserDataDir().Contains("ChikagoShopStartupSmoke"),
            "Startup smoke requires isolated userdata via a temporary override.cfg; refusing to touch player saves");
        GD.Print("Startup smoke user directory: " + OS.GetUserDataDir());
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
            Check(menu.ActiveDialogue != null, "New game must open the intro");
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
            Check(!game.LoadedSavedGame && tutorial.IsActive && !tutorial.CameraPanDone && !tutorial.CameraZoomDone,
                "Every actual menu NewGame path must ignore the previous run's completed tutorial");
            game.SetProcess(false);
            Check(game.CurrentDay == 1 && game.Score == game.StartingMoney && game.DayRemainingSeconds > 88 &&
                  !game.DayStarted && game.GetNode<Button>("HUD/DayClock/Root/DayTimer/StartButton").Visible,
                "A new run must preserve the current starting economy and wait for manual day start during camera practice");
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
