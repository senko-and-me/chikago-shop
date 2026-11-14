using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class ProjectTest
{
    private async Task TestLocalizedCraftButtons()
    {
        await TestCraftPaperEdge();
        string locale = TranslationServer.GetLocale();
        var game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
        game.SavePath = "";
        Root.AddChild(game);
        game.SetProcess(false);
        var camera = game.GetNode<WorldCamera>("Camera2D");
        camera.SetProcess(false);
        game.Score = 1000;
        game.TryUnlockMusicWorkshop();
        try
        {
            foreach (string language in new[] { "ru", "en", "ru", "en" })
            {
                TranslationServer.SetLocale(language);
                await ToSignal(this, SignalName.ProcessFrame);
                foreach (string room in new[] { "Atelier", "MusicWorkshop" })
                {
                    bool english = language == "en";
                    var button = game.GetNode<Button>($"World/{room}/GetItemButton");
                    Check(button.Material == null && button.GetNode<NinePatchRect>("Frame").Texture is AtlasTexture frame &&
                        frame.Atlas.ResourcePath == "res://art/ui/camera_frame.png",
                        "Both craft buttons must use the ready transparent frame texture without a procedural shader");
                    Check(button.Size == new Vector2(450, 190) && button.Text == "" && !button.Disabled,
                        "Language changes must preserve craft-button geometry and availability");
                    foreach (string state in new[] { "normal", "hover", "pressed", "disabled" })
                        Check(button.GetThemeStylebox(state) is StyleBoxEmpty,
                            "Button states must leave the textured scene artwork unobstructed");
                    var caption = button.GetNode<Label>("EnglishCaption");
                    var help = button.GetNode<TextureRect>("RarityHotspot/EnglishHelp");
                    Check(caption.Visible == english && help.Visible && button.GetNode<Control>("RussianCaption").Visible == !english && caption.Text == "CREATE\nITEMS" &&
                        caption.MouseFilter == Control.MouseFilterEnum.Ignore && help.MouseFilter == Control.MouseFilterEnum.Ignore,
                        "English lettering and question icon must switch together without blocking input");
                }
            }
            var atelier = game.GetNode<Button>("World/Atelier/GetItemButton");
            camera.Zoom = Vector2.One * 0.9f;
            camera.Position = atelier.GetGlobalTransform() * (atelier.Size / 2);
            camera.ClampToBounds();
            using (var image = await CapturePresentation("craft-button-english")) { }
            Vector2 point = atelier.GetGlobalTransformWithCanvas() * (atelier.Size / 2);
            Root.PushInput(new InputEventMouseMotion { Position = point, GlobalPosition = point }, true);
            for (int i = 0; i < 3; i++) await ToSignal(this, SignalName.ProcessFrame);
            Check(atelier.GetNode<Control>("Paper").Modulate == atelier.GetNode<Control>("RarityHotspot/EnglishHelp").Modulate &&
                atelier.GetNode<Control>("Paper").Modulate.R < 1,
                "Real mouse hover must darken the help artwork together with the paper");
            using (var image = await CapturePresentation("craft-button-hover")) { }
            TranslationServer.SetLocale("ru");
            using (var image = await CapturePresentation("craft-button-russian")) { }
            TranslationServer.SetLocale("en");
            int cash = game.Score, energy = game.EnergyRemaining;
            await ClickControl(game, "World/Atelier/GetItemButton");
            Check(game.GetNode<EmbroideryGame>("EmbroideryGame").IsOpen && game.Score == cash && game.EnergyRemaining == energy,
                "A real click on the translated button must open the existing minigame without charging before activation");
            await PressEscapeForTest();
            Check(!game.CraftMiniGameOpen && !Paused, "The translated craft flow must still cancel normally");
        }
        finally
        {
            game.QueueFree();
            TranslationServer.SetLocale(locale);
        }
        await ToSignal(this, SignalName.ProcessFrame);
    }

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
                        curtain.GetIndex() == 0 && curtain.Modulate == Colors.White,
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
    private async Task TestDriveAssetReplacements()
    {
        string[] kinds = { "hat", "gloves", "guitar", "trumpet", "clothing_first", "clothing_exclusive",
            "clothing_unique", "music_first", "music_exclusive", "music_unique" };
        Check(WorkshopItemCatalog.Items.Count == kinds.Length &&
              WorkshopItemCatalog.Items.Select(item => item.Kind).SequenceEqual(kinds),
            "Replacing artwork must not add, reorder or rename saved item identities");
        foreach (var entry in new[] { (2, "guitar.png"), (7, "violin.png"), (8, "drums.png") })
        {
            Check(TradeScene.GetItemArtPath(entry.Item1) == "res://art/items/" + entry.Item2,
                "Existing items must resolve to the approved artist PNGs");
            var image = GD.Load<Texture2D>(TradeScene.GetItemArtPath(entry.Item1)).GetImage();
            Check(image.GetFormat() == Image.Format.Rgba8 && image.GetPixel(0, 0).A == 0,
                "Artist images must preserve a transparent background");
        }
        var game = GD.Load<PackedScene>("res://scenes/trade_scene.tscn").Instantiate<TradeScene>();
        game.SavePath = "";
        Root.AddChild(game);
        await ToSignal(this, SignalName.ProcessFrame);
        try
        {
            var variants = game.GetNode<Node2D>("World/TradingRoom/Item").GetChildren().OfType<Sprite2D>().ToArray();
            Check(variants.Length == kinds.Length && variants.Select(sprite => sprite.Name.ToString().ToLowerInvariant()).Distinct().Count() == kinds.Length,
                "Trading must retain exactly one sprite for each existing item kind");
            foreach (int index in new[] { 2, 7, 8 })
            {
                var sprite = variants.Single(value => value.Name.ToString().ToLowerInvariant() == kinds[index]);
                Vector2 size = sprite.Texture.GetSize() * sprite.Scale;
                Check(sprite.Texture.ResourcePath == TradeScene.GetItemArtPath(index) && size.X <= 150.01f && size.Y <= 150.01f,
                    "Trading sprites must use matching catalogue art and fit their existing display area");
            }
            var surface = game.GetNode<PolishingSurface>("PolishingGame/Surface/Polishing");
            Check(PolishingSurface.InstrumentKinds.Count == 4 && PolishingSurface.InstrumentKinds.Contains("drums") &&
                  !PolishingSurface.InstrumentKinds.Contains("sax"),
                "Artist drums must replace the sax choice rather than add another minigame variant");
            foreach (string kind in PolishingSurface.InstrumentKinds.Concat(new[] { "sax" }))
            {
                var session = surface.Begin(kind);
                Check(session.Cells.Any(cell => cell.Inside) && !session.Inside(0, 0),
                    "Polishing silhouettes must remain playable with transparent margins");
                session.Start();
                var cell = session.Cells.First(value => value.Inside && session.Inside(value.X + 14, value.Y));
                session.BeginRub(cell.X, cell.Y);
                for (int i = 0; i < 30; i++) session.MoveRub(cell.X + (i % 2 == 0 ? 14 : 0), cell.Y, 20);
                Check(session.Progress > 0, "Rubbing inside each instrument silhouette must remove its coating");
            }
            // Open the actual paid mini-game on the drum variant and use viewport mouse events.
            game.Score = 1000;
            game.TryUnlockMusicWorkshop();
            int drumIndex = PolishingSurface.InstrumentKinds.ToList().IndexOf("drums");
            for (ulong seed = 0; ; seed++)
            {
                GD.Seed(seed);
                if (GD.Randi() % 4 == (uint)drumIndex) { GD.Seed(seed); break; }
                Check(seed < 100, "A deterministic drum fixture seed must be found");
            }
            var miniGame = game.GetNode<EmbroideryGame>("PolishingGame");
            miniGame.SetProcess(false);
            game.GetItemFromMusicWorkshop();
            Check(miniGame.IsOpen && surface.InstrumentKind == "drums", "Opening polishing must select the ready drum artwork");
            int cashBefore = game.Score, energyBefore = game.EnergyRemaining;
            miniGame.Activate();
            Check(game.Score == cashBefore - WorkshopItemCatalog.CraftCost("music") && game.EnergyRemaining == energyBefore - 1,
                "The new drum presentation must retain the existing craft payment");
            await ToSignal(this, SignalName.ProcessFrame);
            await ToSignal(this, SignalName.ProcessFrame);
            var target = miniGame.Polishing.Cells.First(cell => cell.Inside && miniGame.Polishing.Inside(cell.X + 14, cell.Y));
            Vector2 left = surface.LogicalToScreen(new Vector2((float)target.X, (float)target.Y));
            Vector2 right = surface.LogicalToScreen(new Vector2((float)target.X + 14, (float)target.Y));
            Root.PushInput(new InputEventMouseMotion { Position = left, GlobalPosition = left }, true);
            Root.PushInput(new InputEventMouseButton { Position = left, GlobalPosition = left, ButtonIndex = MouseButton.Left,
                Pressed = true, ButtonMask = MouseButtonMask.Left }, true);
            for (int i = 0; i < 30; i++)
            {
                Vector2 position = i % 2 == 0 ? right : left;
                Root.PushInput(new InputEventMouseMotion { Position = position, GlobalPosition = position,
                    ButtonMask = MouseButtonMask.Left }, true);
            }
            Root.PushInput(new InputEventMouseButton { Position = right, GlobalPosition = right,
                ButtonIndex = MouseButton.Left, Pressed = false }, true);
            Check(miniGame.Polishing.Progress > 0, "Real viewport mouse strokes must clean the artist drum silhouette");
            if (OS.GetCmdlineUserArgs().Contains("--asset-preview"))
            {
                await ToSignal(this, SignalName.ProcessFrame);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                Root.GetTexture().GetImage().SavePng(System.IO.Path.Combine(OS.GetUserDataDir(), "drum-polishing.png"));
            }
        }
        finally { game.QueueFree(); }
        await ToSignal(this, SignalName.ProcessFrame);
        await TestPlaceholderItems();
    }

    private static void TestResourceOrganization()
    {
        var font = GD.Load<FontFile>("res://art/fonts/game.ttf");
        Check(font != null, "The canonical game font must load");
        string supported = font.GetSupportedChars();
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
