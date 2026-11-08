using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using ChikagoShop.Domain.Days;

// Subsystem checks share the ProjectTest runner and fixture helpers.
public partial class ProjectTest
{
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
            Check(menu.GameScene?.ResourcePath == "res://scenes/trade_scene.tscn",
                "The serialized main menu must reference the actual game scene");
            if (nullOverride) menu.GameScene = null;
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
                while (menu.ActiveDialogue != null && steps++ < 512) menu.ActiveDialogue.Advance();
                Check(steps < 512, "The full intro must make progress and reach its finish event");
            }
            await ToSignal(this, SignalName.ProcessFrame);
            await ToSignal(this, SignalName.ProcessFrame);
            Check(CurrentScene is TradeScene && !Paused,
                $"Intro must enter gameplay instead of returning to NewGame (null={nullOverride}, skip={skip})");
            var game = (TradeScene)CurrentScene;
            game.SetProcess(false);
            Check(game.CurrentDay == 1 && game.Score == game.StartingMoney && game.DayRemainingSeconds > 88 &&
                  game.GetNode<Label>("HUD/Root/DayTimer").Text == "90",
                "The exported game must start a real new day with the current economy and clock");
            game.Score = 321;
            game.PersistProgress();
            game.QueueFree();
            CurrentScene = null;
            await ToSignal(this, SignalName.ProcessFrame);
        }
        var continueMenu = menuScene.Instantiate<MainMenu>();
        continueMenu.GameScene = null;
        Root.AddChild(continueMenu);
        CurrentScene = continueMenu;
        continueMenu.OnPlayPressed();
        continueMenu.GetNode<TextureButton>("MainMenuContainer/Menu/ContinueButton").EmitSignal(BaseButton.SignalName.Pressed);
        await ToSignal(this, SignalName.ProcessFrame);
        await ToSignal(this, SignalName.ProcessFrame);
        Check(CurrentScene is TradeScene && ((TradeScene)CurrentScene).Score == 321,
            "Continue must also resolve a missing scene without resetting saved progress");
        CurrentScene.QueueFree();
        CurrentScene = null;
        await ToSignal(this, SignalName.ProcessFrame);
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
        Check(menu.GameScene.ResourcePath == "res://scenes/trade_scene.tscn", "Play must open the trade scene");
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
        await ClickControl(game, "PauseMenu/Root/PauseButton");
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
