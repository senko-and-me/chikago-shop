using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class ProjectTest
{

    private async Task TestCraftPaperEdge()
    {
        var viewport = new SubViewport { Size = new Vector2I(500, 240), TransparentBg = true,
            World2D = new World2D(), RenderTargetUpdateMode = SubViewport.UpdateMode.Always };
        Root.AddChild(viewport);
        try
        {
            var button = GD.Load<PackedScene>("res://scenes/UI/CraftButton.tscn").Instantiate<Button>();
            Check(button.HasNode("Frame") && button.HasNode("EnglishCaption") &&
                button.HasNode("RussianCaption") && button.HasNode("RarityHotspot/EnglishHelp"),
                "All craft artwork must already exist in the reusable scene before Ready");
            button.Position = new Vector2(16, 16);
            viewport.AddChild(button);
            CraftButtonPresentation.Apply(button, true);
            for (int i = 0; i < 3; i++) await ToSignal(this, SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            using (var pixels = viewport.GetTexture().GetImage())
            {
                bool clear = false, ink = false;
                for (int x = 16; x < 466; x++)
                    for (int y = 16; y < 22; y++)
                    {
                        Color c = pixels.GetPixel(x, y);
                        clear |= c.A < 0.1f;
                        ink |= c.A > 0.5f && c.R < 0.2f;
                    }
                Check(clear && ink && pixels.GetPixel(200, 190).A > 0.99f,
                    "The frame texture must render a broken transparent contour around an opaque paper center");
            }
            foreach (bool disabled in new[] { true, false })
            {
                button.Disabled = disabled;
                for (int i = 0; i < 2; i++) await ToSignal(this, SignalName.ProcessFrame);
                var paper = button.GetNode<Control>("Paper");
                var help = button.GetNode<Control>("RarityHotspot/EnglishHelp");
                Check(paper.Modulate == help.Modulate && (disabled ? help.Modulate.R < 0.6f : help.Modulate == Colors.White),
                    "Disabled and restored artwork must tint the help icon and paper together");
                if (disabled)
                {
                    await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    using var pixels = viewport.GetTexture().GetImage();
                    Check(Mathf.Abs(pixels.GetPixel(394, 31).R - pixels.GetPixel(380, 31).R) < 0.03f,
                        "Rendered help-icon paper must match adjacent paper without a bright square");
                }
            }
            button.ToggleMode = true;
            button.ButtonPressed = true;
            for (int i = 0; i < 2; i++) await ToSignal(this, SignalName.ProcessFrame);
            Check(button.GetNode<Control>("Paper").Modulate == button.GetNode<Control>("RarityHotspot/EnglishHelp").Modulate &&
                Mathf.IsEqualApprox(button.GetNode<Control>("Paper").Modulate.R, 0.78f),
                "Pressed artwork must darken the icon and paper together as well");
        }
        finally { viewport.QueueFree(); }
        await ToSignal(this, SignalName.ProcessFrame);
    }
    private async Task TestRoomDoorways()
    {
        const string save = "user://room_doorway_test.json";
        var scene = GD.Load<PackedScene>("res://scenes/trade_scene.tscn");
        TradeScene game = scene.Instantiate<TradeScene>();
        void CheckDoors(bool open)
        {
            if (game.IsInsideTree())
            {
                Check(game.GetNode<Node2D>("World/Showroom/Showcases").Visible == open,
                    "Locked displays must not protrude in front of the curtain at distant camera zoom");
                foreach (string room in new[] { "MusicWorkshop", "Showroom" })
                {
                    var overlay = game.GetNode<ColorRect>($"World/{room}/LockedOverlay");
                    var curtain = overlay.GetNode<TextureRect>("LockedCurtain");
                    Check(!overlay.HasNode("LockTitle"), "Locked rooms and displays must have no title over their curtains");
                    Check(overlay.Visible == !open && curtain.Texture.ResourcePath == "res://art/rooms/locked_curtain.png" &&
                        Mathf.Abs((curtain.Size.X - overlay.Size.X) * overlay.GetParent<Node2D>().GlobalScale.X - 2) < 0.02f &&
                        Mathf.Abs((curtain.Size.Y - overlay.Size.Y) * overlay.GetParent<Node2D>().GlobalScale.Y - 13) < 0.02f &&
                        curtain.MouseFilter == Control.MouseFilterEnum.Ignore &&
                        curtain.GetIndex() == 0 && curtain.Modulate == Colors.White &&
                        curtain.SelfModulate == new Color(0.55f, 0.55f, 0.55f, 1),
                        "Curtains must cover both locked rooms, keep labels above them and leave input to the existing overlay");
                }
            }
            string suffix = open ? ".jpg" : "_closed.jpg";
            foreach (var entry in new[] { ("Corridor/CorridorBackground", "upper_corridor"),
                ("TradingRoom/TradingRoomBackground", "cashier") })
            {
                var sprite = game.GetNode<Sprite2D>("World/" + entry.Item1);
                Check(sprite.Texture.ResourcePath == "res://art/rooms/" + entry.Item2 + suffix &&
                    sprite.Texture.GetSize() == new Vector2(1398, 1080) && !sprite.Centered,
                    $"{entry.Item2} must show the {(open ? "open" : "closed")} doorway with the same source art");
                float sideOverlap = 2 / sprite.GetParent<Node2D>().Scale.X;
                Check(Mathf.Abs(sprite.Position.X - (3 - sideOverlap)) < 0.02f &&
                    Mathf.Abs(sprite.Scale.X * 1398 - (1398 + sideOverlap * 2)) < 0.02f,
                    "Doorway backgrounds must retain the narrower vertical partitions across unlock and reload");
                float overlap = 13 / sprite.GetParent<Node2D>().Scale.Y;
                Check(Mathf.Abs(sprite.Scale.Y * 1080 - (1080 + overlap)) < 0.02f &&
                    Mathf.Abs(sprite.Position.Y - (entry.Item2 == "cashier" ? 4 - overlap : 4)) < 0.02f,
                    "Doorway backgrounds must retain the thinner floor seam across unlock and reload");
            }
        }
        async Task Reload()
        {
            game.QueueFree();
            await ToSignal(this, SignalName.ProcessFrame);
            game = scene.Instantiate<TradeScene>();
            game.SavePath = save;
            Root.AddChild(game);
            game.SetProcess(false);
            await ToSignal(this, SignalName.ProcessFrame);
        }
        try
        {
            if (FileAccess.FileExists(save)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(save));
            CheckDoors(false); // The editor and the initial runtime frame must agree.
            game.SavePath = save;
            Root.AddChild(game);
            game.SetProcess(false);
            await ToSignal(this, SignalName.ProcessFrame);
            CheckDoors(false);
            if (DisplayServer.GetName() != "headless")
            {
                var curtains = new[] { "MusicWorkshop", "Showroom" }.Select(room =>
                    game.GetNode<TextureRect>($"World/{room}/LockedOverlay/LockedCurtain")).ToArray();
                foreach (var curtain in curtains) curtain.SelfModulate = Colors.White;
                using var before = await CapturePresentation("curtains-before");
                foreach (var curtain in curtains) curtain.SelfModulate = new Color(0.55f, 0.55f, 0.55f, 1);
                using var after = await CapturePresentation("curtains-darkened");
                Vector2 scale = new Vector2(after.GetWidth(), after.GetHeight()) / Root.GetVisibleRect().Size;
                foreach (var curtain in curtains)
                {
                    Rect2 bounds = ScreenBounds(curtain).Intersection(Root.GetVisibleRect());
                    int dimmed = 0;
                    for (int y = (int)(bounds.Position.Y * scale.Y) + 5; y < bounds.End.Y * scale.Y - 5; y += 5)
                    for (int x = (int)(bounds.Position.X * scale.X) + 5; x < bounds.End.X * scale.X - 5; x += 5)
                    {
                        float light = before.GetPixel(x, y).R, dark = after.GetPixel(x, y).R;
                        if (light > 0.9f && dark > 0.2f && dark < light * 0.8f) dimmed++;
                    }
                    Check(dimmed > 100, "Both rendered curtains must look substantially darker while retaining visible fabric");
                }
            }
            game.EndDay(); // Unlock checks exercise a running day after the pre-start action gate.
            game.Score = 0;
            game.TryUnlockMusicWorkshop();
            Check(!game.MusicWorkshopUnlocked, "An unaffordable unlock must keep neighboring rooms closed");
            CheckDoors(false);
            game.Score = 220;
            game.TryUnlockMusicWorkshop();
            Check(game.MusicWorkshopUnlocked && game.Score == 120,
                "Existing unlock action and cost must remain unchanged");
            CheckDoors(true);
            await Reload();
            Check(game.MusicWorkshopUnlocked, "Reload must restore the open-room state");
            CheckDoors(true);
            game.ReturnToCheckpoint();
            Check(!game.MusicWorkshopUnlocked, "The initial checkpoint must close the added rooms again");
            CheckDoors(false);
            await Reload();
            CheckDoors(false);
            game.EndDay();
            game.TryUnlockMusicWorkshop();
            CheckDoors(true);
            game.NewGame();
            Check(!game.MusicWorkshopUnlocked && game.Score == 220, "New Game must restore the original locked state");
            CheckDoors(false);
        }
        finally
        {
            game.QueueFree();
            if (FileAccess.FileExists(save)) DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(save));
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }

    private async Task TestLocalizedMenuAssets()
    {
        string locale = TranslationServer.GetLocale();
        var menu = GD.Load<PackedScene>("res://scenes/main_menu.tscn").Instantiate<MainMenu>();
        Root.AddChild(menu);
        try
        {
            foreach (string language in new[] { "ru", "en" })
            {
                menu.GetNode<Button>("SettingsMenuContainer/Settings/LanguageRow/" +
                    (language == "en" ? "EnglishButton" : "RussianButton"))
                    .EmitSignal(BaseButton.SignalName.Pressed);
                await ToSignal(this, SignalName.ProcessFrame);
                string prefix = $"res://art/ui/{language}/";
                foreach (var entry in new[] { ("NewGameButton", "new_game.png"), ("ContinueButton", "continue.png") })
                    Check(menu.GetNode<TextureButton>("MainMenuContainer/Menu/" + entry.Item1).TextureNormal?.ResourcePath == prefix + entry.Item2,
                        "Choice buttons must use renamed localized textures");
                var fullscreen = menu.GetNode<TextureButton>("SettingsMenuContainer/Settings/FullscreenButton");
                Check(fullscreen.TextureNormal?.ResourcePath is string path &&
                    (path == prefix + "fullscreen_on.png" || path == prefix + "fullscreen_off.png"),
                    "Fullscreen button must use renamed localized art");
                Check(menu.GetNode<TextureButton>("MainMenuContainer/Menu/PlayButton").TextureNormal != null,
                    "Dynamically constructed play texture path must load");
            }
        }
        finally
        {
            menu.QueueFree();
            TranslationServer.SetLocale(locale);
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }

    // Asset replacement must preserve all saved catalogue identities and the trading lifecycle.

    private static void TestResourceOrganization()
    {
        var font = GD.Load<FontFile>("res://art/fonts/game.ttf");
        Check(font != null, "The canonical game font must load");
        string supported = font.GetSupportedChars();
        // The original font covers text and digits and uses system fallback for symbols.
        string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789" +
            "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя";
        foreach (char character in alphabet)
            Check(supported.Contains(character), $"Game font must contain its own glyph for {character}");
        Check(!FileAccess.FileExists("res://MyFont_Completed.ttf"), "The root duplicate font must be removed");
        CheckArtDirectory("res://art");
        foreach (string scene in new[] { "main_menu", "trade_scene", "customer" })
            Check(GD.Load<PackedScene>($"res://scenes/{scene}.tscn") != null, $"Scene must load: {scene}");
    }

    private static void CheckArtDirectory(string path)
    {
        using var directory = DirAccess.Open(path);
        Check(directory != null, $"Art directory must exist: {path}");
        foreach (string folder in directory.GetDirectories())
            CheckArtDirectory(path + "/" + folder);
        foreach (string file in directory.GetFiles().Where(file => !file.EndsWith(".import")))
        {
            Check(file.All(character => character is >= 'a' and <= 'z' or >= '0' and <= '9' or '_' or '.'),
                $"Resource names must use lowercase ASCII snake_case: {path}/{file}");
            Check(GD.Load<Resource>(path + "/" + file) != null, $"Resource must load after rename: {path}/{file}");
        }
    }
}
